using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.Mediation
{
    /// <summary>
    /// Điểm vào duy nhất để game gọi quảng cáo. Singleton + Coroutine (không dùng UniTask/VContainer).
    /// Waterfall theo priority trong MediationConfig: ad nào ready đầu tiên (priority thấp nhất) thì được show.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MediationManager : MonoBehaviour
    {
        // ───────────────────────── Singleton ─────────────────────────
        private static MediationManager _instance;
        private static bool _isQuitting;

        public static bool HasInstance => _instance != null;

        public static MediationManager Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    var go = new GameObject("[LSDK] MediationManager");
                    _instance = go.AddComponent<MediationManager>();
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _isQuitting = false;
            OnInitialized = null;
            OnAdLoaded = null;
            OnAdLoadFailed = null;
            OnAdOpened = null;
            OnAdClicked = null;
            OnAdClosed = null;
            OnAdRevenuePaid = null;
            OnFullscreenAdStateChanged = null;
            OnNetworkChanged = null;
        }

        // ───────────────────────── Events ─────────────────────────
        public static event Action OnInitialized;
        public static event Action<AdFormat, AdNetworkType> OnAdLoaded;
        public static event Action<AdFormat, AdNetworkType, MediationError> OnAdLoadFailed;
        /// <summary>(format, network, placement)</summary>
        public static event Action<AdFormat, AdNetworkType, string> OnAdOpened;
        public static event Action<AdFormat, AdNetworkType, string> OnAdClicked;
        /// <summary>(format, network, placement, result)</summary>
        public static event Action<AdFormat, AdNetworkType, string, AdShowResult> OnAdClosed;
        /// <summary>Nối vào TrackingManager để bắn ad revenue.</summary>
        public static event Action<AdRevenueInfo> OnAdRevenuePaid;
        /// <summary>true = fullscreen ad đang mở, false = đã đóng. Dùng để mute/pause game.</summary>
        public static event Action<bool> OnFullscreenAdStateChanged;

        // ───────────────────────── State ─────────────────────────
        private static readonly AdFormat[] FullscreenFormats =
        {
            AdFormat.Interstitial, AdFormat.Rewarded, AdFormat.AppOpen
        };

        private sealed class ShowContext
        {
            public IAdNetworkAdapter Adapter;
            public AdFormat Format;
            public string Placement;
            public Action<AdShowResult> Callback;
            public bool Opened;
        }

        private MediationConfig _config;
        private readonly List<IAdNetworkAdapter> _adapters = new List<IAdNetworkAdapter>();
        private readonly Dictionary<string, int> _retryCounts = new Dictionary<string, int>();
        private readonly List<Action<bool>> _initCallbacks = new List<Action<bool>>();
        private bool _initializing;

        private ShowContext _current;
        private double _lastInterstitialAt = -1e9;
        private double _lastAppOpenAt = -1e9;
        private double _lastFullscreenClosedAt = -1e9;

        private bool _hasPaused;
        private DateTime _pausedAtUtc;

        // banner
        private bool _bannerRequested;
        private bool _bannerLoaded;
        private BannerPosition _bannerPosition = BannerPosition.Bottom;
        private IAdNetworkAdapter _bannerAdapter;
        private int _bannerAdapterIndex;
        private int _bannerFailRound;
        private Coroutine _bannerRetry;

        // network monitor
        private NetworkReachability _lastReachability = NetworkReachability.NotReachable;
        private Coroutine _networkMonitor;

        /// <summary>true = đang có kết nối mạng. Cập nhật mỗi 3 giây, dùng để show/ẩn UI offline.</summary>
        public static event Action<bool> OnNetworkChanged;
        public bool IsNetworkAvailable { get; private set; }

        [field: SerializeField] public bool IsInitialized { get; private set; }
        public MediationConfig Config => _config;
        /// <summary>User đã mua Remove Ads: chặn Banner/Interstitial/AppOpen, Rewarded vẫn chạy.</summary>
        [field: SerializeField] public bool AdsRemoved { get; private set; }
        /// <summary>Tắt tạm App Open tự động (vd đang IAP, đang xin permission...).</summary>
        public bool AppOpenEnabled { get; set; } = true;
        public bool IsShowingFullscreenAd => _current != null;
        public float BannerHeightPixels => _bannerAdapter != null ? _bannerAdapter.BannerHeightPixels : 0f;

        // ───────────────────────── Unity ─────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (IsInitialized || _initializing) return;
            var cfg = MediationConfig.Load();
            if (cfg != null && cfg.autoInitialize) Initialize(cfg);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _hasPaused = true;
                _pausedAtUtc = DateTime.UtcNow;
                StopNetworkMonitor();
                return;
            }

            if (!IsInitialized || !_hasPaused) return;

            PreloadAll(); // load lại ad hết hạn / bị mất
            TryShowAppOpenOnResume();
            StartNetworkMonitor();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
            StopNetworkMonitor();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ───────────────────────── Init ─────────────────────────
        /// <param name="config">null = load từ Resources/LSDK/Mediation/SO/MediationConfig.</param>
        public void Initialize(MediationConfig config = null, Action<bool> onComplete = null)
        {
            if (IsInitialized)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (onComplete != null) _initCallbacks.Add(onComplete);
            if (_initializing) return;

            _config = config != null ? config : MediationConfig.Load();
            if (_config == null)
            {
                DebugUtils.Error("MediationConfig not found. Menu: LSDK > Mediation > Create Config.");
                FlushInitCallbacks(false);
                return;
            }

            _initializing = true;
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            var configs = new List<NetworkConfig>();
            foreach (var n in _config.networks)
            {
                if (n != null && n.enabled) configs.Add(n);
            }
            configs.Sort((a, b) => a.priority.CompareTo(b.priority));

            int pending = 0;
            foreach (var nc in configs)
            {
                var adapter = AdAdapterRegistry.Create(nc.network);
                if (adapter == null)
                {
                    DebugUtils.Warning($"No adapter registered for {nc.network}. " +
                                        "Is the network SDK installed and its scripting define set?");
                    continue;
                }

                _adapters.Add(adapter);
                Hook(adapter);
                pending++;

                var captured = adapter;
                adapter.Initialize(_config, nc, ok =>
                {
                    pending--;
                    if (!ok) DebugUtils.Warning($"{captured.Network} failed to initialize.");
                    else if (IsInitialized) PreloadAdapter(captured); // init trễ hơn timeout
                });
            }

            float waited = 0f;
            while (pending > 0 && waited < _config.initTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            _initializing = false;

            bool any = false;
            foreach (var a in _adapters) any |= a.IsInitialized;
            IsInitialized = any;

            if (!any)
            {
                DebugUtils.Error("No ad network initialized.");
                FlushInitCallbacks(false);
                yield break;
            }

            OnInitialized?.Invoke();
            FlushInitCallbacks(true);
            DebugUtils.Success($"Khởi tạo thành công: {InitializedNetworks()}");

            if (_config.autoPreload) PreloadAll();
            if (_bannerRequested && !AdsRemoved) StartBanner(0);
            StartNetworkMonitor();
        }

        private string InitializedNetworks()
        {
            var names = new List<string>();
            foreach (var a in _adapters)
            {
                if (a.IsInitialized) names.Add(a.Network.ToString());
            }
            return names.Count > 0 ? string.Join(", ", names) : "none";
        }

        private void FlushInitCallbacks(bool success)
        {
            var list = new List<Action<bool>>(_initCallbacks);
            _initCallbacks.Clear();
            foreach (var cb in list)
            {
                try { cb(success); }
                catch (Exception e) { DebugUtils.Exception(e); }
            }
        }

        private void Hook(IAdNetworkAdapter adapter)
        {
            adapter.Loaded += f => HandleLoaded(adapter, f);
            adapter.LoadFailed += (f, e) => HandleLoadFailed(adapter, f, e);
            adapter.Opened += f => HandleOpened(adapter, f);
            adapter.Clicked += f => HandleClicked(adapter, f);
            adapter.Closed += (f, rewarded) => HandleClosed(adapter, f, rewarded);
            adapter.ShowFailed += (f, e) => HandleShowFailed(adapter, f, e);
            adapter.RevenuePaid += info => OnAdRevenuePaid?.Invoke(info);
        }

        // ───────────────────────── Public API ─────────────────────────
        public bool IsReady(AdFormat format)
        {
            return IsInitialized && FindReadyAdapter(format) != null;
        }

        public bool IsRewardedReady => IsReady(AdFormat.Rewarded);
        public bool IsInterstitialReady => IsReady(AdFormat.Interstitial);

        // ───────────────────────── Consent Public API ─────────────────────────
        /// <summary>
        /// true nếu có ít nhất 1 network yêu cầu hiện nút mở Privacy Options (theo UMP).
        /// Dùng để show/ẩn nút Settings trong game.
        /// </summary>
        public bool IsPrivacyOptionsRequired
        {
            get
            {
                foreach (var a in _adapters)
                    if (a.IsPrivacyOptionsRequired) return true;
                return false;
            }
        }

        /// <summary>
        /// Mở Privacy Options Form khi user bấm nút trong Settings.
        /// Chỉ cần gọi khi IsPrivacyOptionsRequired == true.
        /// </summary>
        public void ShowPrivacyOptionsForm(Action onComplete = null)
        {
            foreach (var a in _adapters)
            {
                if (!a.IsPrivacyOptionsRequired) continue;

                a.ShowPrivacyOptionsForm(err =>
                {
                    if (err != null) DebugUtils.Warning($"Privacy options form lỗi: {err}");
                    try { onComplete?.Invoke(); }
                    catch (Exception e) { DebugUtils.Exception(e); }
                });
                return;
            }

            // Không network nào cần — gọi callback ngay
            try { onComplete?.Invoke(); }
            catch (Exception e) { DebugUtils.Exception(e); }
        }

        public void ShowInterstitial(string placement, Action<AdShowResult> onResult = null, bool ignoreCooldown = false)
        {
            ShowFullscreen(AdFormat.Interstitial, placement, onResult, ignoreCooldown);
        }

        /// <summary>Callback gọi SAU khi ad đóng. Completed = user được nhận thưởng.</summary>
        public void ShowRewarded(string placement, Action<AdShowResult> onResult)
        {
            ShowFullscreen(AdFormat.Rewarded, placement, onResult, false);
        }

        public void ShowAppOpen(string placement, Action<AdShowResult> onResult = null)
        {
            ShowFullscreen(AdFormat.AppOpen, placement, onResult, false);
        }

        /// <summary>Gọi khi user mua Remove Ads (và khi load game nếu đã mua).</summary>
        public void SetAdsRemoved(bool removed)
        {
            if (AdsRemoved == removed) return;
            AdsRemoved = removed;

            if (removed)
            {
                StopBannerRetry();
                if (_bannerAdapter != null)
                {
                    DebugUtils.Success($"Tắt: Banner ({_bannerAdapter.Network}) ads removed");
                    _bannerAdapter.DestroyBanner();
                }
                _bannerAdapter = null;
                _bannerLoaded = false;
            }
            else if (IsInitialized)
            {
                PreloadAll();
                if (_bannerRequested) StartBanner(0);
            }
        }

        // ───────────────────────── Fullscreen ─────────────────────────
        private void ShowFullscreen(AdFormat format, string placement, Action<AdShowResult> callback, bool ignoreCooldown)
        {
            placement = placement ?? string.Empty;

            if (!IsInitialized)
            {
                InvokeCallback(callback, AdShowResult.NotReady);
                return;
            }

            if (_current != null)
            {
                InvokeCallback(callback, AdShowResult.Blocked);
                return;
            }

            if (format != AdFormat.Rewarded && AdsRemoved)
            {
                InvokeCallback(callback, AdShowResult.Blocked);
                return;
            }

            double now = Time.realtimeSinceStartupAsDouble;
            if (format == AdFormat.Interstitial && !ignoreCooldown &&
                now - _lastInterstitialAt < _config.interstitialCooldown)
            {
                InvokeCallback(callback, AdShowResult.Blocked);
                return;
            }

            if (format == AdFormat.AppOpen && now - _lastAppOpenAt < _config.appOpenCooldown)
            {
                InvokeCallback(callback, AdShowResult.Blocked);
                return;
            }

            var adapter = FindReadyAdapter(format);
            if (adapter == null)
            {
                foreach (var a in _adapters) LoadIfNeeded(a, format);
                InvokeCallback(callback, AdShowResult.NotReady);
                return;
            }

            _current = new ShowContext
            {
                Adapter = adapter,
                Format = format,
                Placement = placement,
                Callback = callback
            };

            try
            {
                DebugUtils.Success($"Show: {format} ({adapter.Network}) placement={placement}");
                adapter.ShowFullscreen(format, placement);
            }
            catch (Exception e)
            {
                DebugUtils.Exception(e);
                FinishShow(AdShowResult.Failed);
            }
        }

        private void FinishShow(AdShowResult result)
        {
            var ctx = _current;
            if (ctx == null) return;
            _current = null;

            double now = Time.realtimeSinceStartupAsDouble;
            _lastFullscreenClosedAt = now;

            if (result == AdShowResult.Completed || result == AdShowResult.NotCompleted)
            {
                switch (ctx.Format)
                {
                    case AdFormat.Interstitial:
                        _lastInterstitialAt = now;
                        break;
                    case AdFormat.AppOpen:
                        _lastAppOpenAt = now;
                        break;
                    case AdFormat.Rewarded:
                        if (_config.rewardedResetsInterstitialCooldown) _lastInterstitialAt = now;
                        break;
                }
            }

            if (ctx.Opened)
            {
                DebugUtils.Success($"Tắt: {ctx.Format} ({ctx.Adapter.Network}) placement={ctx.Placement} result={result}");
                OnFullscreenAdStateChanged?.Invoke(false);
                OnAdClosed?.Invoke(ctx.Format, ctx.Adapter.Network, ctx.Placement, result);
            }

            if (_config.autoPreload) LoadIfNeeded(ctx.Adapter, ctx.Format);
            InvokeCallback(ctx.Callback, result);
        }

        private static void InvokeCallback(Action<AdShowResult> callback, AdShowResult result)
        {
            if (callback == null) return;
            try { callback(result); }
            catch (Exception e) { DebugUtils.Exception(e); }
        }

        private IAdNetworkAdapter FindReadyAdapter(AdFormat format)
        {
            foreach (var a in _adapters)
            {
                if (a.IsInitialized && a.IsFormatSupported(format) && a.IsReady(format)) return a;
            }
            return null;
        }

        private void TryShowAppOpenOnResume()
        {
            if (!_config.autoShowAppOpen || !AppOpenEnabled || AdsRemoved) return;
            if (_current != null) return;

            // Ad vừa đóng xong sẽ kéo theo OnApplicationPause(false) → bỏ qua
            if (Time.realtimeSinceStartupAsDouble - _lastFullscreenClosedAt < 2.0) return;
            if ((DateTime.UtcNow - _pausedAtUtc).TotalSeconds < _config.appOpenMinBackgroundSeconds) return;

            ShowAppOpen("app_resume");
        }

        // ───────────────────────── Network Monitor ─────────────────────────
        private void StartNetworkMonitor()
        {
            if (_networkMonitor != null) return;
            _lastReachability = Application.internetReachability;
            IsNetworkAvailable = _lastReachability != NetworkReachability.NotReachable;
            _networkMonitor = StartCoroutine(NetworkMonitorRoutine());
        }

        private void StopNetworkMonitor()
        {
            if (_networkMonitor == null) return;
            StopCoroutine(_networkMonitor);
            _networkMonitor = null;
        }

        private IEnumerator NetworkMonitorRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(3f);

                var current = Application.internetReachability;
                bool wasOffline = _lastReachability == NetworkReachability.NotReachable;
                bool isOnline   = current != NetworkReachability.NotReachable;
                bool wasOnline  = _lastReachability != NetworkReachability.NotReachable;

                if (wasOffline && isOnline)
                {
                    IsNetworkAvailable = true;
                    DebugUtils.Success("Mạng trở lại — load lại ad.");
                    OnNetworkChanged?.Invoke(true);
                    OnNetworkRestored();
                }
                else if (wasOnline && !isOnline)
                {
                    IsNetworkAvailable = false;
                    DebugUtils.Warning("Mất kết nối mạng.");
                    OnNetworkChanged?.Invoke(false);
                }

                _lastReachability = current;
            }
        }

        private void OnNetworkRestored()
        {
            // Fullscreen: reset backoff rồi load ngay
            _retryCounts.Clear();
            if (_config.autoPreload) PreloadAll();

            // Banner: nếu chưa hiện (đang chờ retry hoặc load lỗi) thì reset và thử lại ngay
            if (_bannerRequested && !AdsRemoved)
            {
                if (_bannerAdapter == null)
                {
                    // Đang ở backoff → tắt timer cũ, reset round và khởi động ngay
                    StopBannerRetry();
                    _bannerFailRound = 0;
                    StartBanner(0);
                }
                // Nếu banner đang tồn tại (đã hiện) → AdMob tự refresh, không cần can thiệp
            }
        }

        // ───────────────────────── Load / Retry ─────────────────────────
        private void PreloadAll()
        {
            foreach (var a in _adapters) PreloadAdapter(a);
        }

        private void PreloadAdapter(IAdNetworkAdapter adapter)
        {
            foreach (var f in FullscreenFormats) LoadIfNeeded(adapter, f);
        }

        private void LoadIfNeeded(IAdNetworkAdapter adapter, AdFormat format)
        {
            if (format == AdFormat.Banner) return; // banner load qua ShowBanner
            if (!adapter.IsInitialized || !adapter.IsFormatSupported(format)) return;
            if (AdsRemoved && format != AdFormat.Rewarded) return;
            if (adapter.IsReady(format) || adapter.IsLoading(format)) return;

            adapter.Load(format);
        }

        private void ScheduleRetry(IAdNetworkAdapter adapter, AdFormat format)
        {
            string key = Key(adapter, format);
            _retryCounts.TryGetValue(key, out int count);
            count++;
            _retryCounts[key] = count;

            if (_config.maxRetryCount > 0 && count > _config.maxRetryCount) return;

            float delay = Mathf.Min(_config.retryBaseDelay * Mathf.Pow(2f, count - 1), _config.retryMaxDelay);
            StartCoroutine(RetryLoadRoutine(adapter, format, delay));
        }

        private IEnumerator RetryLoadRoutine(IAdNetworkAdapter adapter, AdFormat format, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            LoadIfNeeded(adapter, format);
        }

        private static string Key(IAdNetworkAdapter adapter, AdFormat format) => adapter.Network + "_" + format;

        // ───────────────────────── Adapter events ─────────────────────────
        private void HandleLoaded(IAdNetworkAdapter adapter, AdFormat format)
        {
            _retryCounts.Remove(Key(adapter, format));

            if (format == AdFormat.Banner && adapter == _bannerAdapter)
            {
                _bannerLoaded = true;
                _bannerFailRound = 0;
            }

            DebugUtils.Success($"Load thành công: {format} ({adapter.Network})");
            OnAdLoaded?.Invoke(format, adapter.Network);
        }

        private void HandleLoadFailed(IAdNetworkAdapter adapter, AdFormat format, MediationError error)
        {
            DebugUtils.Warning($"{adapter.Network} {format} load failed: {error}");
            OnAdLoadFailed?.Invoke(format, adapter.Network, error);

            if (format == AdFormat.Banner)
            {
                HandleBannerLoadFailed(adapter);
                return;
            }

            if (_config.autoPreload) ScheduleRetry(adapter, format);
        }

        private void HandleOpened(IAdNetworkAdapter adapter, AdFormat format)
        {
            if (_current == null || _current.Adapter != adapter || _current.Format != format) return;

            _current.Opened = true;
            OnFullscreenAdStateChanged?.Invoke(true);
            OnAdOpened?.Invoke(format, adapter.Network, _current.Placement);
        }

        private void HandleClicked(IAdNetworkAdapter adapter, AdFormat format)
        {
            string placement = format == AdFormat.Banner ? "banner" : (_current != null ? _current.Placement : string.Empty);
            OnAdClicked?.Invoke(format, adapter.Network, placement);
        }

        private void HandleClosed(IAdNetworkAdapter adapter, AdFormat format, bool rewardEarned)
        {
            if (_current == null || _current.Adapter != adapter || _current.Format != format) return;

            AdShowResult result = (format == AdFormat.Rewarded && !rewardEarned)
                ? AdShowResult.NotCompleted
                : AdShowResult.Completed;
            FinishShow(result);
        }

        private void HandleShowFailed(IAdNetworkAdapter adapter, AdFormat format, MediationError error)
        {
            DebugUtils.Warning($"{adapter.Network} {format} show failed: {error}");
            if (_current == null || _current.Adapter != adapter || _current.Format != format) return;

            FinishShow(AdShowResult.Failed);
        }

        // ───────────────────────── Banner ─────────────────────────
        public void ShowBanner(BannerPosition position = BannerPosition.Bottom)
        {
            _bannerRequested = true;
            _bannerPosition = position;

            if (!IsInitialized || AdsRemoved) return; // sẽ tự show sau khi init / sau khi gỡ AdsRemoved

            if (_bannerAdapter != null)
            {
                DebugUtils.Success($"Show: Banner ({_bannerAdapter.Network}) position={position}");
                _bannerAdapter.ShowBanner(position); // banner đã tạo (đang ẩn hoặc đang load)
                return;
            }

            StartBanner(0);
        }

        /// <summary>Ẩn banner nhưng giữ instance (AdMob vẫn có thể tự refresh ngầm). Muốn dừng hẳn dùng DestroyBanner.</summary>
        public void HideBanner()
        {
            _bannerRequested = false;
            StopBannerRetry();
            if (_bannerAdapter == null) return;

            DebugUtils.Success($"Tắt: Banner ({_bannerAdapter.Network})");
            _bannerAdapter.HideBanner();
        }

        public void DestroyBanner()
        {
            _bannerRequested = false;
            StopBannerRetry();
            if (_bannerAdapter != null)
            {
                DebugUtils.Success($"Tắt: Banner ({_bannerAdapter.Network}) destroy");
                _bannerAdapter.DestroyBanner();
            }
            _bannerAdapter = null;
            _bannerLoaded = false;
        }

        private void StartBanner(int fromIndex)
        {
            StopBannerRetry();

            for (int i = fromIndex; i < _adapters.Count; i++)
            {
                var a = _adapters[i];
                if (!a.IsInitialized || !a.IsFormatSupported(AdFormat.Banner)) continue;

                _bannerAdapter = a;
                _bannerAdapterIndex = i;
                _bannerLoaded = false;
                DebugUtils.Success($"Show: Banner ({a.Network}) position={_bannerPosition}");
                a.ShowBanner(_bannerPosition);
                return;
            }

            // Hết network để thử → đợi rồi thử lại từ network ưu tiên nhất
            _bannerAdapter = null;
            ScheduleBannerRetry();
        }

        private void HandleBannerLoadFailed(IAdNetworkAdapter adapter)
        {
            if (adapter != _bannerAdapter || !_bannerRequested || AdsRemoved) return;

            int next = _bannerAdapterIndex + 1;
            adapter.DestroyBanner();
            _bannerAdapter = null;
            _bannerLoaded = false;
            StartBanner(next);
        }

        private void ScheduleBannerRetry()
        {
            bool anySupport = false;
            foreach (var a in _adapters)
            {
                if (a.IsInitialized && a.IsFormatSupported(AdFormat.Banner)) { anySupport = true; break; }
            }
            if (!anySupport || !_bannerRequested) return;

            _bannerFailRound++;
            if (_config.maxRetryCount > 0 && _bannerFailRound > _config.maxRetryCount) return;

            float delay = Mathf.Min(_config.retryBaseDelay * Mathf.Pow(2f, _bannerFailRound - 1), _config.retryMaxDelay);
            _bannerRetry = StartCoroutine(BannerRetryRoutine(delay));
        }

        private IEnumerator BannerRetryRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _bannerRetry = null;
            if (_bannerRequested && !AdsRemoved && _bannerAdapter == null) StartBanner(0);
        }

        private void StopBannerRetry()
        {
            if (_bannerRetry != null)
            {
                StopCoroutine(_bannerRetry);
                _bannerRetry = null;
            }
        }
    }
}

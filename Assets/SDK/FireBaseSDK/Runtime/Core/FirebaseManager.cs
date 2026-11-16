using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using UnityEngine;

namespace LSDK.Firebase
{
    /// <summary>
    /// Điểm vào Firebase. Khởi tạo một lần rồi mới gọi Analytics, Auth, Crashlytics, Remote Config, Messaging.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class FirebaseManager : MonoBehaviour
    {
        private static FirebaseManager _instance;
        private static bool _isQuitting;

        public static bool HasInstance => _instance != null;

        public static FirebaseManager Instance
        {
            get
            {
                if (_instance == null && !_isQuitting)
                {
                    var go = new GameObject("[LSDK] FirebaseManager");
                    _instance = go.AddComponent<FirebaseManager>();
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
            OnAuthStateChanged = null;
            OnRegistrationId = null;
            OnMessageReceived = null;
        }

        /// <summary>true khi CheckAndFixDependencies thành công.</summary>
        public static event Action<bool> OnInitialized;
        /// <summary>Null khi đăng xuất.</summary>
        public static event Action<FirebaseUserInfo> OnAuthStateChanged;
        /// <summary>Firebase Installation ID dùng để nhắm thiết bị trên FCM.</summary>
        public static event Action<string> OnRegistrationId;
        public static event Action<FirebasePushMessage> OnMessageReceived;

        private readonly Queue<Action> _main = new Queue<Action>();
        private readonly List<Action<bool>> _initCallbacks = new List<Action<bool>>();

        private FirebaseConfig _config;
        private FirebaseAnalyticsService _analytics;
        private FirebaseCrashlyticsService _crashlytics;
        private FirebaseAuthService _auth;
        private FirebaseInstallationsService _installations;
        private FirebaseRemoteConfigService _remoteConfig;
        private FirebaseMessagingService _messaging;
        private Coroutine _initRoutine;
        private bool _initializing;

        /// <summary>true khi Firebase khởi tạo thành công. Hiện trên Inspector lúc Play.</summary>
        [field: SerializeField]
        [field: Tooltip("Bật khi CheckAndFixDependencies thành công.")]
        public bool IsInitialized { get; private set; }
        public FirebaseConfig Config => _config;
        public string InstallationId => _installations == null ? string.Empty : _installations.Id;
        public string RegistrationId => _messaging == null ? string.Empty : _messaging.RegistrationId;
        public bool IsSignedIn => _auth != null && _auth.Current != null;
        public string UserId => _auth == null || _auth.Current == null ? string.Empty : _auth.Current.UserId;

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
            _analytics = new FirebaseAnalyticsService();
            _crashlytics = new FirebaseCrashlyticsService();
            _auth = new FirebaseAuthService(Post);
            _installations = new FirebaseInstallationsService();
            _remoteConfig = new FirebaseRemoteConfigService();
            _messaging = new FirebaseMessagingService(Post);
        }

        private void Start()
        {
            if (IsInitialized || _initializing) return;
            FirebaseConfig config = FirebaseConfig.Load();
            if (config != null && config.AutoInitialize) Initialize(config);
        }

        private void Update()
        {
            List<Action> batch = null;
            lock (_main)
            {
                if (_main.Count == 0) return;
                batch = new List<Action>(_main.Count);
                while (_main.Count > 0) batch.Add(_main.Dequeue());
            }

            for (int i = 0; i < batch.Count; i++)
            {
                try
                {
                    batch[i]();
                }
                catch (Exception exception)
                {
                    FirebaseLog.Exception(exception);
                }
            }
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_auth != null) _auth.Stop();
            if (_messaging != null) _messaging.Stop();
            StopAllCoroutines();
            _initRoutine = null;
            if (_instance == this) _instance = null;
        }

        /// <summary>Khởi tạo theo config. Null thì load Resources/LSDK/Firebase/SO/FirebaseConfig.</summary>
        public void Initialize(FirebaseConfig config = null, Action<bool> onComplete = null)
        {
            if (_isQuitting)
            {
                if (onComplete != null) onComplete(false);
                return;
            }

            if (IsInitialized)
            {
                if (onComplete != null) onComplete(true);
                return;
            }

            if (onComplete != null) _initCallbacks.Add(onComplete);
            if (_initializing) return;

            _config = config != null ? config : FirebaseConfig.Load();
            if (_config == null)
            {
                FirebaseLog.Error("Không thấy FirebaseConfig. Menu: Tools/LSDK/Firebase/Create Config.");
                FlushInit(false);
                Action<bool> missing = OnInitialized;
                if (missing != null) missing(false);
                return;
            }

            _initializing = true;
            _initRoutine = StartCoroutine(InitializeRoutine());
        }

        /// <summary>Ghi sự kiện Analytics.</summary>
        public void LogEvent(string name)
        {
            if (!ReadyAnalytics()) return;
            _analytics.LogEvent(name);
        }

        /// <summary>Ghi sự kiện Analytics với một tham số chuỗi.</summary>
        public void LogEvent(string name, string parameterName, string parameterValue)
        {
            if (!ReadyAnalytics()) return;
            _analytics.LogEvent(name, parameterName, parameterValue);
        }

        /// <summary>Ghi sự kiện Analytics với một tham số số nguyên.</summary>
        public void LogEvent(string name, string parameterName, long parameterValue)
        {
            if (!ReadyAnalytics()) return;
            _analytics.LogEvent(name, parameterName, parameterValue);
        }

        /// <summary>Ghi sự kiện Analytics với một tham số số thực.</summary>
        public void LogEvent(string name, string parameterName, double parameterValue)
        {
            if (!ReadyAnalytics()) return;
            _analytics.LogEvent(name, parameterName, parameterValue);
        }

        /// <summary>Gắn user Analytics. Chuỗi rỗng để xóa.</summary>
        public void SetAnalyticsUserId(string userId)
        {
            if (!ReadyAnalytics()) return;
            _analytics.SetUserId(userId);
        }

        /// <summary>User property Analytics.</summary>
        public void SetUserProperty(string name, string value)
        {
            if (!ReadyAnalytics()) return;
            _analytics.SetUserProperty(name, value);
        }

        /// <summary>Đẩy doanh thu quảng cáo sang Analytics (event ad_impression). Mediation gọi vào đây.</summary>
        public void LogAdRevenue(string adPlatform, string adSource, string adFormat, string adUnit, double value, string currency)
        {
            if (!ReadyAnalytics()) return;
            _analytics.LogAdRevenue(adPlatform, adSource, adFormat, adUnit, value, currency);
        }

        /// <summary>Gắn user Crashlytics.</summary>
        public void SetCrashlyticsUserId(string userId)
        {
            if (!ReadyCrashlytics()) return;
            _crashlytics.SetUserId(userId);
        }

        /// <summary>Custom key Crashlytics.</summary>
        public void SetCrashlyticsKey(string key, string value)
        {
            if (!ReadyCrashlytics()) return;
            _crashlytics.SetCustomKey(key, value);
        }

        /// <summary>Breadcrumb Crashlytics.</summary>
        public void LogCrashlytics(string message)
        {
            if (!ReadyCrashlytics()) return;
            _crashlytics.Log(message);
        }

        /// <summary>Ghi exception không fatal.</summary>
        public void RecordException(Exception exception)
        {
            if (!ReadyCrashlytics()) return;
            _crashlytics.RecordException(exception);
        }

        /// <summary>Đăng nhập ẩn danh.</summary>
        public void SignInAnonymously(Action<bool, FirebaseUserInfo> callback)
        {
            if (!ReadyAuth())
            {
                if (callback != null) callback(false, null);
                return;
            }

            StartCoroutine(_auth.SignInAnonymously((ok, info, error) => FinishAuth(ok, info, callback)));
        }

        /// <summary>Đăng nhập email/mật khẩu.</summary>
        public void SignInWithEmail(string email, string password, Action<bool, FirebaseUserInfo> callback)
        {
            if (!ReadyAuth())
            {
                if (callback != null) callback(false, null);
                return;
            }

            StartCoroutine(_auth.SignInWithEmail(email, password, (ok, info, error) => FinishAuth(ok, info, callback)));
        }

        /// <summary>Tạo user email/mật khẩu rồi đăng nhập.</summary>
        public void CreateUserWithEmail(string email, string password, Action<bool, FirebaseUserInfo> callback)
        {
            if (!ReadyAuth())
            {
                if (callback != null) callback(false, null);
                return;
            }

            StartCoroutine(_auth.CreateUserWithEmail(email, password, (ok, info, error) => FinishAuth(ok, info, callback)));
        }

        /// <summary>Đăng xuất Auth.</summary>
        public void SignOut()
        {
            if (!ReadyAuth()) return;
            _auth.SignOut();
        }

        /// <summary>Lấy Firebase Installation ID.</summary>
        public void GetInstallationId(Action<bool, string> callback)
        {
            if (!IsInitialized || _config == null || !_config.EnableInstallations)
            {
                FirebaseLog.Warning("Installations chưa bật.");
                if (callback != null) callback(false, null);
                return;
            }

            StartCoroutine(_installations.FetchId(callback));
        }

        /// <summary>Fetch Remote Config rồi activate.</summary>
        public void FetchAndActivate(Action<bool> callback)
        {
            if (!ReadyRemoteConfig())
            {
                if (callback != null) callback(false);
                return;
            }

            int seconds = _config != null ? _config.CacheExpirationSeconds : 3600;
            StartCoroutine(_remoteConfig.FetchAndActivate(seconds, callback));
        }

        /// <summary>Đọc bool. Trả fallback khi chưa có giá trị remote hoặc default.</summary>
        public bool GetBool(string key, bool fallback = false)
        {
            if (!ReadyRemoteConfig()) return fallback;
            return _remoteConfig.GetBool(key, fallback);
        }

        /// <summary>Đọc số nguyên.</summary>
        public long GetLong(string key, long fallback = 0)
        {
            if (!ReadyRemoteConfig()) return fallback;
            return _remoteConfig.GetLong(key, fallback);
        }

        /// <summary>Đọc số thực.</summary>
        public double GetDouble(string key, double fallback = 0d)
        {
            if (!ReadyRemoteConfig()) return fallback;
            return _remoteConfig.GetDouble(key, fallback);
        }

        /// <summary>Đọc chuỗi.</summary>
        public string GetString(string key, string fallback = "")
        {
            if (!ReadyRemoteConfig()) return fallback;
            return _remoteConfig.GetString(key, fallback);
        }

        /// <summary>Custom signals cho điều kiện Remote Config.</summary>
        public void SetCustomSignals(IDictionary<string, string> signals, Action<bool> callback)
        {
            if (!ReadyRemoteConfig())
            {
                if (callback != null) callback(false);
                return;
            }

            StartCoroutine(_remoteConfig.SetCustomSignals(signals, callback));
        }

        /// <summary>Xin quyền thông báo. Trên Android hoàn tất ngay.</summary>
        public void RequestNotificationPermission(Action<bool> callback)
        {
            if (!ReadyMessaging())
            {
                if (callback != null) callback(false);
                return;
            }

            StartCoroutine(_messaging.RequestPermission(callback));
        }

        /// <summary>Đăng ký FCM. Installation ID trả về qua OnRegistrationId.</summary>
        public void RegisterMessaging(Action<bool> callback)
        {
            if (!ReadyMessaging())
            {
                if (callback != null) callback(false);
                return;
            }

            StartCoroutine(_messaging.Register(callback));
        }

        /// <summary>Subscribe topic FCM.</summary>
        public void SubscribeTopic(string topic, Action<bool> callback)
        {
            if (!ReadyMessaging())
            {
                if (callback != null) callback(false);
                return;
            }

            StartCoroutine(_messaging.Subscribe(topic, callback));
        }

        /// <summary>Unsubscribe topic FCM.</summary>
        public void UnsubscribeTopic(string topic, Action<bool> callback)
        {
            if (!ReadyMessaging())
            {
                if (callback != null) callback(false);
                return;
            }

            StartCoroutine(_messaging.Unsubscribe(topic, callback));
        }

        private IEnumerator InitializeRoutine()
        {
            System.Threading.Tasks.Task<DependencyStatus> check;
            try
            {
                check = FirebaseApp.CheckAndFixDependenciesAsync();
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                FinishInit(false);
                yield break;
            }

            yield return FirebaseTasks.Wait(check);
            string error = FirebaseTasks.GetError(check);
            if (error != null || check.Result != DependencyStatus.Available)
            {
                string status = error != null ? error : check.Result.ToString();
                FirebaseLog.Error("Firebase dependency không sẵn sàng: " + status);
                FinishInit(false);
                yield break;
            }

            try
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    FirebaseLog.Error("FirebaseApp.DefaultInstance null. Thiếu google-services.json hoặc GoogleService-Info.plist.");
                    FinishInit(false);
                    yield break;
                }
            }
            catch (Exception exception)
            {
                FirebaseLog.Exception(exception);
                FinishInit(false);
                yield break;
            }

            if (_config.EnableAnalytics) _analytics.Enable();
            if (_config.EnableCrashlytics) _crashlytics.Enable();
            if (_config.EnableAuth) _auth.Start(HandleAuthChanged);

            if (_config.EnableInstallations)
                yield return _installations.FetchId(null);

            if (_config.EnableRemoteConfig)
            {
                // ── 1. Khởi động RemoteConfigManager, thu gom defaults từ module ──
                RemoteConfigManager rcManager = RemoteConfigManager.Instance;
                var moduleDefaults = rcManager.CollectAllDefaults();
                yield return _remoteConfig.ApplyDefaultsRaw(moduleDefaults);

                // ── 2. Fetch + Activate ──
                bool rcFetched = false;
                if (_config.FetchOnInit)
                {
                    yield return _remoteConfig.FetchAndActivate(
                        _config.CacheExpirationSeconds,
                        ok => rcFetched = ok);
                }

                // ── 3. Populate tất cả module ──
                if (rcFetched || _remoteConfig.IsReady)
                    rcManager.PopulateAll();
            }

            if (_config.EnableMessaging)
            {
                _messaging.SetAutoRegister(_config.MessagingRegisterOnInit);
                _messaging.Start(RaiseRegistration, RaiseMessage);
                if (_config.RequestNotificationPermissionOnInit)
                    yield return _messaging.RequestPermission(null);
                if (_config.MessagingRegisterOnInit)
                    yield return _messaging.Register(null);
            }
            else
            {
                try
                {
                    _messaging.SetAutoRegister(false);
                }
                catch (Exception exception)
                {
                    FirebaseLog.Exception(exception);
                }
            }

            FirebaseLog.Info("Đã khởi tạo.");
            FinishInit(true);
        }

        private void HandleAuthChanged(FirebaseUserInfo info)
        {
            if (_config != null && _config.EnableAnalytics)
                _analytics.SetUserId(info == null ? string.Empty : info.UserId);
            if (_config != null && _config.EnableCrashlytics)
                _crashlytics.SetUserId(info == null ? string.Empty : info.UserId);

            Action<FirebaseUserInfo> handler = OnAuthStateChanged;
            if (handler != null) handler(info);
        }

        private void RaiseRegistration(string id)
        {
            Action<string> handler = OnRegistrationId;
            if (handler != null) handler(id);
        }

        private void RaiseMessage(FirebasePushMessage message)
        {
            Action<FirebasePushMessage> handler = OnMessageReceived;
            if (handler != null) handler(message);
        }

        private void FinishAuth(bool ok, FirebaseUserInfo info, Action<bool, FirebaseUserInfo> callback)
        {
            if (callback != null) callback(ok, info);
        }

        private void FinishInit(bool success)
        {
            _initializing = false;
            _initRoutine = null;
            IsInitialized = success;
            FlushInit(success);
            Action<bool> handler = OnInitialized;
            if (handler != null) handler(success);
        }

        private void FlushInit(bool success)
        {
            for (int i = 0; i < _initCallbacks.Count; i++)
                _initCallbacks[i](success);
            _initCallbacks.Clear();
        }

        private void Post(Action action)
        {
            if (action == null) return;
            lock (_main) _main.Enqueue(action);
        }

        private bool ReadyAnalytics()
        {
            if (IsInitialized && _config != null && _config.EnableAnalytics && _analytics.IsEnabled) return true;
            FirebaseLog.Warning("Analytics chưa sẵn sàng.");
            return false;
        }

        private bool ReadyCrashlytics()
        {
            if (IsInitialized && _config != null && _config.EnableCrashlytics && _crashlytics.IsEnabled) return true;
            FirebaseLog.Warning("Crashlytics chưa sẵn sàng.");
            return false;
        }

        private bool ReadyAuth()
        {
            if (IsInitialized && _config != null && _config.EnableAuth && _auth.IsListening) return true;
            FirebaseLog.Warning("Auth chưa sẵn sàng.");
            return false;
        }

        private bool ReadyRemoteConfig()
        {
            if (IsInitialized && _config != null && _config.EnableRemoteConfig && _remoteConfig.IsReady) return true;
            FirebaseLog.Warning("Remote Config chưa sẵn sàng.");
            return false;
        }

        private bool ReadyMessaging()
        {
            if (IsInitialized && _config != null && _config.EnableMessaging && _messaging.IsListening) return true;
            FirebaseLog.Warning("Messaging chưa sẵn sàng.");
            return false;
        }
    }
}

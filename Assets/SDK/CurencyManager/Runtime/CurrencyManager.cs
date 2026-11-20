// Assets/SDK/CurencyManager/Runtime/CurrencyManager.cs
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using LSDK.Core.Base;
using LSDK.Core.ObjectPools;
using LSDK.GameData;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>
    /// Ví tiền. Loại nào bật hồi trong catalog thì tự cộng theo thời gian,
    /// cùng luật với heart: một mốc, trần, unlimited chặn tiêu.
    /// Vòng hồi ngủ tới mốc đơn vị kế tiếp, không poll cố định. Không có gì đang hồi thì vòng dừng hẳn.
    /// Gọi <see cref="Refresh(CurrencyType)"/> trước khi đọc số để chặn chơi.
    /// <see cref="Add(CurrencyType, long, Transform)"/> cộng số dư và chơi animation nhận.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CurrencyManager : SingletonBehaviour<CurrencyManager>
    {
        const float MinSleepSeconds = 0.5f;

        [Tooltip("Bật thì manager tự cộng đơn vị hồi khi tới hạn và bắn AmountChanged.")]
        [SerializeField] private bool _autoRefresh = true;

        [Tooltip("Giới hạn trên mỗi lần ngủ của vòng hồi, giây. Vòng ngủ tới mốc kế tiếp nhưng không quá giá trị này, để theo kịp khi người chơi đổi giờ máy.")]
        [SerializeField, Min(1f)] private float _maxSleepSeconds = 30f;

        /// <summary>Bắn khi số dư đổi. Đổi riêng lịch hồi thì không bắn.</summary>
        public event Action<CurrencyAmountChanged> AmountChanged;

        CurrencyCatalog _catalog;
        CurrencyData _data;
        Coroutine _refreshRoutine;
        WaitForSecondsRealtime _wait;
        long _sleepingDue;
        int _lastResumeFrame = -1;
        bool _ready;
        bool _inRefresh;
        bool _dataFailed;
        bool _loggedResolve;
        bool _warnedPool;
        bool _warnedSprite;

        // Chỉ tạo khi có lần nhận có animation. Một dòng cho mỗi loại đang đếm.
        Dictionary<int, CountRun> _counts;
        Dictionary<int, Tween> _punches;
        List<CurrencyFlyIcon> _flying;

        /// <summary>Tạo manager sau scene đầu để vòng hồi chạy khi không có object trên scene.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            _ = Instance;
        }

        /// <summary>
        /// Gắn catalog và data. Seed và bắt kịp offline đã chạy trong <see cref="CurrencyData.OnLoaded"/>.
        /// </summary>
        protected override void OnInitialize()
        {
            _ready = true;
            _catalog = CurrencyCatalog.Load();
            BindData();

            if (_catalog == null)
                CurrencyLog.Warning("Không thấy CurrencyCatalog. Tools → LSDK → Currency → Create Catalog.");
        }

        void OnEnable()
        {
            if (!_ready) return;
            ScheduleLoop(true);
        }

        void OnDisable()
        {
            StopRefreshLoop();
        }

        // Quay lại app thường bắn cả Pause(false) lẫn Focus(true) cùng frame: chỉ xử lý một lần.
        void OnApplicationPause(bool paused)
        {
            if (!paused) Resume();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) Resume();
        }

        /// <summary>Dừng vòng hồi và bỏ đăng ký data khi object bị hủy.</summary>
        protected override void OnDestroy()
        {
            StopRefreshLoop();
            StopReceive();
            if (_data != null) _data.Changed -= OnDataChanged;
            base.OnDestroy();
        }

        #region Refresh
        /// <summary>Bắt kịp một loại theo giờ UTC hiện tại.</summary>
        public void Refresh(CurrencyType type)
        {
            Refresh(type, CurrencyClock.UtcNow());
        }

        /// <summary>Bắt kịp một loại theo mốc Unix UTC giây.</summary>
        public void Refresh(CurrencyType type, long now)
        {
            if (!TryGet(type, out var def, out var record, true)) return;
            RefreshDefinition(def, record, now);
        }

        /// <summary>Bắt kịp mọi loại trong catalog.</summary>
        public void RefreshAll()
        {
            RefreshAll(CurrencyClock.UtcNow());
        }

        /// <summary>Bắt kịp mọi loại theo mốc Unix UTC giây.</summary>
        public void RefreshAll(long now)
        {
            if (!EnsureLoaded(true)) return;
            var definitions = _catalog.Definitions;
            if (definitions == null) return;

            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null) continue;
                RefreshDefinition(def, _data.GetOrCreate(def.Id), now);
            }
        }

        void RefreshDefinition(CurrencyDefinition def, CurrencyRecord record, long now)
        {
            bool seeded = Prepare(def, record, out long previous);
            bool refreshed = RegenWallet.Refresh(record, def, now, out _);
            Commit((CurrencyType)def.Id, previous, record.Amount, seeded || refreshed);
        }
        #endregion

        #region Read
        /// <summary>Số dư đã lưu. Không tự bắt kịp thời gian.</summary>
        public long GetAmount(CurrencyType type)
        {
            if (!TryFind(type, out _, out var record)) return 0;
            return record.Amount;
        }

        /// <summary>Đọc toàn bộ trạng thái một loại bằng một lần tra cứu, giờ UTC hiện tại. Dùng cho UI.</summary>
        public bool TryGetState(CurrencyType type, out CurrencyState state)
        {
            return TryGetState(type, CurrencyClock.UtcNow(), out state);
        }

        /// <summary>Đọc toàn bộ trạng thái một loại tại mốc Unix UTC giây.</summary>
        public bool TryGetState(CurrencyType type, long now, out CurrencyState state)
        {
            state = default;
            if (!TryFind(type, out var def, out var record)) return false;

            bool regen = def.UseRegen;
            state = new CurrencyState(
                type,
                record.Amount,
                regen,
                regen ? def.MaxAmount : 0,
                regen && record.Amount >= def.MaxAmount,
                RegenWallet.RegenRemaining(record, def, now),
                def.AllowUnlimited ? RegenWallet.UnlimitedRemaining(record, now) : 0);
            return true;
        }

        /// <summary>true khi trừ được <paramref name="cost"/> ở thời điểm hiện tại. Không đổi số dư.</summary>
        public bool CanSpend(CurrencyType type, long cost)
        {
            return CanSpend(type, cost, CurrencyClock.UtcNow());
        }

        /// <summary>true khi trừ được <paramref name="cost"/> tại mốc đã cho. Không đổi số dư.</summary>
        public bool CanSpend(CurrencyType type, long cost, long now)
        {
            if (cost <= 0) return false;
            if (!TryFind(type, out var def, out var record)) return false;
            if (now < 0) now = 0;
            if (def.AllowUnlimited && RegenWallet.UnlimitedRemaining(record, now) > 0) return false;
            return record.Amount >= cost;
        }

        /// <summary>true khi loại này có hồi và số dư đã chạm trần.</summary>
        public bool IsFull(CurrencyType type)
        {
            if (!TryFind(type, out var def, out var record) || !def.UseRegen) return false;
            return record.Amount >= def.MaxAmount;
        }

        /// <summary>true khi catalog bật hồi cho loại này.</summary>
        public bool UsesRegen(CurrencyType type)
        {
            if (!TryDef(type, out var def)) return false;
            return def.UseRegen;
        }

        /// <summary>Trần hồi. 0 nếu loại này không hồi hoặc chưa có định nghĩa.</summary>
        public long GetMax(CurrencyType type)
        {
            if (!TryDef(type, out var def) || !def.UseRegen) return 0;
            return def.MaxAmount;
        }

        /// <summary>Chu kỳ hồi, giây. 0 nếu loại này không hồi.</summary>
        public int GetRegenIntervalSeconds(CurrencyType type)
        {
            if (!TryDef(type, out var def) || !def.UseRegen) return 0;
            return def.RegenIntervalSeconds;
        }

        /// <summary>Unix UTC giây của đơn vị kế tiếp. 0 nếu không có lịch.</summary>
        public int GetNextRegenUnix(CurrencyType type)
        {
            if (!TryFind(type, out _, out var record)) return 0;
            return record.NextRegenUnix;
        }

        /// <summary>Giây còn lại tới đơn vị kế tiếp. 0 khi đầy, chưa có lịch, hoặc đã tới mốc.</summary>
        public int GetRegenRemainingSeconds(CurrencyType type)
        {
            return GetRegenRemainingSeconds(type, CurrencyClock.UtcNow());
        }

        /// <summary>Giây còn lại tới đơn vị kế tiếp tại mốc Unix UTC giây.</summary>
        public int GetRegenRemainingSeconds(CurrencyType type, long now)
        {
            if (!TryFind(type, out var def, out var record)) return 0;
            return RegenWallet.RegenRemaining(record, def, now);
        }

        /// <summary>true khi unlimited còn hạn.</summary>
        public bool IsUnlimitedActive(CurrencyType type)
        {
            return IsUnlimitedActive(type, CurrencyClock.UtcNow());
        }

        /// <summary>true khi unlimited còn hạn tại mốc Unix UTC giây.</summary>
        public bool IsUnlimitedActive(CurrencyType type, long now)
        {
            return GetUnlimitedRemainingSeconds(type, now) > 0;
        }

        /// <summary>Giây unlimited còn lại. 0 khi hết hạn hoặc loại này không bật unlimited.</summary>
        public long GetUnlimitedRemainingSeconds(CurrencyType type)
        {
            return GetUnlimitedRemainingSeconds(type, CurrencyClock.UtcNow());
        }

        /// <summary>Giây unlimited còn lại tại mốc Unix UTC giây.</summary>
        public long GetUnlimitedRemainingSeconds(CurrencyType type, long now)
        {
            if (!TryFind(type, out var def, out var record) || !def.AllowUnlimited) return 0;
            return RegenWallet.UnlimitedRemaining(record, now);
        }
        #endregion

        #region Write
        /// <summary>Ghi số dư theo luật setter, giờ UTC hiện tại.</summary>
        public void Set(CurrencyType type, long quantity)
        {
            Set(type, quantity, CurrencyClock.UtcNow());
        }

        /// <summary>Ghi số dư theo luật setter tại mốc Unix UTC giây.</summary>
        public void Set(CurrencyType type, long quantity, long now)
        {
            if (!TryGet(type, out var def, out var record, true)) return;
            bool seeded = Prepare(def, record, out long previous);
            bool applied = RegenWallet.ApplyAmount(record, def, quantity, now, out _);
            Commit(type, previous, record.Amount, seeded || applied);
        }

        /// <summary>Cộng số dư. Số không dương bị bỏ qua. Trừ bằng <see cref="TrySpend(CurrencyType, long)"/>.</summary>
        public void Add(CurrencyType type, long amount)
        {
            Add(type, amount, CurrencyClock.UtcNow());
        }

        /// <summary>
        /// Cộng số dư và chơi animation. Icon bay từ <paramref name="origin"/> tới ô cùng loại.
        /// Null thì bay từ giữa màn hình. Chữ bắt đầu đếm khi icon đầu tiên chạm ô.
        /// Không thấy ô đang mở, hoặc không spawn được icon, thì cộng và hiện số mới ngay.
        /// </summary>
        public void Add(CurrencyType type, long amount, Transform origin)
        {
            if (amount <= 0) return;
            var config = CurrencyReceiveConfig.Load();
            if (!CurrencyItem.TryFind(type, out var item))
            {
                CurrencyLog.Warning("Không thấy CurrencyItem đang bật cho " + type + ". Đã cộng số dư, không chơi animation. Hãy mở CurrencyPanel.");
                Add(type, amount);
                return;
            }

            long from = item.IsHolding ? item.ShownAmount : GetAmount(type);
            item.BeginHold(from);
            Add(type, amount);
            long to = GetAmount(type);
            if (to <= from)
            {
                item.EndHold();
                return;
            }

            QueueCount(item, type, from, to);
            if (Spawn(item, type, to - from, origin, config) <= 0)
                StartCount(item, type, from, to, config);
        }

        /// <summary>Cộng số dư tại mốc Unix UTC giây.</summary>
        public void Add(CurrencyType type, long amount, long now)
        {
            if (amount <= 0) return;
            if (!TryGet(type, out var def, out var record, true)) return;
            bool seeded = Prepare(def, record, out long previous);
            long current = record.Amount;
            long next = current > long.MaxValue - amount ? long.MaxValue : current + amount;
            bool applied = RegenWallet.ApplyAmount(record, def, next, now, out _);
            Commit(type, previous, record.Amount, seeded || applied);
        }

        /// <summary>
        /// Trừ số dư. Đang unlimited, không đủ, hoặc không tìm thấy loại thì giữ nguyên và trả false.
        /// </summary>
        public bool TrySpend(CurrencyType type, long cost)
        {
            return TrySpend(type, cost, CurrencyClock.UtcNow());
        }

        /// <summary>Trừ số dư tại mốc Unix UTC giây.</summary>
        public bool TrySpend(CurrencyType type, long cost, long now)
        {
            if (cost <= 0) return false;
            if (!TryGet(type, out var def, out var record, true)) return false;
            bool seeded = Prepare(def, record, out long previous);
            if (now < 0) now = 0;

            bool spent = false;
            bool applied = false;
            bool blocked = def.AllowUnlimited && RegenWallet.UnlimitedRemaining(record, now) > 0;
            if (!blocked && record.Amount >= cost)
                applied = RegenWallet.ApplyAmount(record, def, record.Amount - cost, now, out spent);

            Commit(type, previous, record.Amount, seeded || applied);
            return spent;
        }

        /// <summary>Cộng giây unlimited. Tham số là giây, cộng dồn từ mốc còn hạn hoặc từ bây giờ.</summary>
        public void AddUnlimitedSeconds(CurrencyType type, long seconds)
        {
            AddUnlimitedSeconds(type, seconds, CurrencyClock.UtcNow());
        }

        /// <summary>Cộng giây unlimited tại mốc Unix UTC giây.</summary>
        public void AddUnlimitedSeconds(CurrencyType type, long seconds, long now)
        {
            if (seconds <= 0) return;
            if (!TryGet(type, out var def, out var record, true)) return;
            if (!def.AllowUnlimited)
            {
                CurrencyLog.Warning(type + " không bật unlimited trong catalog.");
                return;
            }

            bool seeded = Prepare(def, record, out long previous);
            bool applied = RegenWallet.AddUnlimited(record, def, seconds, now, out _);
            Commit(type, previous, record.Amount, seeded || applied);
        }

        // Seed im lặng, trả số dư trước khi seed để Commit gộp mọi thay đổi thành một lần Save.
        static bool Prepare(CurrencyDefinition def, CurrencyRecord record, out long previous)
        {
            previous = record.Amount;
            return RegenWallet.Seed(record, def);
        }

        void Commit(CurrencyType type, long previous, long current, bool changed)
        {
            if (!changed || _data == null) return;
            _data.Save();
            if (previous == current) return;
            AmountChanged?.Invoke(new CurrencyAmountChanged(type, previous, current));
        }
        #endregion

        #region Lookup
        bool TryDef(CurrencyType type, out CurrencyDefinition def)
        {
            def = null;
            if (_catalog == null) _catalog = CurrencyCatalog.Load();
            if (_catalog == null) return false;
            return _catalog.TryGet(type, out def);
        }

        bool TryFind(CurrencyType type, out CurrencyDefinition def, out CurrencyRecord record)
        {
            record = null;
            if (!TryDef(type, out def)) return false;
            if (!EnsureLoaded(false)) return false;
            return _data.TryGet(def.Id, out record);
        }

        bool TryGet(CurrencyType type, out CurrencyDefinition def, out CurrencyRecord record, bool log)
        {
            def = null;
            record = null;
            if (!EnsureLoaded(log)) return false;
            if (!_catalog.TryGet(type, out def))
            {
                if (log) CurrencyLog.Error("Không có định nghĩa cho " + type + " trong CurrencyCatalog.");
                return false;
            }

            record = _data.GetOrCreate(def.Id);
            return record != null;
        }

        bool EnsureLoaded(bool log)
        {
            if (_catalog == null) _catalog = CurrencyCatalog.Load();
            if (_data == null && !_dataFailed) BindData();

            bool ready = _catalog != null && _data != null;
            if (!ready && log && !_loggedResolve)
            {
                _loggedResolve = true;
                CurrencyLog.Error("CurrencyCatalog hoặc CurrencyData chưa sẵn sàng.");
            }
            return ready;
        }

        bool BindData()
        {
            if (_data != null) return true;

            var dataManager = GameDataManager.Instance;
            if (dataManager == null) return false;

            _data = dataManager.Get<CurrencyData>();
            if (_data == null)
            {
                _dataFailed = true;
                return false;
            }

            _data.Changed += OnDataChanged;
            return true;
        }
        #endregion

        #region Loop
        void Resume()
        {
            if (!_ready || _lastResumeFrame == Time.frameCount) return;
            _lastResumeFrame = Time.frameCount;

            if (EnsureLoaded(false)) RefreshAllGuarded(CurrencyClock.UtcNow());
            ScheduleLoop(true);
        }

        // Data đổi từ ngoài vòng hồi (chi tiêu, thưởng, editor): lịch hồi có thể đã đổi.
        void OnDataChanged(GameDataModule module)
        {
            if (_inRefresh) return;
            ScheduleLoop(false);
        }

        void ScheduleLoop(bool force)
        {
            if (!_ready || !_autoRefresh || !isActiveAndEnabled) return;
            if (!EnsureLoaded(false)) return;

            long due = NextDueUnix(CurrencyClock.UtcNow());
            if (!force && _refreshRoutine != null && due == _sleepingDue) return;

            StopRefreshLoop();
            if (due <= 0) return;
            _refreshRoutine = StartCoroutine(RefreshLoop());
        }

        // Mốc sớm nhất cần bắt kịp. 0 khi không có gì đang hồi.
        // Dưới trần mà chưa có lịch thì tính là tới hạn ngay, để Refresh đặt lịch.
        // Unlimited đang bật cũng là một mốc: tới hạn thì Refresh hạ cờ IsUnlimited.
        long NextDueUnix(long now)
        {
            if (_catalog == null || _data == null) return 0;
            var definitions = _catalog.Definitions;
            if (definitions == null) return 0;

            long due = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null) continue;
                if (!_data.TryGet(def.Id, out var record)) continue;

                if (def.UseRegen && record.Amount < def.MaxAmount)
                {
                    long next = record.NextRegenUnix > 0 ? record.NextRegenUnix : now;
                    if (due == 0 || next < due) due = next;
                }

                if (def.AllowUnlimited && record.IsUnlimited)
                {
                    long expire = record.UnlimitedUntilUnix;
                    if (expire < 1) expire = now;
                    if (due == 0 || expire < due) due = expire;
                }
            }
            return due;
        }

        void RefreshAllGuarded(long now)
        {
            _inRefresh = true;
            try
            {
                RefreshAll(now);
            }
            finally
            {
                _inRefresh = false;
            }
        }

        IEnumerator RefreshLoop()
        {
            if (_wait == null) _wait = new WaitForSecondsRealtime(MinSleepSeconds);

            while (true)
            {
                long due = NextDueUnix(CurrencyClock.UtcNow());
                if (due <= 0) break;

                _sleepingDue = due;
                float wait = Mathf.Clamp(due - CurrencyClock.UtcNow(), MinSleepSeconds, _maxSleepSeconds);
                _wait.waitTime = wait;
                _wait.Reset();
                yield return _wait;

                try
                {
                    RefreshAllGuarded(CurrencyClock.UtcNow());
                }
                catch (Exception e)
                {
                    // Subscriber của AmountChanged ném lỗi không được giết vòng hồi.
                    Debug.LogException(e);
                }
            }

            _refreshRoutine = null;
            _sleepingDue = 0;
        }

        void StopRefreshLoop()
        {
            _sleepingDue = 0;
            if (_refreshRoutine == null) return;
            StopCoroutine(_refreshRoutine);
            _refreshRoutine = null;
            if (_wait != null) _wait.Reset();
        }
        #endregion

        #region Receive
        struct CountRun
        {
            public Tween Tween;
            public CurrencyItem Item;
            public long From;
            public long To;
        }

        // Giữ số cũ. Chưa chạy chữ. Icon đầu tiên tới nơi mới gọi StartCount.
        void QueueCount(CurrencyItem item, CurrencyType type, long from, long to)
        {
            if (_counts == null) _counts = new Dictionary<int, CountRun>(4);
            int key = (int)type;
            if (_counts.TryGetValue(key, out var running) && running.Tween != null && running.Tween.IsActive())
                running.Tween.Kill(false);
            _counts[key] = new CountRun { Item = item, From = from, To = to };
        }

        void TryBeginCount(CurrencyItem item)
        {
            if (item == null || _counts == null) return;
            int key = (int)item.Type;
            if (!_counts.TryGetValue(key, out var run) || run.Tween != null) return;
            StartCount(item, item.Type, run.From, run.To, CurrencyReceiveConfig.Load());
        }

        void StartCount(CurrencyItem item, CurrencyType type, long from, long to, CurrencyReceiveConfig config)
        {
            if (_counts == null) _counts = new Dictionary<int, CountRun>(4);
            int key = (int)type;

            if (config.CountDuration <= 0f)
            {
                item.EndHold();
                _counts.Remove(key);
                return;
            }

            if (_counts.TryGetValue(key, out var running) && running.Tween != null && running.Tween.IsActive())
                running.Tween.Kill(false);

            float progress = 0f;
            var tween = DOTween.To(() => progress, value =>
            {
                progress = value;
                if (item == null) return;
                item.SetShown(Lerp(from, to, value));
            }, 1f, config.CountDuration).SetEase(config.CountEase).SetUpdate(config.UseUnscaledTime).SetTarget(item);
            tween.SetLink(item.gameObject, LinkBehaviour.KillOnDestroy);
            tween.OnComplete(() => FinishCount(key, tween, item));
            _counts[key] = new CountRun { Tween = tween, Item = item, From = from, To = to };
        }

        void FinishCount(int key, Tween tween, CurrencyItem item)
        {
            if (_counts == null || !_counts.TryGetValue(key, out var run) || run.Tween != tween) return;
            _counts.Remove(key);
            if (item != null) item.EndHold();
        }

        int Spawn(CurrencyItem item, CurrencyType type, long gained, Transform origin, CurrencyReceiveConfig config)
        {
            var canvas = item.GetComponentInParent<Canvas>();
            var layer = canvas != null ? canvas.transform as RectTransform : null;
            if (layer == null)
            {
                CurrencyLog.Warning(type + " không nằm trên Canvas. Không spawn icon.");
                return 0;
            }

            var pool = PoolServer.HasInstance ? PoolServer.Instance : null;
            if (pool == null || config.PoolId <= 0 || !pool.HasPool(config.PoolId))
            {
                WarnOnce(ref _warnedPool, "Chưa gắn pool icon. Chạy Tools → LSDK → Currency → Create Receive Setup, và scene phải có PoolServer.");
                return 0;
            }

            Camera layerCamera = CurrencyFlyIcon.CameraOf(layer);
            Transform start = origin;
            Camera originCamera = null;
            if (origin != null)
            {
                originCamera = CurrencyFlyIcon.CameraOf(origin);
                if (origin.GetComponentInParent<Canvas>() == null && originCamera == null)
                {
                    CurrencyLog.Warning("Không có Camera.main. Icon bay từ giữa màn hình.");
                    start = null;
                }
            }

            if (!CurrencyFlyIcon.TryToLocal(layer, layerCamera, start, originCamera, out var from))
                return 0;

            var target = item.FlyTarget;
            Camera targetCamera = CurrencyFlyIcon.CameraOf(target);
            Sprite sprite = null;
            if (TryDef(type, out var def) && def != null) sprite = def.Icon;
            if (sprite == null) sprite = item.DisplayedSprite;
            if (sprite == null)
                WarnOnce(ref _warnedSprite, type + " chưa có sprite. Gán Icon trong CurrencyCatalog, hoặc gán sprite trên Image của CurrencyItem.");

            int count = config.IconCount(gained);
            if (_flying == null) _flying = new List<CurrencyFlyIcon>(count);
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                var go = pool.GetObject(config.PoolId);
                if (go == null) return spawned;

                var icon = go.GetComponent<CurrencyFlyIcon>();
                if (icon == null)
                {
                    CurrencyLog.Error("Prefab pool #" + config.PoolId + " không có CurrencyFlyIcon.");
                    pool.ReturnObject(go);
                    return spawned;
                }

                _flying.Add(icon);
                icon.Launch(sprite, layer, layerCamera, from, target, targetCamera, config, i, this, item);
                spawned++;
            }

            return spawned;
        }

        /// <summary>Icon vừa tới ô. Gọi từ <see cref="CurrencyFlyIcon"/> trước khi object về pool.</summary>
        internal void NotifyIconArrived(CurrencyFlyIcon icon, CurrencyItem item)
        {
            if (_flying != null)
            {
                int index = _flying.IndexOf(icon);
                if (index >= 0)
                {
                    int last = _flying.Count - 1;
                    _flying[index] = _flying[last];
                    _flying.RemoveAt(last);
                }
            }

            if (item == null) return;
            TryBeginCount(item);
            Punch(item);
        }

        void Punch(CurrencyItem item)
        {
            var config = CurrencyReceiveConfig.Load();
            float extra = config.PunchScale - 1f;
            if (extra <= 0f || config.PunchDuration <= 0f) return;

            var rt = item.FlyTarget;
            if (rt == null) return;

            if (_punches == null) _punches = new Dictionary<int, Tween>(4);
#if UNITY_6000_3_OR_NEWER
            int id = item.GetEntityId().GetHashCode();
#else
            int id = item.GetInstanceID();
#endif
            if (_punches.TryGetValue(id, out var old) && old != null && old.IsActive())
                old.Kill(false);

            float progress = 0f;
            var tween = DOTween.To(() => progress, value =>
            {
                progress = value;
                if (rt == null) return;
                float scale = 1f + Mathf.Sin(value * Mathf.PI) * extra;
                rt.localScale = new Vector3(scale, scale, 1f);
            }, 1f, config.PunchDuration).SetEase(Ease.Linear).SetUpdate(config.UseUnscaledTime).SetTarget(rt);
            tween.OnComplete(() =>
            {
                if (_punches != null && _punches.TryGetValue(id, out var current) && current == tween)
                    _punches.Remove(id);
            });
            tween.OnKill(() =>
            {
                if (rt != null) rt.localScale = Vector3.one;
            });
            _punches[id] = tween;
        }

        void StopReceive()
        {
            if (_counts != null)
            {
                foreach (var pair in _counts)
                {
                    var run = pair.Value;
                    if (run.Tween != null && run.Tween.IsActive()) run.Tween.Kill(false);
                    if (run.Item != null) run.Item.EndHold();
                }
                _counts.Clear();
            }

            var pool = PoolServer.HasInstance ? PoolServer.Instance : null;
            if (_flying != null)
            {
                for (int i = _flying.Count - 1; i >= 0; i--)
                {
                    var icon = _flying[i];
                    if (icon != null && pool != null) pool.ReturnObject(icon.gameObject);
                }
                _flying.Clear();
            }

            if (_punches != null)
            {
                foreach (var pair in _punches)
                {
                    if (pair.Value != null && pair.Value.IsActive()) pair.Value.Kill(false);
                }
                _punches.Clear();
            }
        }

        static void WarnOnce(ref bool warned, string message)
        {
            if (warned) return;
            warned = true;
            CurrencyLog.Warning(message);
        }

        static long Lerp(long from, long to, float t)
        {
            if (t <= 0f) return from;
            if (t >= 1f) return to;
            return from + (long)Math.Round((to - from) * (double)t);
        }
        #endregion
    }
}

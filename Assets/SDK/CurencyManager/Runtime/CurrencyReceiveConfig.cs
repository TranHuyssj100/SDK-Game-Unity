// Assets/SDK/CurencyManager/Runtime/CurrencyReceiveConfig.cs
using DG.Tweening;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>
    /// Thông số animation nhận tiền. Một asset dùng chung mọi loại.
    /// Đặt trong Resources tại <c>LSDK/Currency/CurrencyReceiveConfig</c>.
    /// Tạo bằng Tools → LSDK → Currency → Create Receive Setup.
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyReceiveConfig", menuName = "LSDK/Currency/Receive Config", order = 1)]
    public sealed class CurrencyReceiveConfig : ScriptableObject
    {
        /// <summary>Đường dẫn Resources, không gồm đuôi asset.</summary>
        public const string ResourcePath = "LSDK/Currency/CurrencyReceiveConfig";

        static CurrencyReceiveConfig _cached;
        static CurrencyReceiveConfig _fallback;
        static bool _warned;

        [Header("Bay")]
        [Tooltip("Giây icon nổ ra quanh điểm gọi trước khi bay về ô tiền.")]
        [SerializeField, Min(0f)] private float _burstDuration = 0.22f;

        [Tooltip("Giây bay từ điểm nổ tới ô tiền.")]
        [SerializeField, Min(0.05f)] private float _flyDuration = 0.55f;

        [Tooltip("Giây lệch giữa hai icon liên tiếp.")]
        [SerializeField, Min(0f)] private float _stagger = 0.045f;

        [Tooltip("Bán kính nổ, đơn vị local của Canvas.")]
        [SerializeField, Min(0f)] private float _spread = 90f;

        [Tooltip("Độ cong của đường bay. 0 là đường thẳng.")]
        [SerializeField, Min(0f)] private float _arc = 140f;

        [SerializeField] private Ease _flyEase = Ease.InCubic;

        [Header("Số trên ô")]
        [Tooltip("Giây chữ đếm từ số cũ tới số mới.")]
        [SerializeField, Min(0f)] private float _countDuration = 0.7f;

        [SerializeField] private Ease _countEase = Ease.OutQuad;

        [Header("Số icon")]
        [Tooltip("Ít nhất bấy nhiêu icon khi có nhận.")]
        [SerializeField, Min(1)] private int _minIcons = 1;

        [Tooltip("Nhiều nhất bấy nhiêu icon, dù số nhận lớn hơn.")]
        [SerializeField, Min(1)] private int _maxIcons = 10;

        [Tooltip("Mỗi icon đại diện bấy nhiêu đơn vị. 100 với 10 thì 10 icon, rồi bị kẹp bởi Max Icons.")]
        [SerializeField] private long _amountPerIcon = 1;

        [Tooltip("Cạnh icon bay, pixel Canvas.")]
        [SerializeField, Min(8f)] private float _iconSize = 72f;

        [Header("Khi icon tới")]
        [Tooltip("Scale đỉnh của ô tiền khi một icon tới. 1 là không nhún.")]
        [SerializeField, Min(1f)] private float _punchScale = 1.15f;

        [SerializeField, Min(0f)] private float _punchDuration = 0.18f;

        [Header("Pool")]
        [Tooltip("Id prefab CurrencyFlyIcon trong PoolDatabase. 0 thì không spawn.")]
        [SerializeField] private int _poolId;

        [SerializeField] private bool _useUnscaledTime = true;

        /// <summary>Giây nổ ra trước khi bay.</summary>
        public float BurstDuration => _burstDuration < 0f ? 0f : _burstDuration;

        /// <summary>Giây bay về ô tiền.</summary>
        public float FlyDuration => _flyDuration < 0.05f ? 0.05f : _flyDuration;

        /// <summary>Độ lệch thời gian giữa các icon.</summary>
        public float Stagger => _stagger < 0f ? 0f : _stagger;

        /// <summary>Bán kính nổ.</summary>
        public float Spread => _spread < 0f ? 0f : _spread;

        /// <summary>Độ cong đường bay.</summary>
        public float Arc => _arc < 0f ? 0f : _arc;

        /// <summary>Ease của đoạn bay về.</summary>
        public Ease FlyEase => _flyEase;

        /// <summary>Giây chữ chạy từ số cũ tới số mới.</summary>
        public float CountDuration => _countDuration < 0f ? 0f : _countDuration;

        /// <summary>Ease của chữ đếm.</summary>
        public Ease CountEase => _countEase;

        /// <summary>Cạnh icon bay.</summary>
        public float IconSize => _iconSize < 8f ? 8f : _iconSize;

        /// <summary>Scale đỉnh khi icon chạm ô.</summary>
        public float PunchScale => _punchScale < 1f ? 1f : _punchScale;

        /// <summary>Giây nhún ô tiền.</summary>
        public float PunchDuration => _punchDuration < 0f ? 0f : _punchDuration;

        /// <summary>Id pool của prefab bay. 0 là chưa gắn.</summary>
        public int PoolId => _poolId;

        /// <summary>true thì animation chạy khi timescale = 0.</summary>
        public bool UseUnscaledTime => _useUnscaledTime;

        /// <summary>Số icon spawn cho một lần nhận. Bị kẹp bởi min và max.</summary>
        public int IconCount(long amount)
        {
            if (amount <= 0) return 0;

            int min = _minIcons < 1 ? 1 : _minIcons;
            int max = _maxIcons < 1 ? 1 : _maxIcons;
            if (min > max) min = max;

            long per = _amountPerIcon < 1 ? 1 : _amountPerIcon;
            long count = amount / per;
            if (amount % per != 0) count++;
            if (count < min) count = min;
            if (count > max) count = max;
            return (int)count;
        }

        /// <summary>Load asset Resources. Thiếu asset thì dùng bản mặc định tạm, không ghi ra đĩa.</summary>
        public static CurrencyReceiveConfig Load()
        {
            if (_cached != null) return _cached;

            _cached = Resources.Load<CurrencyReceiveConfig>(ResourcePath);
            if (_cached != null) return _cached;

            if (!_warned)
            {
                _warned = true;
                CurrencyLog.Warning("Chưa có CurrencyReceiveConfig. Tools → LSDK → Currency → Create Receive Setup. Đang dùng thông số mặc định.");
            }

            if (_fallback == null)
            {
                _fallback = CreateInstance<CurrencyReceiveConfig>();
                _fallback.hideFlags = HideFlags.HideAndDontSave;
            }
            return _fallback;
        }

        /// <summary>Xóa cache. Gọi sau khi tạo asset trong Editor.</summary>
        public static void ClearCache()
        {
            _cached = null;
            _warned = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            _cached = null;
            _fallback = null;
            _warned = false;
        }
    }
}

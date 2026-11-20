// Assets/SDK/CurencyManager/Runtime/Generate/Code/CurrencyItem.cs
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LSDK.Currency
{
    /// <summary>
    /// Một ô trên thanh tiền. Hiện số dư của một <see cref="CurrencyType"/>.
    /// Sprite lấy từ <see cref="CurrencyDefinition.Icon"/>. Catalog trống thì giữ sprite đang gán trên Image.
    /// Khi <see cref="CurrencyManager.Add(CurrencyType, long, Transform)"/> đang đếm, chữ không bị <see cref="Paint"/> ghi đè.
    /// </summary>
    public sealed class CurrencyItem : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("currencyType")]
        private CurrencyType _currencyType;

        [SerializeField] private TMP_Text _amountLabel;

        [Tooltip("Ảnh icon của ô. Sprite do catalog ghi đè khi catalog có Icon.")]
        [SerializeField] private Image _icon;

        CurrencyManager _wallet;
        bool _painted;
        long _paintedAmount;
        long _paintedMax;
        int _paintedRemaining;
        bool _paintedUsesRegen;
        bool _holding;
        long _shown;
        Sprite _appliedIcon;

        /// <summary>Loại tiền ô này đang hiện.</summary>
        public CurrencyType Type => _currencyType;

        /// <summary>true khi chữ đang đếm từ số cũ tới số mới, chưa theo số dư thật.</summary>
        public bool IsHolding => _holding;

        /// <summary>Số đang hiện trên chữ. Khi không giữ thì bằng số dư thật.</summary>
        public long ShownAmount => _holding ? _shown : _paintedAmount;

        /// <summary>
        /// Sprite đang hiện. Catalog có Icon thì dùng Icon, không thì dùng sprite trên <see cref="Image"/>.
        /// Icon bay của pool lấy ảnh này khi catalog trống.
        /// </summary>
        public Sprite DisplayedSprite
        {
            get
            {
                var catalog = CurrencyCatalog.Load();
                if (catalog != null && catalog.TryGet(_currencyType, out var def) && def != null && def.Icon != null)
                    return def.Icon;
                return _icon != null ? _icon.sprite : null;
            }
        }

        /// <summary>Điểm icon bay đáp xuống. Ưu tiên Image icon, không có thì chính ô này.</summary>
        public RectTransform FlyTarget
        {
            get
            {
                if (_icon != null) return _icon.rectTransform;
                return transform as RectTransform;
            }
        }

        /// <summary>Ô đang bật, cùng loại, trên một CurrencyPanel. Không thấy thì false.</summary>
        public static bool TryFind(CurrencyType type, out CurrencyItem item)
        {
            var found = FindObjectsByType<CurrencyItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                var candidate = found[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.Type != type) continue;
                if (candidate.GetComponentInParent<CurrencyPanel>(true) == null) continue;
                item = candidate;
                return true;
            }

            item = null;
            return false;
        }

        void Awake()
        {
            if (_amountLabel == null)
                _amountLabel = GetComponentInChildren<TMP_Text>(true);
            if (_icon == null)
                _icon = FindIcon();
        }

        void OnEnable()
        {
            _wallet = CurrencyManager.Instance;
            _painted = false;
            _appliedIcon = null;
        }

        /// <summary>Giữ chữ ở <paramref name="shown"/> cho tới khi đếm xong. Gọi trước khi cộng số dư.</summary>
        internal void BeginHold(long shown)
        {
            _holding = true;
            _shown = shown;
            Write(shown);
        }

        /// <summary>Thả chữ về số dư thật.</summary>
        internal void EndHold()
        {
            if (!_holding) return;
            _holding = false;
            _painted = false;
            Paint();
        }

        /// <summary>Đặt số đang đếm. Chỉ có tác dụng khi đang giữ.</summary>
        internal void SetShown(long amount)
        {
            if (!_holding || amount == _shown) return;
            _shown = amount;
            Write(amount);
        }

        /// <summary>
        /// Vẽ lại chữ nếu trạng thái đổi. Ví không được refresh ở đây, manager lo việc đó.
        /// Đang giữ chữ thì không ghi đè số đang đếm.
        /// Trả true khi ô còn đang chờ hồi (dưới trần, chưa unlimited) nên cần được vẽ lại mỗi giây.
        /// </summary>
        public bool Paint()
        {
            ApplyIcon();
            if (_amountLabel == null) return false;
            if (_wallet == null) _wallet = CurrencyManager.Instance;
            if (_wallet == null) return false;
            if (!_wallet.TryGetState(_currencyType, out var state)) return false;

            bool waiting = state.UsesRegen && !state.IsFull && !state.IsUnlimited;
            if (_holding) return waiting;

            bool counting = state.IsCountingDown;
            int remaining = counting ? state.RegenRemainingSeconds : 0;

            if (_painted
                && _paintedAmount == state.Amount
                && _paintedMax == state.Max
                && _paintedUsesRegen == state.UsesRegen
                && _paintedRemaining == remaining)
                return waiting;

            _painted = true;
            _paintedAmount = state.Amount;
            _paintedMax = state.Max;
            _paintedUsesRegen = state.UsesRegen;
            _paintedRemaining = remaining;

            _amountLabel.text = Build(state, counting, remaining);
            return waiting;
        }

        void Write(long amount)
        {
            if (_amountLabel == null) return;
            if (_wallet == null) _wallet = CurrencyManager.Instance;
            if (_wallet == null || !_wallet.TryGetState(_currencyType, out var state))
            {
                _amountLabel.text = amount.ToString();
                return;
            }

            bool full = state.UsesRegen && amount >= state.Max;
            int remaining = state.RegenRemainingSeconds;
            bool counting = state.UsesRegen && !full && !state.IsUnlimited && remaining > 0;
            var view = new CurrencyState(
                state.Type,
                amount,
                state.UsesRegen,
                state.Max,
                full,
                remaining,
                state.UnlimitedRemainingSeconds);
            _amountLabel.text = Build(view, counting, remaining);
        }

        void ApplyIcon()
        {
            if (_icon == null) _icon = FindIcon();
            if (_icon == null) return;

            var catalog = CurrencyCatalog.Load();
            if (catalog == null || !catalog.TryGet(_currencyType, out var def) || def == null || def.Icon == null)
                return;
            if (_appliedIcon == def.Icon && _icon.sprite == def.Icon) return;

            _appliedIcon = def.Icon;
            _icon.sprite = def.Icon;
            _icon.enabled = true;
            _icon.preserveAspect = true;
        }

        Image FindIcon()
        {
            var images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image.gameObject == gameObject) continue;
                if (image.gameObject.name == "Icon") return image;
            }

            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image != null && image.gameObject != gameObject) return image;
            }

            return null;
        }

        static string Build(CurrencyState state, bool counting, int remaining)
        {
            if (!state.UsesRegen)
                return state.Amount.ToString();

            string text = state.Amount + "/" + state.Max;
            if (!counting) return text;

            int minutes = remaining / 60;
            int seconds = remaining % 60;
            return text + "  " + minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }
}

// Assets/SDK/CurencyManager/Runtime/CurrencyDefinition.cs
using System;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>
    /// Một loại tiền trong catalog. Bật hồi phục thì ví có trần, một mốc hồi,
    /// và có thể bật unlimited. Tắt hồi phục thì chỉ là số dư sàn 0.
    /// </summary>
    [Serializable]
    public sealed class CurrencyDefinition
    {
        [Tooltip("Id ổn định. Trùng giá trị enum CurrencyType. Không đổi sau khi đã có save.")]
        [SerializeField] private int _id;

        [Tooltip("Tên enum. PascalCase, không dấu cách.")]
        [SerializeField] private string _name = "Coin";

        [Tooltip("Số dư lần đầu khi loại tiền này không hồi theo thời gian.")]
        [SerializeField] private long _initialAmount;

        [Tooltip("Bật thì số dư tự cộng theo thời gian tới trần.")]
        [SerializeField] private bool _useRegen;

        [Tooltip("Trần khi đang hồi. Amount có thể vượt trần nếu một lần cộng nhảy qua, rồi bị chặn tăng tiếp.")]
        [SerializeField, Min(0)] private long _maxAmount = 5;

        [Tooltip("Số giây cho một đơn vị hồi. Heart mẫu là 1800.")]
        [SerializeField, Min(1)] private int _regenIntervalSeconds = 1800;

        [Tooltip("Bật thì AddUnlimitedSeconds chặn mọi lần giảm trong thời hạn.")]
        [SerializeField] private bool _allowUnlimited;

        [Tooltip("Sprite của loại tiền. CurrencyItem và icon bay (pool) đều lấy ảnh này.")]
        [SerializeField] private Sprite _icon;

        /// <summary>Id lưu trong save và giá trị enum.</summary>
        public int Id => _id;

        /// <summary>Tên hiển thị và tên enum.</summary>
        public string Name => string.IsNullOrWhiteSpace(_name) ? "Currency" + _id : _name.Trim();

        /// <summary>Số dư khởi tạo của loại không hồi.</summary>
        public long InitialAmount => _initialAmount < 0 ? 0 : _initialAmount;

        /// <summary>true khi bật hồi, trần tối thiểu 1 và chu kỳ tối thiểu 1 giây.</summary>
        public bool UseRegen => _useRegen && _maxAmount >= 1 && _regenIntervalSeconds >= 1;

        /// <summary>Trần hồi. 0 nếu không dùng hồi.</summary>
        public long MaxAmount => _maxAmount;

        /// <summary>Chu kỳ hồi, giây. Tối thiểu 1 khi giá trị cấu hình nhỏ hơn 1.</summary>
        public int RegenIntervalSeconds => _regenIntervalSeconds < 1 ? 1 : _regenIntervalSeconds;

        /// <summary>true khi loại tiền này nhận buff unlimited.</summary>
        public bool AllowUnlimited => _allowUnlimited;

        /// <summary>Sprite hiển thị trên ô tiền và trên icon bay khi nhận.</summary>
        public Sprite Icon => _icon;
    }
}

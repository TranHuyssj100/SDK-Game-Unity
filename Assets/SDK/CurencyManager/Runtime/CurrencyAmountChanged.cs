// Assets/SDK/CurencyManager/Runtime/CurrencyAmountChanged.cs

namespace LSDK.Currency
{
    /// <summary>Số dư vừa đổi. Đổi riêng đồng hồ hồi không tạo sự kiện này.</summary>
    public readonly struct CurrencyAmountChanged
    {
        /// <summary>Loại tiền vừa đổi.</summary>
        public CurrencyType Type { get; }

        /// <summary>Số dư trước khi đổi.</summary>
        public long Previous { get; }

        /// <summary>Số dư sau khi đổi.</summary>
        public long Amount { get; }

        /// <summary>Tạo thông báo đổi số dư.</summary>
        public CurrencyAmountChanged(CurrencyType type, long previous, long amount)
        {
            Type = type;
            Previous = previous;
            Amount = amount;
        }
    }
}

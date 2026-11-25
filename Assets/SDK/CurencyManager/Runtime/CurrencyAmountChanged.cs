// Assets/SDK/CurencyManager/Runtime/CurrencyAmountChanged.cs

namespace LSDK.Currency
{
    /// <summary>Số dư vừa đổi. Đổi riêng đồng hồ hồi không tạo sự kiện này.</summary>
    public readonly struct CurrencyAmountChanged
    {
        /// <summary>Id loại tiền vừa đổi. Trùng giá trị enum <c>CurrencyType</c>.</summary>
        public int TypeId { get; }

        /// <summary>Số dư trước khi đổi.</summary>
        public long Previous { get; }

        /// <summary>Số dư sau khi đổi.</summary>
        public long Amount { get; }

        /// <summary>Tạo thông báo đổi số dư.</summary>
        public CurrencyAmountChanged(int typeId, long previous, long amount)
        {
            TypeId   = typeId;
            Previous = previous;
            Amount   = amount;
        }
    }
}

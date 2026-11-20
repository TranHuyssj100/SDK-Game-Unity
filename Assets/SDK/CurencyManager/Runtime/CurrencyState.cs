// Assets/SDK/CurencyManager/Runtime/CurrencyState.cs

namespace LSDK.Currency
{
    /// <summary>
    /// Ảnh chụp trạng thái một loại tiền tại một mốc thời gian.
    /// Đọc một lần qua <see cref="CurrencyManager.TryGetState(CurrencyType, out CurrencyState)"/>
    /// thay cho gọi nhiều hàm lẻ.
    /// </summary>
    public readonly struct CurrencyState
    {
        /// <summary>Loại tiền.</summary>
        public CurrencyType Type { get; }

        /// <summary>Số dư.</summary>
        public long Amount { get; }

        /// <summary>true khi catalog bật hồi cho loại này.</summary>
        public bool UsesRegen { get; }

        /// <summary>Trần hồi. 0 nếu không hồi.</summary>
        public long Max { get; }

        /// <summary>true khi có hồi và số dư đã chạm trần.</summary>
        public bool IsFull { get; }

        /// <summary>Giây còn lại tới đơn vị kế tiếp. 0 khi đầy, chưa có lịch, hoặc đã tới mốc.</summary>
        public int RegenRemainingSeconds { get; }

        /// <summary>Giây unlimited còn lại. 0 khi hết hạn hoặc không bật unlimited.</summary>
        public long UnlimitedRemainingSeconds { get; }

        /// <summary>true khi unlimited còn hạn.</summary>
        public bool IsUnlimited => UnlimitedRemainingSeconds > 0;

        /// <summary>true khi có đồng hồ đang đếm ngược cần hiển thị.</summary>
        public bool IsCountingDown => UsesRegen && !IsFull && !IsUnlimited && RegenRemainingSeconds > 0;

        /// <summary>Tạo ảnh chụp trạng thái.</summary>
        public CurrencyState(
            CurrencyType type,
            long amount,
            bool usesRegen,
            long max,
            bool isFull,
            int regenRemainingSeconds,
            long unlimitedRemainingSeconds)
        {
            Type = type;
            Amount = amount;
            UsesRegen = usesRegen;
            Max = max;
            IsFull = isFull;
            RegenRemainingSeconds = regenRemainingSeconds;
            UnlimitedRemainingSeconds = unlimitedRemainingSeconds;
        }
    }
}

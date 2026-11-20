// Assets/SDK/CurencyManager/Runtime/CurrencyClock.cs
using System;

namespace LSDK.Currency
{
    /// <summary>Mốc thời gian UTC dùng chung cho hồi phục và unlimited, tính bằng giây Unix.</summary>
    public static class CurrencyClock
    {
        /// <summary>Số giây Unix UTC hiện tại.</summary>
        public static long UtcNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}

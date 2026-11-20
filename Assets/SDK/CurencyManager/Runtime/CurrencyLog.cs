// Assets/SDK/CurencyManager/Runtime/CurrencyLog.cs
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>Log của module Currency. Prefix <c>[LSDK.Currency]</c>.</summary>
    public static class CurrencyLog
    {
        const string Tag = "[LSDK.Currency]";

        /// <summary>Log thông tin.</summary>
        public static void Info(string message)
        {
            Debug.Log(Tag + " " + message);
        }

        /// <summary>Log cảnh báo.</summary>
        public static void Warning(string message)
        {
            Debug.LogWarning(Tag + " " + message);
        }

        /// <summary>Log lỗi.</summary>
        public static void Error(string message)
        {
            Debug.LogError(Tag + " " + message);
        }
    }
}

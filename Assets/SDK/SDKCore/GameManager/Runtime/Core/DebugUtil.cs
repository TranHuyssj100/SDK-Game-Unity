// Assets/SDK/SDKCore/GameManager/Runtime/Core/DebugUtil.cs
using System;
using UnityEngine;

namespace LSDK.GameManager
{
    /// <summary>
    /// Log của GameManager. Tag tô màu giống Firebase:
    /// info xanh, warning vàng, error đỏ.
    /// </summary>
    public static class DebugUtil
    {
        private const string Tag = "[LSDK.GameManager]";

        /// <summary>Log thông tin. Tag màu xanh.</summary>
        public static void Info(string message)
        {
            Debug.Log("<color=green>" + Tag + "</color>" + " " + message);
        }

        /// <summary>Log cảnh báo. Tag màu vàng.</summary>
        public static void Warning(string message)
        {
            Debug.LogWarning("<color=yellow>" + Tag + "</color>" + " " + message);
        }

        /// <summary>Log lỗi. Tag màu đỏ.</summary>
        public static void Error(string message)
        {
            Debug.LogError("<color=red>" + Tag + "</color>" + " " + message);
        }

        /// <summary>Log exception kèm stack trace.</summary>
        public static void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}

using System;
using UnityEngine;

namespace LSDK.Mediation
{
    /// <summary>Log dùng chung. Thành công màu xanh, warning vàng, error đỏ.</summary>
    public static class DebugUtils
    {
        private const string Tag = "[Mediation]";

        public static void Success(string message)
        {
            Debug.Log(Format("green", message));
        }

        public static void Warning(string message)
        {
            Debug.Log(Format("yellow", message));
        }

        public static void Error(string message)
        {
            Debug.LogError(Format("red", message));
        }

        public static void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }

        private static string Format(string color, string message)
        {
            return $"<color={color}>{Tag} {message}</color>";
        }
    }
}

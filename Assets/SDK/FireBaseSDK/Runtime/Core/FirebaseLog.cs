using System;
using UnityEngine;

namespace LSDK.Firebase
{
    /// <summary>Log dùng chung, prefix [LSDK.Firebase].</summary>
    internal static class FirebaseLog
    {
        private const string Tag = "[LSDK.Firebase]";

        public static void Info(string message)
        {
            Debug.Log(Tag + " " + message);
        }

        public static void Warning(string message)
        {
            Debug.LogWarning(Tag + " " + message);
        }

        public static void Error(string message)
        {
            Debug.LogError(Tag + " " + message);
        }

        public static void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}

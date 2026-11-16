using System;
using UnityEngine;

namespace LSDK.Firebase
{
    /// <summary>Event toàn cục Remote Config. Nhớ unsubscribe ở OnDisable/OnDestroy.</summary>
    public static class RemoteConfigEvents
    {
        /// <summary>Bắn khi một module cụ thể vừa được Populate từ Firebase.</summary>
        public static event Action<RemoteConfigModule> ModuleUpdated;

        /// <summary>Bắn khi tất cả module đã được Populate lần đầu (sau FetchAndActivate).</summary>
        public static event Action AllUpdated;

        internal static void RaiseModuleUpdated(RemoteConfigModule m) { ModuleUpdated?.Invoke(m); }
        internal static void RaiseAllUpdated() { AllUpdated?.Invoke(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            ModuleUpdated = null;
            AllUpdated = null;
        }
    }
}

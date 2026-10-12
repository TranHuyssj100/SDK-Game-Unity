using System;
using UnityEngine;

namespace LSDK.GameData
{
    /// <summary>Event toàn cục. Nhớ unsubscribe ở OnDisable/OnDestroy.</summary>
    public static class DataEvents
    {
        public static event Action<GameDataModule> ModuleChanged;
        public static event Action<GameDataModule> ModuleLoaded;
        public static event Action<GameDataModule> ModuleSaved;
        public static event Action AllLoaded;

        internal static void RaiseModuleChanged(GameDataModule m) { ModuleChanged?.Invoke(m); }
        internal static void RaiseModuleLoaded(GameDataModule m) { ModuleLoaded?.Invoke(m); }
        internal static void RaiseModuleSaved(GameDataModule m) { ModuleSaved?.Invoke(m); }
        internal static void RaiseAllLoaded() { AllLoaded?.Invoke(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            ModuleChanged = null;
            ModuleLoaded = null;
            ModuleSaved = null;
            AllLoaded = null;
        }
    }
}

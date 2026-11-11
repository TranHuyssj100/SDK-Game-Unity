using UnityEngine;

namespace LSDK.GameData
{
    /// <summary>Chạy từng bước migrate: stored → stored+1 → ... → CurrentVersion.</summary>
    public static class DataMigration
    {
        /// <returns>true nếu có migrate (cần save lại).</returns>
        public static bool Run(GameDataModule module)
        {
            int stored = module.Version;
            int target = module.CurrentVersion;

            if (stored == target) return false;

            if (stored > target)
            {
                Debug.LogWarning($"[GameData] '{module.Key}' có version {stored} mới hơn code ({target}). Giữ nguyên data.");
                return false;
            }

            for (int v = stored; v < target; v++)
                module.MigrateStep(v);

            module.SetVersion(target);
            return true;
        }
    }
}

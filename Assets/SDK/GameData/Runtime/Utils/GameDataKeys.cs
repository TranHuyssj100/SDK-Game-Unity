namespace LSDK.GameData
{
    /// <summary>Tập trung toàn bộ key PlayerPrefs. Mỗi module 1 key.</summary>
    public static class GameDataKeys
    {
        public const string Prefix = "SDK_GameData_";
        public const string CorruptSuffix = "_corrupt";

        public const string Player = Prefix + "Player";
        public const string Currency = Prefix + "Currency";
        public const string Inventory = Prefix + "Inventory";
        public const string Settings = Prefix + "Settings";
        public const string Gameplay = Prefix + "Gameplay";
    }
}

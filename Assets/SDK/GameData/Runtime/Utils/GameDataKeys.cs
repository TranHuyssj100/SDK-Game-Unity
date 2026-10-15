namespace LSDK.GameData
{
    /// <summary>Key có sẵn cho module mẫu. Module mới trong Assets trả về chuỗi riêng, không cần sửa file này.</summary>
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

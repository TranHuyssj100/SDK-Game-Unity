// Assets/SDK/CurencyManager/Editor/CurrencyPaths.cs
using UnityEditor;

namespace LSDK.Currency.Editor
{
    /// <summary>
    /// Đường dẫn của module Currency, suy ra từ vị trí script thật.
    /// Package cài từ git nằm trong PackageCache, thư mục đó chỉ đọc.
    ///
    /// Hướng 2: CurrencyType, asmdef và extension method KHÔNG nằm trong package.
    /// Chúng được sinh vào <see cref="TypesRoot"/> phía project, hoạt động ở mọi project.
    /// </summary>
    public static class CurrencyPaths
    {
        const string AnchorSuffix = "/Runtime/CurrencyCatalog.cs";
        const string FallbackRoot = "Assets/SDK/CurencyManager";

        // ── Project-side generated folder ────────────────────────────────────────
        /// <summary>
        /// Thư mục project nơi lưu CurrencyType.cs, asmdef và extensions.
        /// Luôn ghi được (nằm trong Assets, không phải PackageCache).
        /// </summary>
        public const string TypesRoot = "Assets/LSDK/Currency";

        /// <summary>File enum CurrencyType (project-side, sinh tự động).</summary>
        public static string EnumPath => TypesRoot + "/CurrencyType.cs";

        /// <summary>File asmdef cho assembly Types (project-side, sinh tự động).</summary>
        public static string TypesAsmdefPath => TypesRoot + "/LSDK.Currency.Types.asmdef";

        /// <summary>File extension methods bridge CurrencyType → int (project-side, sinh tự động).</summary>
        public static string ExtensionsPath => TypesRoot + "/CurrencyManagerExtensions.cs";

        // ── Package-side paths ────────────────────────────────────────────────────
        /// <summary>Thư mục gốc module trong package, chứa Runtime và Editor.</summary>
        public static string Root
        {
            get
            {
                string[] guids = AssetDatabase.FindAssets("CurrencyCatalog t:MonoScript");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (path.EndsWith(AnchorSuffix, System.StringComparison.Ordinal))
                        return path.Substring(0, path.Length - AnchorSuffix.Length);
                }
                return FallbackRoot;
            }
        }

        /// <summary>true khi được ghi file cạnh script. PackageCache trả false.</summary>
        public static bool CanWriteInsidePackage
        {
            get { return AssetDatabase.IsOpenForEdit(Root + "/Runtime/CurrencyCatalog.cs"); }
        }

        /// <summary>Thư mục Generate bên trong package (prefab, v.v.).</summary>
        public static string PackageGenerateRoot => Root + "/Runtime/Generate";

        /// <summary>
        /// Nơi tạo prefab và asset khác. Package chỉ đọc thì dùng thư mục project.
        /// Chú ý: đây KHÔNG phải nơi enum sinh ra (xem <see cref="TypesRoot"/>).
        /// </summary>
        public static string GenerateRoot => CanWriteInsidePackage ? PackageGenerateRoot : TypesRoot + "/Prefabs";
    }
}

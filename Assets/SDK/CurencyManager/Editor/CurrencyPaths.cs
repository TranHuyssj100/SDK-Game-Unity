// Assets/SDK/CurencyManager/Editor/CurrencyPaths.cs
using UnityEditor;

namespace LSDK.Currency.Editor
{
    /// <summary>
    /// Đường dẫn của module Currency, suy ra từ vị trí script thật.
    /// Dời folder module đi đâu cũng không phải sửa hằng số.
    /// </summary>
    public static class CurrencyPaths
    {
        const string AnchorSuffix = "/Runtime/CurrencyCatalog.cs";
        const string FallbackRoot = "Assets/SDK/CurencyManager";

        /// <summary>Thư mục gốc module, chứa Runtime và Editor.</summary>
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

        /// <summary>Thư mục chứa code và prefab sinh ra.</summary>
        public static string GenerateRoot => Root + "/Runtime/Generate";

        /// <summary>File enum <c>CurrencyType</c>.</summary>
        public static string EnumPath => GenerateRoot + "/Code/CurrencyType.cs";
    }
}

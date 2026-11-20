// Assets/SDK/CurencyManager/Editor/CurrencyMenu.cs
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LSDK.Currency.Editor
{
    /// <summary>Menu tạo catalog và sinh enum.</summary>
    public static class CurrencyMenu
    {
        const string Root = "Tools/LSDK/Currency/";
        const string AssetPath = "Assets/Resources/LSDK/Currency/CurrencyCatalog.asset";

        [MenuItem(Root + "Create Catalog", false, 0)]
        static void CreateCatalog()
        {
            var existing = FindCatalog();
            if (existing != null)
            {
                CurrencyEnumGenerator.Generate(existing);
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                CurrencyCatalog.ClearCache();
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            var catalog = ScriptableObject.CreateInstance<CurrencyCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);

            var serialized = new SerializedObject(catalog);
            var list = serialized.FindProperty("_definitions");
            list.arraySize = 3;
            Write(list.GetArrayElementAtIndex(0), 0, "Coin", 0, false, 0, 1800, false);
            Write(list.GetArrayElementAtIndex(1), 1, "Heart", 5, true, 5, 1800, true);
            Write(list.GetArrayElementAtIndex(2), 2, "Diamond", 0, false, 0, 1800, false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

            catalog = AssetDatabase.LoadAssetAtPath<CurrencyCatalog>(AssetPath);
            CurrencyCatalog.ClearCache();
            CurrencyEnumGenerator.Generate(catalog);
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
            Debug.Log("[LSDK.Currency] Đã tạo " + AssetPath);
        }

        [MenuItem(Root + "Generate CurrencyType", false, 1)]
        static void GenerateEnum()
        {
            var catalog = FindCatalog();
            if (catalog == null)
            {
                Debug.LogError("[LSDK.Currency] Chưa có CurrencyCatalog. Chạy Create Catalog trước.");
                return;
            }

            CurrencyCatalog.ClearCache();
            CurrencyEnumGenerator.Generate(catalog);
        }

        static CurrencyCatalog FindCatalog()
        {
            var atPath = AssetDatabase.LoadAssetAtPath<CurrencyCatalog>(AssetPath);
            if (atPath != null) return atPath;

            string[] guids = AssetDatabase.FindAssets("t:CurrencyCatalog");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var found = AssetDatabase.LoadAssetAtPath<CurrencyCatalog>(path);
                if (found != null) return found;
            }
            return null;
        }

        static void Write(SerializedProperty element, int id, string name, long initial, bool regen, long max, int interval, bool unlimited)
        {
            element.FindPropertyRelative("_id").intValue = id;
            element.FindPropertyRelative("_name").stringValue = name;
            element.FindPropertyRelative("_initialAmount").longValue = initial;
            element.FindPropertyRelative("_useRegen").boolValue = regen;
            element.FindPropertyRelative("_maxAmount").longValue = max;
            element.FindPropertyRelative("_regenIntervalSeconds").intValue = interval;
            element.FindPropertyRelative("_allowUnlimited").boolValue = unlimited;
        }
    }
}

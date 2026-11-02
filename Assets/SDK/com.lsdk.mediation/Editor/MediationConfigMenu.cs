using System.IO;
using UnityEditor;
using UnityEngine;

namespace LSDK.Mediation.EditorTools
{
    public static class MediationConfigMenu
    {
        private const string Dir = "Assets/Resources/LSDK";
        private const string AssetPath = Dir + "/MediationConfig.asset";

        [MenuItem("LSDK/Mediation/Create Config")]
        private static void CreateConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MediationConfig>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            Directory.CreateDirectory(Dir);
            var config = ScriptableObject.CreateInstance<MediationConfig>();
            config.ResetToDefaults();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}

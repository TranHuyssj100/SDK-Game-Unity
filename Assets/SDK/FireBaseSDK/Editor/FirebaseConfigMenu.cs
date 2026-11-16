// Assets/SDK/FireBaseSDK/Editor/FirebaseConfigMenu.cs
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LSDK.Firebase.Editor
{
    public static class FirebaseConfigMenu
    {
        private const string Dir       = "Assets/Resources/LSDK/Firebase/SO";
        private const string AssetPath = Dir + "/FirebaseConfig.asset";

        [MenuItem("Tools/LSDK/Firebase/Create Config")]
        private static void CreateConfig()
        {
            FirebaseConfig existing = AssetDatabase.LoadAssetAtPath<FirebaseConfig>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            Directory.CreateDirectory(Dir);
            FirebaseConfig config = ScriptableObject.CreateInstance<FirebaseConfig>();
            config.ResetToDefaults();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}

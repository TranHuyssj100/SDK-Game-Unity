// Assets/SDK/SDKCore/GameManager/Editor/GameManagerMenu.cs
using UnityEditor;
using UnityEngine;
using LSDK.GameManager;

namespace LSDK.GameManager.Editor
{
    /// <summary>
    /// Menu Editor tiện ích cho LSDK GameManager.
    /// Truy cập qua <c>Tools → LSDK → GameManager</c>.
    /// </summary>
    public static class GameManagerMenu
    {
        private const string MenuRoot       = "Tools/LSDK/GameManager/";
        private const string ConfigPath     = "Assets/Resources/LSDK/GameManager/SO/GameManagerConfig.asset";
        private const string PrefabPath     = "Assets/Settings/GameManager.prefab";

        // ── Create Config ────────────────────────────────────────

        [MenuItem(MenuRoot + "Create Config", priority = 1)]
        public static void CreateConfig()
        {
            // Đảm bảo thư mục tồn tại
            System.IO.Directory.CreateDirectory("Assets/Resources/LSDK/GameManager/SO");
            AssetDatabase.Refresh();

            // Nếu đã tồn tại thì chọn luôn
            var existing = AssetDatabase.LoadAssetAtPath<GameManagerConfig>(ConfigPath);
            if (existing != null)
            {
                Debug.Log($"[LSDK.GameManager] Config đã tồn tại tại: {ConfigPath}");
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var config = ScriptableObject.CreateInstance<GameManagerConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
            Debug.Log($"[LSDK.GameManager] Đã tạo GameManagerConfig tại: {ConfigPath}");
        }

        // ── Create GameObject ────────────────────────────────────

        [MenuItem(MenuRoot + "Add GameManager to Scene", priority = 2)]
        public static void AddToScene()
        {
            // Kiểm tra đã có trong scene chưa
            var existing = Object.FindObjectOfType<GameManager>();
            if (existing != null)
            {
                Debug.Log("[LSDK.GameManager] GameManager đã có trong scene.");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();

            // Gán config nếu có
            var config = AssetDatabase.LoadAssetAtPath<GameManagerConfig>(ConfigPath);
            if (config != null)
            {
                var serialized = new SerializedObject(gm);
                serialized.FindProperty("_config").objectReferenceValue = config;
                serialized.ApplyModifiedProperties();
                Debug.Log("[LSDK.GameManager] Đã gán GameManagerConfig vào GameManager.");
            }
            else
            {
                Debug.LogWarning($"[LSDK.GameManager] Chưa tìm thấy config tại '{ConfigPath}'. Chạy 'Create Config' trước, sau đó gán thủ công vào GameManager._config.");
            }

            Undo.RegisterCreatedObjectUndo(go, "Add GameManager");
            Selection.activeGameObject = go;
            Debug.Log("[LSDK.GameManager] Đã thêm GameManager vào scene.");
        }

        // ── Validate menus ───────────────────────────────────────

        [MenuItem(MenuRoot + "Add GameManager to Scene", true)]
        private static bool ValidateAddToScene()
            => !Application.isPlaying;
    }
}

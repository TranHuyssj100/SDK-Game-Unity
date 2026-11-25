// Assets/SDK/CurencyManager/Editor/CurrencyReceiveMenu.cs
using System.IO;
using LSDK.Core.ObjectPools;
using LSDK.Core.ObjectPools.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LSDK.Currency.Editor
{
    /// <summary>
    /// Tạo config nhận tiền, prefab icon bay, và gắn prefab vào PoolDatabase.
    /// </summary>
    public static class CurrencyReceiveMenu
    {
        const string Root = "Tools/LSDK/Currency/";
        const string ConfigPath = "Assets/Resources/LSDK/Currency/CurrencyReceiveConfig.asset";

        static string FlyPath => CurrencyPaths.GenerateRoot + "/Prefabs/CurrencyFlyIcon.prefab";

        [MenuItem(Root + "Create Receive Setup", false, 30)]
        static void Create()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[LSDK.Currency] Tắt Play mode rồi chạy Create Receive Setup.");
                return;
            }

            var config = Setup();
            if (config != null)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
        }

        /// <summary>
        /// Tạo prefab CurrencyFlyIcon nếu chưa có, gắn vào mọi PoolDatabase, và ghi pool id vào config.
        /// </summary>
        public static CurrencyReceiveConfig Setup()
        {
            EnsureFolder(Path.GetDirectoryName(FlyPath).Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(ConfigPath).Replace('\\', '/'));

            var item = AssetDatabase.LoadAssetAtPath<CurrencyItem>(CurrencyPaths.GenerateRoot + "/Prefabs/CurrencyItem.prefab");
            CurrencyUiMenu.EnsureItemIcon(item);

            var prefab = LoadOrCreateFly();
            int poolId = Register(prefab);
            var config = LoadOrCreateConfig(poolId);

            CurrencyReceiveConfig.ClearCache();
            Debug.Log("[LSDK.Currency] Receive setup: config " + ConfigPath + ", prefab " + FlyPath + ", pool #" + poolId + ".");
            return config;
        }

        static GameObject LoadOrCreateFly()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(FlyPath);
            if (existing != null) return existing;

            var instance = BuildFly();
            try
            {
                return PrefabUtility.SaveAsPrefabAsset(instance, FlyPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static GameObject BuildFly()
        {
            int ui = LayerMask.NameToLayer("UI");
            var root = new GameObject("CurrencyFlyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (ui >= 0) root.layer = ui;

            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(72f, 72f);

            var image = root.GetComponent<Image>();
            image.sprite = FlySprite();
            image.preserveAspect = true;
            image.raycastTarget = false;

            var icon = root.AddComponent<CurrencyFlyIcon>();
            var so = new SerializedObject(icon);
            so.FindProperty("_image").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static CurrencyReceiveConfig LoadOrCreateConfig(int poolId)
        {
            var config = AssetDatabase.LoadAssetAtPath<CurrencyReceiveConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CurrencyReceiveConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            if (poolId > 0)
            {
                var so = new SerializedObject(config);
                var id = so.FindProperty("_poolId");
                if (id != null && id.intValue <= 0)
                {
                    id.intValue = poolId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(config);
            return config;
        }

        static int Register(GameObject prefab)
        {
            if (prefab == null) return 0;

            string[] guids = AssetDatabase.FindAssets("t:PoolDatabaseSO");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[LSDK.Currency] Chưa có PoolDatabase. Tạo asset LSDK/Object Pools/Pool Database, gán vào PoolServer, rồi chạy lại Create Receive Setup.");
                return 0;
            }

            int first = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var db = AssetDatabase.LoadAssetAtPath<PoolDatabaseSO>(path);
                if (db == null) continue;

                int id = EnsureEntry(db, prefab);
                if (first <= 0) first = id;
                if (db.autoGenerate)
                    PoolEnumGenerator.Generate(db);
            }
            return first;
        }

        static int EnsureEntry(PoolDatabaseSO db, GameObject prefab)
        {
            var so = new SerializedObject(db);
            var entries = so.FindProperty("entries");
            var nextId = so.FindProperty("nextId");

            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("prefab").objectReferenceValue == prefab)
                    return entry.FindPropertyRelative("id").intValue;
            }

            int id = Mathf.Max(1, nextId.intValue);
            nextId.intValue = id + 1;
            int index = entries.arraySize;
            entries.arraySize++;
            var created = entries.GetArrayElementAtIndex(index);
            created.FindPropertyRelative("id").intValue = id;
            created.FindPropertyRelative("name").stringValue = "CurrencyFlyIcon";
            created.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            created.FindPropertyRelative("initialSize").intValue = 16;
            created.FindPropertyRelative("maxSize").intValue = 0;
            created.FindPropertyRelative("expandable").boolValue = true;
            created.FindPropertyRelative("collectionCheck").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(db);
            return id;
        }

        static Sprite FlySprite()
        {
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            return knob != null ? knob : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}

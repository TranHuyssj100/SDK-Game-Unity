// Assets/SDK/SDKCore/ToastSystem/Editor/ToastMenu.cs
using System.IO;
using LSDK.ToastSystem;
using LSDK.UISystem;
using LSDK.UISystem.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LSDK.ToastSystem.Editor
{
    public static class ToastMenu
    {
        const string Folder = "Assets/SDK/SDKCore/ToastSystem/Prefabs";
        const string ItemPath = Folder + "/ToastItem.prefab";
        const string PanelPath = Folder + "/ToastPanel.prefab";
        const string LogPrefix = "[LSDK.ToastSystem]";

        [MenuItem("Tools/LSDK/Toast/Create Toast Sample")]
        static void CreateSample()
        {
            EnsureFolder(Folder);

            var item = LoadOrCreateItem();
            var panel = LoadOrCreatePanel(item);
            Wire(panel, item);
            Register(panel);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var panelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (panelAsset != null)
            {
                Selection.activeObject = panelAsset;
                EditorGUIUtility.PingObject(panelAsset);
            }

            Debug.Log(LogPrefix + " Đã tạo sample tại " + Folder + ". ToastPanel được đăng ký nếu scene có UIRegistry.");
        }

        static ToastItem LoadOrCreateItem()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ToastItem>(ItemPath);
            if (existing != null)
                return existing;

            var instance = BuildItem();
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, ItemPath);
                return prefab != null ? prefab.GetComponent<ToastItem>() : null;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static ToastPanel LoadOrCreatePanel(ToastItem item)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ToastPanel>(PanelPath);
            if (existing != null)
                return existing;

            var instance = BuildPanel(item);
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PanelPath);
                return prefab != null ? prefab.GetComponent<ToastPanel>() : null;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static GameObject BuildItem()
        {
            int ui = UiLayer();
            var root = new GameObject("ToastItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            SetUiLayer(root, ui);

            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 110f);
            rect.anchoredPosition = Vector2.zero;

            var image = root.GetComponent<Image>();
            var sprite = UiSprite();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(0.1f, 0.11f, 0.13f, 0.94f);
            image.raycastTarget = false;

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var label = CreateLabel(root.transform, ui);
            var item = root.AddComponent<ToastItem>();
            var so = new SerializedObject(item);
            so.FindProperty("_label").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static TextMeshProUGUI CreateLabel(Transform parent, int ui)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            SetUiLayer(go, ui);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(28f, 12f);
            rect.offsetMax = new Vector2(-28f, -12f);

            var text = go.AddComponent<TextMeshProUGUI>();
            var font = FindFont();
            if (font != null)
                text.font = font;
            else
                Debug.LogWarning(LogPrefix + " Chưa có TMP Font Asset. Gán font cho Label trên ToastItem.");

            text.text = "Toast";
            text.fontSize = 36f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        static GameObject BuildPanel(ToastItem itemPrefab)
        {
            int ui = UiLayer();
            var root = new GameObject("ToastPanel", typeof(RectTransform), typeof(CanvasGroup));
            SetUiLayer(root, ui);
            Stretch((RectTransform)root.transform);

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var panel = root.AddComponent<ToastPanel>();
            panel.Layer = UILayer.Overlay;
            panel.CloseOnBack = false;
            panel.OpaqueBackground = false;

            var animator = root.GetComponent<UIAnimator>();
            if (animator != null)
            {
                var animatorSo = new SerializedObject(animator);
                animatorSo.FindProperty("motion").intValue = (int)UIMotion.None;
                animatorSo.FindProperty("target").objectReferenceValue = root.GetComponent<RectTransform>();
                animatorSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var container = new GameObject("Container", typeof(RectTransform));
            SetUiLayer(container, ui);
            var containerRect = (RectTransform)container.transform;
            containerRect.SetParent(root.transform, false);
            Stretch(containerRect);

            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("_toastPrefab").objectReferenceValue = itemPrefab;
            panelSo.FindProperty("_toastContainer").objectReferenceValue = containerRect;
            panelSo.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void Wire(ToastPanel panel, ToastItem item)
        {
            if (panel == null || item == null)
                return;

            var so = new SerializedObject(panel);
            var prefab = so.FindProperty("_toastPrefab");
            if (prefab.objectReferenceValue == null)
                prefab.objectReferenceValue = item;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        static void Register(ToastPanel prefab)
        {
            if (prefab == null)
                return;

            var guids = AssetDatabase.FindAssets("t:UIRegistry");
            if (guids.Length == 0)
            {
                Debug.LogWarning(LogPrefix + " Chưa có UIRegistry. Tạo registry rồi kéo ToastPanel vào.");
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var registry = AssetDatabase.LoadAssetAtPath<UIRegistry>(path);
                if (registry == null || Contains(registry, prefab))
                    continue;

                var so = new SerializedObject(registry);
                var entries = so.FindProperty("entries");
                var nextId = so.FindProperty("nextId");
                int index = entries.arraySize;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").intValue = nextId.intValue;
                entry.FindPropertyRelative("name").stringValue = "Toast";
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                nextId.intValue++;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(registry);

                if (registry.autoGenerate)
                    UIEnumGenerator.Generate(registry);
            }
        }

        static bool Contains(UIRegistry registry, ToastPanel prefab)
        {
            var entries = registry.entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;
                if (entry.prefab == prefab)
                    return true;
                if (entry.name == "Toast")
                    return true;
            }
            return false;
        }

        static TMP_FontAsset FindFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            string fallback = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.IndexOf("LiberationSans", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fallback == null)
                    fallback = path;
            }

            return fallback != null ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallback) : null;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static int UiLayer()
        {
            return LayerMask.NameToLayer("UI");
        }

        static void SetUiLayer(GameObject go, int ui)
        {
            if (ui >= 0)
                go.layer = ui;
        }

        static Sprite UiSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}

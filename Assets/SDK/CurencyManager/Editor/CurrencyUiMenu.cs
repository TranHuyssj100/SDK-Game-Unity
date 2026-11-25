// Assets/SDK/CurencyManager/Editor/CurrencyUiMenu.cs
using System.Collections.Generic;
using System.IO;
using LSDK.UISystem;
using LSDK.UISystem.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LSDK.Currency.Editor
{
    /// <summary>
    /// Tạo prefab trong Generate và đăng ký CurrencyPanel vào mọi UIRegistry.
    /// </summary>
    public static class CurrencyUiMenu
    {
        const string Root = "Tools/LSDK/Currency/";
        const string RegistryName = "Currency";

        // Suy ra từ vị trí module, không hard-code đường dẫn.
        static string GenerateRoot => CurrencyPaths.GenerateRoot;
        static string ItemFolder => GenerateRoot + "/Prefabs";
        static string PanelFolder => GenerateRoot + "/Panel";
        static string ItemPath => ItemFolder + "/CurrencyItem.prefab";
        static string PanelPath => PanelFolder + "/CurrencyPanel.prefab";

        [MenuItem(Root + "Create Currency UI", false, 20)]
        static void Create()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[LSDK.Currency] Tắt Play mode rồi chạy Create Currency UI.");
                return;
            }

            EnsureFolder(ItemFolder);
            EnsureFolder(PanelFolder);

            var catalog = FindCatalog();
            if (catalog != null)
            {
                CurrencyCatalog.ClearCache();
                CurrencyEnumGenerator.Generate(catalog);
            }
            else
            {
                Debug.LogWarning("[LSDK.Currency] Chưa có CurrencyCatalog. Thanh tiền chưa có ô mới.");
            }

            var item = LoadOrCreateItem();
            var panel = LoadOrCreatePanel(item, catalog);
            if (panel != null)
                panel = Sync(item, catalog);

            Register(panel);
            CurrencyReceiveMenu.Setup();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            Debug.Log("[LSDK.Currency] Đã tạo UI tại " + GenerateRoot + ", đăng ký CurrencyPanel, tạo CurrencyFlyIcon và gắn vào PoolDatabase.");
        }

        static CurrencyCatalog FindCatalog()
        {
            const string assetPath = "Assets/Resources/LSDK/Currency/CurrencyCatalog.asset";
            var atPath = AssetDatabase.LoadAssetAtPath<CurrencyCatalog>(assetPath);
            if (atPath != null) return atPath;

            string[] guids = AssetDatabase.FindAssets("t:CurrencyCatalog");
            for (int i = 0; i < guids.Length; i++)
            {
                var found = AssetDatabase.LoadAssetAtPath<CurrencyCatalog>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (found != null) return found;
            }
            return null;
        }

        static CurrencyItem LoadOrCreateItem()
        {
            var existing = AssetDatabase.LoadAssetAtPath<CurrencyItem>(ItemPath);
            if (existing != null) return existing;

            var instance = BuildItem();
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, ItemPath);
                return prefab != null ? prefab.GetComponent<CurrencyItem>() : null;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static CurrencyPanel LoadOrCreatePanel(CurrencyItem item, CurrencyCatalog catalog)
        {
            var existing = AssetDatabase.LoadAssetAtPath<CurrencyPanel>(PanelPath);
            if (existing != null) return existing;

            var instance = BuildPanel(item, catalog);
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, PanelPath);
                return prefab != null ? prefab.GetComponent<CurrencyPanel>() : null;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        static CurrencyPanel Sync(CurrencyItem itemPrefab, CurrencyCatalog catalog)
        {
            var root = PrefabUtility.LoadPrefabContents(PanelPath);
            try
            {
                var loaded = root.GetComponent<CurrencyPanel>();
                if (loaded != null)
                {
                    loaded.Layer = UILayer.Overlay;
                    loaded.CloseOnBack = false;
                    loaded.OpaqueBackground = false;
                }

                var animator = root.GetComponent<UIAnimator>();
                if (animator != null)
                {
                    var animatorSo = new SerializedObject(animator);
                    animatorSo.FindProperty("motion").intValue = (int)UIMotion.None;
                    animatorSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var content = root.transform.Find("Content");
                WireLabels(root);
                if (content != null)
                    AddMissing(content, itemPrefab, catalog);

                PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<CurrencyPanel>(PanelPath);
        }

        static void WireLabels(GameObject root)
        {
            var items = root.GetComponentsInChildren<CurrencyItem>(true);
            for (int i = 0; i < items.Length; i++)
            {
                var so = new SerializedObject(items[i]);
                var label = so.FindProperty("_amountLabel");
                if (label != null && label.objectReferenceValue == null)
                    label.objectReferenceValue = items[i].GetComponentInChildren<TMP_Text>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void AddMissing(Transform content, CurrencyItem itemPrefab, CurrencyCatalog catalog)
        {
            if (itemPrefab == null || catalog == null || catalog.Definitions == null) return;

            var have = new HashSet<int>();
            var items = content.GetComponentsInChildren<CurrencyItem>(true);
            for (int i = 0; i < items.Length; i++)
                have.Add(ReadType(items[i]));

            var definitions = catalog.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null || !have.Add(def.Id)) continue;

                var instance = (CurrencyItem)PrefabUtility.InstantiatePrefab(itemPrefab, content);
                if (instance == null) continue;
                instance.gameObject.name = def.Name + "Item";
                var so = new SerializedObject(instance);
                var type = so.FindProperty("_currencyTypeId");
                if (type != null) type.intValue = def.Id;
                var label = so.FindProperty("_amountLabel");
                if (label != null && label.objectReferenceValue == null)
                    label.objectReferenceValue = instance.GetComponentInChildren<TMP_Text>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Gắn Image con tên Icon vào ô tiền nếu prefab chưa gán.</summary>
        public static void EnsureItemIcon(CurrencyItem item)
        {
            if (item == null) return;
            var so = new SerializedObject(item);
            var icon = so.FindProperty("_icon");
            if (icon == null || icon.objectReferenceValue != null) return;

            var images = item.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || image.gameObject == item.gameObject) continue;
                if (image.gameObject.name != "Icon") continue;
                icon.objectReferenceValue = image;
                so.ApplyModifiedPropertiesWithoutUndo();
                return;
            }
        }

        static int ReadType(CurrencyItem item)
        {
            var so = new SerializedObject(item);
            var type = so.FindProperty("_currencyTypeId");
            return type != null ? type.intValue : -1;
        }

        static GameObject BuildItem()
        {
            int ui = UiLayer();
            var root = new GameObject("CurrencyItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            SetUiLayer(root, ui);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(260f, 100f);

            var image = root.GetComponent<Image>();
            var sprite = UiSprite();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = new Color(1f, 1f, 1f, 0.92f);
            image.raycastTarget = false;

            var icon = CreateImage(root.transform, "Icon", ui, new Vector2(80f, 80f), new Vector2(-80f, 0f));
            icon.raycastTarget = false;

            var label = CreateLabel(root.transform, ui);
            var item = root.AddComponent<CurrencyItem>();
            var so = new SerializedObject(item);
            so.FindProperty("_amountLabel").objectReferenceValue = label;
            var iconProp = so.FindProperty("_icon");
            if (iconProp != null) iconProp.objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static GameObject BuildPanel(CurrencyItem itemPrefab, CurrencyCatalog catalog)
        {
            int ui = UiLayer();
            var root = new GameObject("CurrencyPanel", typeof(RectTransform), typeof(CanvasGroup));
            SetUiLayer(root, ui);

            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -50f);
            rect.sizeDelta = new Vector2(0f, 100f);

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var panel = root.AddComponent<CurrencyPanel>();
            panel.Layer = UILayer.Overlay;
            panel.CloseOnBack = false;
            panel.OpaqueBackground = false;

            var animator = root.GetComponent<UIAnimator>();
            if (animator != null)
            {
                var animatorSo = new SerializedObject(animator);
                animatorSo.FindProperty("motion").intValue = (int)UIMotion.None;
                animatorSo.FindProperty("target").objectReferenceValue = rect;
                animatorSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            SetUiLayer(contentGo, ui);
            var content = (RectTransform)contentGo.transform;
            content.SetParent(root.transform, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(24f, 0f);
            content.offsetMax = new Vector2(-24f, 0f);

            var layout = contentGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (itemPrefab != null && catalog != null && catalog.Definitions != null)
            {
                var definitions = catalog.Definitions;
                for (int i = 0; i < definitions.Count; i++)
                {
                    var def = definitions[i];
                    if (def == null) continue;
                    var instance = (CurrencyItem)PrefabUtility.InstantiatePrefab(itemPrefab, content);
                    if (instance == null) continue;
                    instance.gameObject.name = def.Name + "Item";
                    var so = new SerializedObject(instance);
                    var type = so.FindProperty("_currencyTypeId");
                    if (type != null) type.intValue = def.Id;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return root;
        }

        static Image CreateImage(Transform parent, string name, int ui, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            SetUiLayer(go, ui);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text CreateLabel(Transform parent, int ui)
        {
            var go = new GameObject("Text (TMP)", typeof(RectTransform));
            SetUiLayer(go, ui);
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(150f, 50f);
            rect.anchoredPosition = new Vector2(40f, 0f);

            var text = go.AddComponent<TextMeshProUGUI>();
            var font = FindFont();
            if (font != null) text.font = font;
            text.text = "0";
            text.fontSize = 36f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.color = Color.black;
            text.raycastTarget = false;
            return text;
        }

        static void Register(CurrencyPanel prefab)
        {
            if (prefab == null) return;

            string[] guids = AssetDatabase.FindAssets("t:UIRegistry");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[LSDK.Currency] Chưa có UIRegistry. Tạo registry rồi chạy lại Create Currency UI.");
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var registry = AssetDatabase.LoadAssetAtPath<UIRegistry>(path);
                if (registry == null || BindExisting(registry, prefab)) continue;

                var so = new SerializedObject(registry);
                var entries = so.FindProperty("entries");
                var nextId = so.FindProperty("nextId");
                int index = entries.arraySize;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").intValue = nextId.intValue;
                entry.FindPropertyRelative("name").stringValue = RegistryName;
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                nextId.intValue++;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(registry);

                if (registry.autoGenerate)
                    UIEnumGenerator.Generate(registry);
            }
        }

        static bool BindExisting(UIRegistry registry, CurrencyPanel prefab)
        {
            var entries = registry.entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;
                if (entry.prefab == prefab) return true;
                if (entry.name != RegistryName) continue;

                var so = new SerializedObject(registry);
                so.FindProperty("entries").GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = prefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(registry);
                if (registry.autoGenerate)
                    UIEnumGenerator.Generate(registry);
                return true;
            }
            return false;
        }

        static TMP_FontAsset FindFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            string fallback = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.IndexOf("LiberationSans", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fallback == null) fallback = path;
            }
            return fallback != null ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallback) : null;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
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
            if (ui >= 0) go.layer = ui;
        }

        static Sprite UiSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}

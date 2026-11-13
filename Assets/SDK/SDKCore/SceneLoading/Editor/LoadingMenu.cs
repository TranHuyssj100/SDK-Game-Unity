using System.IO;
using LSDK.Core;
using LSDK.Loading;
using LSDK.UISystem;
using LSDK.UISystem.EditorTools;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace LSDK.Loading.EditorTools
{
    public static class LoadingMenu
    {
        const string ConfigPath = "Assets/Resources/LSDK/Loading/SO/LoadingConfig.asset";
        const string PresetPath = "Assets/Resources/LSDK/Loading/SO/LoadingFade.asset";
        const string PrefabPath = "Assets/SDK/SDKCore/Loading/Runtime/UI/Prefabs/LoadingPanel.prefab";

        [MenuItem("Tools/LSDK/Loading/Setup Loading")]
        static void Setup()
        {
            EnsureFolder("Assets/Resources/LSDK/Loading/SO");
            EnsureFolder("Assets/SDK/SDKCore/Loading/Runtime/UI/Prefabs");

            var config = EnsureConfig();
            var preset = EnsurePreset();
            var prefab = EnsurePrefab(preset);
            Register(prefab);
            EnsureManager(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Loading] Setup xong. LoadingPanel nằm trên layer Blocker và đã được đăng ký nếu có UIRegistry.");
        }

        static LoadingConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LoadingConfig>(ConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<LoadingConfig>();
            config.minimumCoveredTime = 0.6f;
            config.progressSmoothSpeed = 1.5f;
            config.randomTip = true;
            config.tips = new[] { "Ready..." };
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        static UIAnimationPreset EnsurePreset()
        {
            var preset = AssetDatabase.LoadAssetAtPath<UIAnimationPreset>(PresetPath);
            if (preset != null) return preset;

            preset = ScriptableObject.CreateInstance<UIAnimationPreset>();
            AssetDatabase.CreateAsset(preset, PresetPath);
            preset.UpdateMotions();
            var so = new SerializedObject(preset);
            var motions = so.FindProperty("motions");
            for (int i = 0; i < motions.arraySize; i++)
            {
                var element = motions.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("motion").intValue != (int)UIMotion.None) continue;
                WriteFade(element.FindPropertyRelative("show"));
                WriteFade(element.FindPropertyRelative("hide"));
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(preset);
            return preset;
        }

        static void WriteFade(SerializedProperty config)
        {
            config.FindPropertyRelative("fade").boolValue = true;
            config.FindPropertyRelative("ease").intValue = 1; // Ease.Linear
            config.FindPropertyRelative("duration").floatValue = 0.35f;
            config.FindPropertyRelative("delay").floatValue = 0f;
            config.FindPropertyRelative("scale").floatValue = 1f;
        }

        static LoadingPanel EnsurePrefab(UIAnimationPreset preset)
        {
            var existingGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existingGo != null && existingGo.TryGetComponent(out LoadingPanel existing))
                return existing;

            var root = BuildPanel(preset);
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return prefab != null ? prefab.GetComponent<LoadingPanel>() : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static GameObject BuildPanel(UIAnimationPreset preset)
        {
            var root = new GameObject("LoadingPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.layer = LayerMask.NameToLayer("UI");
            Stretch((RectTransform)root.transform);

            var image = root.GetComponent<Image>();
            image.sprite = UiSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.07f, 0.08f, 0.1f, 1f);
            image.raycastTarget = true;

            var panel = root.AddComponent<LoadingPanel>();
            panel.Layer = UILayer.Blocker;
            panel.OpaqueBackground = true;
            panel.CloseOnBack = false;

            var animator = root.GetComponent<UIAnimator>();
            var animatorSo = new SerializedObject(animator);
            animatorSo.FindProperty("preset").objectReferenceValue = preset;
            animatorSo.FindProperty("motion").intValue = (int)UIMotion.None;
            animatorSo.FindProperty("target").objectReferenceValue = root.GetComponent<RectTransform>();
            animatorSo.ApplyModifiedPropertiesWithoutUndo();

            var status = CreateText("Status", root.transform, 36, TextAnchor.MiddleCenter, Color.white);
            Place(status.rectTransform, 0.12f, 0.56f, 0.88f, 0.7f);

            var slider = CreateSlider(root.transform);
            var percent = CreateText("Percent", root.transform, 28, TextAnchor.MiddleCenter, Color.white);
            Place(percent.rectTransform, 0.12f, 0.38f, 0.88f, 0.46f);
            percent.text = "0%";

            var tip = CreateText("Tip", root.transform, 22, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));
            Place(tip.rectTransform, 0.14f, 0.18f, 0.86f, 0.32f);

            var dismiss = CreateDismissButton(root.transform, panel);

            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("progressBar").objectReferenceValue = slider;
            panelSo.FindProperty("progressLabel").objectReferenceValue = percent;
            panelSo.FindProperty("statusLabel").objectReferenceValue = status;
            panelSo.FindProperty("tipLabel").objectReferenceValue = tip;
            panelSo.FindProperty("dismissButton").objectReferenceValue = dismiss;
            panelSo.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void Register(LoadingPanel prefab)
        {
            if (prefab == null) return;
            var guids = AssetDatabase.FindAssets("t:UIRegistry");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[Loading] Chưa có UIRegistry. Tạo registry rồi kéo LoadingPanel vào.");
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var registry = AssetDatabase.LoadAssetAtPath<UIRegistry>(path);
                if (registry == null || Contains(registry, prefab)) continue;

                var so = new SerializedObject(registry);
                var entries = so.FindProperty("entries");
                var nextId = so.FindProperty("nextId");
                int index = entries.arraySize;
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").intValue = nextId.intValue;
                entry.FindPropertyRelative("name").stringValue = "Loading";
                entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                nextId.intValue++;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(registry);

                if (registry.autoGenerate) UIEnumGenerator.Generate(registry);
            }
        }

        static bool Contains(UIRegistry registry, LoadingPanel prefab)
        {
            var entries = registry.entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null) continue;
                if (entry.prefab == prefab) return true;
                if (entry.name == "Loading") return true;
            }
            return false;
        }

        static void EnsureManager(LoadingConfig config)
        {
#if UNITY_2023_1_OR_NEWER
            var existing = Object.FindAnyObjectByType<LoadingManager>(FindObjectsInactive.Include);
            var root = Object.FindAnyObjectByType<SDKCoreRoot>();
#else
            var existing = Object.FindObjectOfType<LoadingManager>();
            var root = Object.FindObjectOfType<SDKCoreRoot>();
#endif
            if (existing != null)
            {
                var so = new SerializedObject(existing);
                var configProp = so.FindProperty("config");
                if (configProp.objectReferenceValue == null)
                {
                    configProp.objectReferenceValue = config;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(existing);
                }
                return;
            }

            if (root == null)
            {
                Debug.Log("[Loading] Scene chưa có SDKCoreRoot. Gắn LoadingManager làm con của root khi đã có.");
                return;
            }

            var go = new GameObject("LoadingManager");
            Undo.RegisterCreatedObjectUndo(go, "Create Loading Manager");
            Undo.SetTransformParent(go.transform, root.transform, "Create Loading Manager");
            var manager = Undo.AddComponent<LoadingManager>(go);
            var managerSo = new SerializedObject(manager);
            managerSo.FindProperty("config").objectReferenceValue = config;
            managerSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static Button CreateDismissButton(Transform parent, LoadingPanel panel)
        {
            var go = new GameObject("Dismiss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.32f, 0.08f);
            rt.anchorMax = new Vector2(0.68f, 0.15f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = UiSprite();
            image.color = new Color(0.95f, 0.78f, 0.28f, 1f);

            var label = CreateText("Label", rt, 26, TextAnchor.MiddleCenter, new Color(0.1f, 0.1f, 0.12f, 1f));
            Stretch(label.rectTransform);
            label.text = "Đóng";

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            UnityEventTools.AddPersistentListener(button.onClick, panel.Dismiss);
            go.SetActive(false);
            return button;
        }

        static Slider CreateSlider(Transform parent)
        {
            var go = new GameObject("Progress", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.12f, 0.48f);
            rt.anchorMax = new Vector2(0.88f, 0.48f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 28f);
            rt.anchoredPosition = Vector2.zero;

            var background = CreateImage("Background", rt, new Color(1f, 1f, 1f, 0.16f));
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.gameObject.layer = LayerMask.NameToLayer("UI");
            fillArea.SetParent(rt, false);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(4f, 4f);
            fillArea.offsetMax = new Vector2(-4f, -4f);

            var fill = CreateImage("Fill", fillArea, new Color(0.95f, 0.78f, 0.28f, 1f));
            fill.raycastTarget = false;
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.pivot = new Vector2(0f, 0.5f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.targetGraphic = background;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
            var navigation = slider.navigation;
            navigation.mode = Navigation.Mode.None;
            slider.navigation = navigation;
            return slider;
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = UiSprite();
            image.color = color;
            return image;
        }

        static Text CreateText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var text = go.AddComponent<Text>();
            text.font = BuiltinFont();
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.rectTransform.SetParent(parent, false);
            return text;
        }

        static void Place(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static Font BuiltinFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        static Sprite UiSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }
    }
}

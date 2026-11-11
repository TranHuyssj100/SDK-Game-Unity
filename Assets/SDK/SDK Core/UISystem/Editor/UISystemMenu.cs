using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LSDK.UISystem.EditorTools
{
    public static class UISystemMenu
    {
        // Sorting order của mỗi layer canvas (giá trị tăng dần = vẽ trên).
        static readonly int[] LayerSortOrder = { 0, 10, 20, 30 };

        [MenuItem("Tools/LSDK/UI System/Create UI Root")]
        static void CreateUIRootMenu()
        {
            Selection.activeGameObject = CreateUIRoot(null, true);
        }

        /// <summary>
        /// Tạo UIRoot đủ Canvas, 4 layer và UIManager.
        /// parent != null thì gắn làm con. createEventSystem đưa EventSystem vào cùng parent (hoặc scene root).
        /// </summary>
        public static GameObject CreateUIRoot(Transform parent, bool createEventSystem)
        {
            // UIRoot: Canvas gốc + CanvasScaler. Không có GraphicRaycaster ở đây,
            // mỗi layer canvas có raycaster riêng.
            var root = new GameObject("UIRoot", typeof(Canvas), typeof(CanvasScaler), typeof(UIManager));
            Undo.RegisterCreatedObjectUndo(root, "Create UI Root");
            if (parent != null)
                Undo.SetTransformParent(root.transform, parent, "Create UI Root");
            root.layer = LayerMask.NameToLayer("UI");

            var rootCanvas = root.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // Tạo Canvas con cho mỗi UILayer.
            // Mỗi Canvas con: overrideSorting để đảm bảo thứ tự; có raycaster riêng.
            var layers = new Transform[4];
            var layerCanvases = new Canvas[4];
            foreach (UILayer l in System.Enum.GetValues(typeof(UILayer)))
            {
                int idx = (int)l;
                var rt = StretchedCanvas(l.ToString(), root.transform, LayerSortOrder[idx]);
                layers[idx] = rt;
                layerCanvases[idx] = rt.GetComponent<Canvas>();
            }

            // InputBlocker: con của Blocker layer, fullscreen, chặn input khi loading.
            var blocker = Stretched("InputBlocker", layers[(int)UILayer.Blocker]);
            var img = blocker.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            img.raycastTarget = true;
            blocker.gameObject.SetActive(false);

            var mgr = root.GetComponent<UIManager>();
            var so = new SerializedObject(mgr);

            var arr = so.FindProperty("layerRoots");
            arr.arraySize = layers.Length;
            for (int i = 0; i < layers.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];

            var canvasArr = so.FindProperty("layerCanvases");
            canvasArr.arraySize = layerCanvases.Length;
            for (int i = 0; i < layerCanvases.Length; i++)
                canvasArr.GetArrayElementAtIndex(i).objectReferenceValue = layerCanvases[i];

            so.FindProperty("blocker").objectReferenceValue = blocker.gameObject;
            so.ApplyModifiedProperties();

            if (createEventSystem) EnsureEventSystem(parent);
            return root;
        }

        /// <summary>Một EventSystem cho UI. Đưa về parent nếu đang nằm chỗ khác.</summary>
        public static void EnsureEventSystem(Transform parent)
        {
            var current = FindEventSystem();
            if (current == null)
            {
                EditorApplication.ExecuteMenuItem("GameObject/UI/Event System");
                current = FindEventSystem();
            }
            if (current == null) return;
            if (parent != null && current.transform.parent != parent)
                Undo.SetTransformParent(current.transform, parent, "Create UI Root");
        }

        // Tạo RectTransform kéo giãn toàn màn hình, không có Canvas.
        static RectTransform Stretched(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        // Tạo Canvas con kéo giãn toàn màn hình, có GraphicRaycaster riêng.
        static RectTransform StretchedCanvas(string name, Transform parent, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");

            var c = go.GetComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = sortOrder;

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static EventSystem FindEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
#else
            return Object.FindObjectOfType<EventSystem>(true);
#endif
        }
    }
}
    
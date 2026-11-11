using UnityEngine;
using UnityEngine.UI;

namespace LSDK.UISystem
{
    /// <summary>
    /// Gắn component này vào child object chứa SkeletonGraphic (Spine).
    /// SpineHolder tự thêm một Canvas + GraphicRaycaster riêng cho object đó,
    /// cô lập mesh rebuild của Spine khỏi phần còn lại của panel.
    ///
    /// Hướng dẫn:
    ///   1. Trong prefab UI, tạo child "SpineHolder" (RectTransform, anchored toàn phần hoặc tuỳ vị trí).
    ///   2. Gắn component SpineHolder vào child đó.
    ///   3. Gắn SkeletonGraphic vào cùng object hoặc child của nó.
    ///   4. Tắt "raycastTarget" trên SkeletonGraphic để không tốn raycaster hit.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class SpineHolder : MonoBehaviour
    {
        [Tooltip("Bật nếu cần bắt click trên vùng Spine này.")]
        [SerializeField] bool enableRaycast = false;

        Canvas _canvas;
        GraphicRaycaster _raycaster;

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.overrideSorting = false;   // kế thừa sortingOrder từ layer canvas cha.

            _raycaster = GetComponent<GraphicRaycaster>();
            if (enableRaycast)
            {
                if (_raycaster == null) _raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
            else
            {
                if (_raycaster != null) Destroy(_raycaster);
            }
        }

#if UNITY_EDITOR
        // Tự setup trong Editor khi add component, không cần tay.
        void Reset()
        {
            var c = GetComponent<Canvas>();
            if (c == null) c = gameObject.AddComponent<Canvas>();
            c.overrideSorting = false;

            // Xoá raycaster nếu không cần (mặc định enableRaycast = false).
            var r = GetComponent<GraphicRaycaster>();
            if (r != null) DestroyImmediate(r);
        }
#endif
    }
}

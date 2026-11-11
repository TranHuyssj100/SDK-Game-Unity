using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.UISystem
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] UIRegistry registry;
        [SerializeField] Transform[] layerRoots = new Transform[4];     // index theo UILayer
        [SerializeField] Canvas[] layerCanvases = new Canvas[4];        // Canvas của từng layer
        [SerializeField] GameObject blocker;                             // Image raycast chặn input
        [SerializeField] bool dontDestroyOnLoad = true;

        readonly Dictionary<int, UIPanel> _instances = new Dictionary<int, UIPanel>();
        readonly List<UIPanel> _screens = new List<UIPanel>();
        readonly List<UIPanel> _popups = new List<UIPanel>();

        // Đếm số popup/blocker nền đục đang mở để biết có cần tắt Screen canvas không.
        int _opaqueOverlayCount;
        int _blockCount;

        public UIPanel TopScreen => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;
        public UIPanel TopPopup => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        Canvas ScreenCanvas => layerCanvases != null && layerCanvases.Length > (int)UILayer.Screen
            ? layerCanvases[(int)UILayer.Screen] : null;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(transform.root.gameObject);
            if (blocker != null) blocker.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) Back();
#endif
        }

        // ---------- Open ----------
        public void Open(Enum id, object data = null, Action<UIPanel> onOpened = null)
            => StartCoroutine(OpenRoutine(Convert.ToInt32(id), data, onOpened));

        public void Open<T>(object data = null, Action<T> onOpened = null) where T : UIPanel
        {
            if (registry == null || !registry.TryGetId(typeof(T), out var id))
            {
                Debug.LogError($"[UI] {typeof(T).Name} is not in the UIRegistry");
                return;
            }
            StartCoroutine(OpenRoutine(id, data, p => onOpened?.Invoke((T)p)));
        }

        // ---------- Close ----------
        public void Close(Enum id)
        {
            if (_instances.TryGetValue(Convert.ToInt32(id), out var p)) Close(p);
        }

        public void Close<T>() where T : UIPanel
        {
            if (registry != null && registry.TryGetId(typeof(T), out var id) && _instances.TryGetValue(id, out var p))
                Close(p);
        }

        public void Close(UIPanel panel)
        {
            if (panel != null) StartCoroutine(CloseRoutine(panel));
        }

        public void CloseAllPopups()
        {
            for (int i = _popups.Count - 1; i >= 0; i--) Close(_popups[i]);
        }

        // Escape / nút Back: popup trên cùng trước, rồi tới screen (nếu còn screen bên dưới).
        public bool Back()
        {
            if (_blockCount > 0) return false;
            var top = TopPopup ?? (_screens.Count > 1 ? TopScreen : null);
            if (top == null || !top.IsOpen) return false;
            top.OnBack();
            return true;
        }

        // ---------- Query / lifecycle ----------
        public T Get<T>() where T : UIPanel
        {
            if (registry != null && registry.TryGetId(typeof(T), out var id) && _instances.TryGetValue(id, out var p))
                return p as T;
            return null;
        }

        public bool IsOpen(Enum id) => _instances.TryGetValue(Convert.ToInt32(id), out var p) && p != null && p.IsOpen;

        public void Preload(Enum id) => GetOrCreate(Convert.ToInt32(id));

        public void Unload(Enum id)
        {
            int key = Convert.ToInt32(id);
            if (!_instances.TryGetValue(key, out var p)) return;
            _screens.Remove(p);
            _popups.Remove(p);
            _instances.Remove(key);
            if (p != null) Destroy(p.gameObject);
        }

        public void Block(bool on)
        {
            _blockCount = Mathf.Max(0, _blockCount + (on ? 1 : -1));
            if (blocker != null) blocker.SetActive(_blockCount > 0);
        }

        // ---------- Internal ----------
        UIPanel GetOrCreate(int id)
        {
            if (_instances.TryGetValue(id, out var inst) && inst != null) return inst;

            if (registry == null || !registry.TryGetPrefab(id, out var prefab))
            {
                Debug.LogError($"[UI] No prefab registered for id {id}");
                return null;
            }

            var panel = Instantiate(prefab, layerRoots[(int)prefab.Layer], false);
            panel.name = prefab.name;
            panel.Setup();
            _instances[id] = panel;
            return panel;
        }

        IEnumerator OpenRoutine(int id, object data, Action<UIPanel> onOpened)
        {
            var panel = GetOrCreate(id);
            if (panel == null || panel.IsOpen || panel.IsTransitioning) yield break;

            Block(true);
            if (panel.Layer == UILayer.Screen)
            {
                var prev = TopScreen;
                _screens.Add(panel);
                // Screen cũ ẩn đi: bật lại Canvas trước, animate hide, rồi tắt Canvas.
                if (prev != null)
                {
                    SetLayerCanvas(UILayer.Screen, true);
                    yield return prev.HideRoutine();
                }
            }
            else if (panel.Layer == UILayer.Popup || panel.Layer == UILayer.Blocker)
            {
                if (panel.Layer == UILayer.Popup) _popups.Add(panel);
                // Popup/Blocker nền đục → tắt Screen canvas để screen và Spine bên dưới
                // không gom lại mesh mỗi frame khi popup đang mở.
                if (panel.OpaqueBackground)
                {
                    _opaqueOverlayCount++;
                    SetLayerCanvas(UILayer.Screen, false);
                }
            }

            yield return panel.ShowRoutine(data);
            Block(false);
            onOpened?.Invoke(panel);
        }

        IEnumerator CloseRoutine(UIPanel panel)
        {
            if (!panel.IsOpen || panel.IsTransitioning) yield break;

            Block(true);
            bool wasTopScreen = panel == TopScreen;
            bool wasOpaque = panel.OpaqueBackground &&
                (panel.Layer == UILayer.Popup || panel.Layer == UILayer.Blocker);

            _screens.Remove(panel);
            _popups.Remove(panel);

            // Sắp đóng popup nền đục → bật Screen canvas lại trước khi animate hide
            // để screen hiện ra từ phía dưới popup đang đóng.
            if (wasOpaque)
            {
                _opaqueOverlayCount = Mathf.Max(0, _opaqueOverlayCount - 1);
                if (_opaqueOverlayCount == 0)
                    SetLayerCanvas(UILayer.Screen, true);
            }

            yield return panel.HideRoutine();

            // Screen dưới: bật Canvas lại nếu chưa bật (trường hợp không có popup che).
            if (wasTopScreen && TopScreen != null)
            {
                SetLayerCanvas(UILayer.Screen, true);
                yield return TopScreen.ShowRoutine(TopScreen.Data);
            }

            // Tắt Screen canvas sau khi screen animate xong nếu vẫn còn popup che kín.
            if (_opaqueOverlayCount > 0)
                SetLayerCanvas(UILayer.Screen, false);

            Block(false);
        }

        // Bật / tắt Canvas của một layer. Dùng Canvas.enabled thay SetActive
        // để không phải dựng lại object khi bật trở lại.
        void SetLayerCanvas(UILayer layer, bool enabled)
        {
            var canvas = layerCanvases != null && layerCanvases.Length > (int)layer
                ? layerCanvases[(int)layer] : null;
            if (canvas != null) canvas.enabled = enabled;
        }
    }
}

// Assets/SDK/SDKCore/ToastSystem/Runtime/ToastPanel.cs
using System.Collections;
using System.Collections.Generic;
using LSDK.Core.ObjectPools;
using LSDK.UISystem;
using UnityEngine;

namespace LSDK.ToastSystem
{
    /// <summary>
    /// Toast trên layer Overlay của UISystem. Không chặn click, không vào stack Back.
    /// Mỗi dòng lấy từ <see cref="ObjectPool"/> (cùng pool SDK), prefab gắn trên panel
    /// nên không cần khai báo trong PoolDatabase.
    /// </summary>
    public class ToastPanel : UIPanel
    {
        const string LogPrefix = "[LSDK.ToastSystem]";

        [Header("Pool")]
        [Tooltip("Prefab ToastItem. ObjectPool của panel dùng prefab này.")]
        [SerializeField] ToastItem _toastPrefab;
        [Tooltip("Nơi hiện toast đang chạy. Để trống thì dùng chính panel.")]
        [SerializeField] Transform _toastContainer;
        [Tooltip("Nơi giữ item đang nằm trong pool. Để trống thì panel tự tạo.")]
        [SerializeField] Transform _poolRoot;
        [SerializeField, Min(0)] int _prewarm = 4;
        [Tooltip("0 = không giới hạn.")]
        [SerializeField, Min(0)] int _maxSize = 16;

        [Header("Motion")]
        [SerializeField, Min(0.01f)] float _popDuration = 0.25f;
        [SerializeField, Min(0.01f)] float _floatDuration = 1.5f;
        [SerializeField] float _startAnchorY = 300f;
        [SerializeField] float _endAnchorY = 450f;
        [Tooltip("Khoảng cách dọc giữa các toast đang hiện cùng lúc.")]
        [SerializeField, Min(0f)] float _stackSpacing = 90f;
        [SerializeField] Vector3 _startScale = new Vector3(0.8f, 0f, 1f);

        static readonly Queue<ToastRequest> Pending = new Queue<ToastRequest>();

        readonly List<ToastItem> _active = new List<ToastItem>();
        ObjectPool _pool;
        CanvasGroup _panelGroup;
        Coroutine _closeRoutine;

        struct ToastRequest
        {
            public string Message;
            public ToastType Type;
        }

        void Reset()
        {
            Layer = UILayer.Overlay;
            CloseOnBack = false;
            OpaqueBackground = false;
        }

        void OnValidate()
        {
            Layer = UILayer.Overlay;
            CloseOnBack = false;
            OpaqueBackground = false;
        }

        void Awake()
        {
            _panelGroup = GetComponent<CanvasGroup>();
        }

        void OnDisable()
        {
            StopCloseRoutine();
        }

        void OnDestroy()
        {
            StopCloseRoutine();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null)
                    _active[i].Cancel();
            }
            _active.Clear();
            _pool?.Clear(true);
            _pool = null;
        }

        /// <summary>Hiện toast. Normal trắng, Success xanh, Warning vàng, Error đỏ.</summary>
        public static void Show(string message, ToastType type)
        {
            if (string.IsNullOrEmpty(message))
                return;

            var ui = UIManager.Instance;
            if (ui == null)
            {
                Debug.LogWarning(LogPrefix + " UIManager chưa sẵn sàng.");
                return;
            }

            if (!ui.Contains<ToastPanel>())
            {
                Debug.LogWarning(LogPrefix + " ToastPanel chưa có trong UIRegistry.");
                return;
            }

            var panel = ui.Get<ToastPanel>();
            if (panel != null && panel.IsOpen && !panel.IsTransitioning)
            {
                panel.Push(message, type);
                return;
            }

            Pending.Enqueue(new ToastRequest { Message = message, Type = type });
            if (panel == null || (!panel.IsOpen && !panel.IsTransitioning))
                ui.Open<ToastPanel>();
        }

        protected override void OnOpen(object data)
        {
            PassThrough();
            EnsurePool();
        }

        protected override void OnShown()
        {
            PassThrough();
            while (Pending.Count > 0)
            {
                var request = Pending.Dequeue();
                Push(request.Message, request.Type);
            }
        }

        protected override void OnHide()
        {
            StopCloseRoutine();
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                if (item == null)
                    continue;
                item.Cancel();
                if (_pool != null)
                    _pool.ReturnObject(item.gameObject);
            }
            _active.Clear();
        }

        protected override void OnClosed()
        {
            if (Pending.Count > 0 && UIManager.Instance != null)
                UIManager.Instance.Open<ToastPanel>();
        }

        void Push(string message, ToastType type)
        {
            StopCloseRoutine();
            EnsurePool();
            if (_pool == null)
                return;

            var instance = _pool.GetObject();
            if (instance == null)
                return;

            var item = instance.GetComponent<ToastItem>();
            if (item == null)
            {
                Debug.LogError(LogPrefix + " Prefab pool không có ToastItem.");
                _pool.ReturnObject(instance);
                return;
            }

            var parent = _toastContainer != null ? _toastContainer : transform;
            instance.transform.SetParent(parent, false);
            instance.transform.SetAsLastSibling();

            float slot = _active.Count * _stackSpacing;
            _active.Add(item);
            item.Play(message, type, new ToastMotionSettings
            {
                PopDuration = _popDuration,
                FloatDuration = _floatDuration,
                StartAnchorY = _startAnchorY - slot,
                EndAnchorY = _endAnchorY + slot,
                StartScale = _startScale
            }, () => Finish(item));
        }

        void Finish(ToastItem item)
        {
            if (!_active.Remove(item))
                return;
            if (_pool != null && item != null)
                _pool.ReturnObject(item.gameObject);
            if (_active.Count == 0 && IsOpen && !IsTransitioning)
                _closeRoutine = StartCoroutine(CloseNextFrame());
        }

        IEnumerator CloseNextFrame()
        {
            yield return null;
            _closeRoutine = null;
            if (_active.Count == 0 && IsOpen && !IsTransitioning)
                Close();
        }

        void EnsurePool()
        {
            if (_pool != null)
                return;
            if (_toastPrefab == null)
            {
                Debug.LogError(LogPrefix + " Chưa gán prefab ToastItem.");
                return;
            }

            if (_poolRoot == null)
            {
                var rootObject = new GameObject("ToastPool", typeof(RectTransform));
                rootObject.transform.SetParent(transform, false);
                _poolRoot = rootObject.transform;
            }

            var entry = new PoolDatabaseSO.Entry
            {
                id = 1,
                name = "ToastItem",
                prefab = _toastPrefab.gameObject,
                initialSize = _prewarm,
                maxSize = _maxSize,
                expandable = true,
                collectionCheck = true
            };
            _pool = new ObjectPool(entry, _poolRoot);
        }

        void PassThrough()
        {
            if (_panelGroup == null)
                _panelGroup = GetComponent<CanvasGroup>();
            if (_panelGroup == null)
                return;
            _panelGroup.blocksRaycasts = false;
            _panelGroup.interactable = false;
        }

        void StopCloseRoutine()
        {
            if (_closeRoutine == null)
                return;
            StopCoroutine(_closeRoutine);
            _closeRoutine = null;
        }
    }
}

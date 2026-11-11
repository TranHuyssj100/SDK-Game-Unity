using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace LSDK.UISystem
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(UIAnimator))]
    public abstract class UIPanel : MonoBehaviour
    {
        [Header("UIPanel")]
        public UILayer Layer = UILayer.Popup;
        public bool CloseOnBack = true;
        [Tooltip("Bật nếu panel này che kín toàn màn hình (nền đục). " +
                 "UIManager sẽ tắt Canvas Screen khi panel này mở, " +
                 "giảm gom lại mesh cho các layer bên dưới.")]
        public bool OpaqueBackground = false;

        CanvasGroup _group;
        UIAnimator _animator;

        public bool IsOpen { get; private set; }
        public bool IsTransitioning { get; private set; }
        public object Data { get; private set; }

        public event Action Shown;
        public event Action Closed;

        internal void Setup()
        {
            _group = GetComponent<CanvasGroup>();
            _animator = GetComponentInChildren<UIAnimator>(true);
            gameObject.SetActive(false);
        }

        internal IEnumerator ShowRoutine(object data)
        {
            IsTransitioning = true;
            Data = data;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _group.interactable = false;
            OnOpen(data);

            if (_animator != null)
            {
                var t = _animator.PlayShow();
                if (t != null) yield return t.WaitForCompletion();
            }

            _group.interactable = true;
            IsOpen = true;
            IsTransitioning = false;
            OnShown();
            Shown?.Invoke();
        }

        internal IEnumerator HideRoutine()
        {
            IsTransitioning = true;
            IsOpen = false;
            _group.interactable = false;
            OnHide();

            if (_animator != null)
            {
                var t = _animator.PlayHide();
                if (t != null) yield return t.WaitForCompletion();
            }

            gameObject.SetActive(false);
            IsTransitioning = false;
            OnClosed();
            Closed?.Invoke();
        }

        protected virtual void OnOpen(object data) { }   // trước animation show
        protected virtual void OnShown() { }             // sau animation show
        protected virtual void OnHide() { }              // bắt đầu animation hide
        protected virtual void OnClosed() { }            // đã tắt hẳn

        public virtual void OnBack()
        {
            if (CloseOnBack) UIManager.Instance.Close(this);
        }

        // Gắn vào Button "X"
        public void Close() => UIManager.Instance.Close(this);
    }
}

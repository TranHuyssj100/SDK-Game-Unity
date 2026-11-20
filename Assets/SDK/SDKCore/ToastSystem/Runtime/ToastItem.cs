// Assets/SDK/SDKCore/ToastSystem/Runtime/ToastItem.cs
using System;
using DG.Tweening;
using LSDK.Core.ObjectPools;
using TMPro;
using UnityEngine;

namespace LSDK.ToastSystem
{
    /// <summary>
    /// Một dòng toast. Prefab này là mẫu của <see cref="ObjectPool"/> do <see cref="ToastPanel"/> giữ.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastItem : MonoBehaviour, IPooledObject
    {
        [Header("Nội dung")]
        [SerializeField] TMP_Text _label;

        [Header("Màu chữ")]
        [SerializeField] Color _normalColor = Color.white;
        [SerializeField] Color _successColor = new Color(0.2f, 0.9f, 0.35f, 1f);
        [SerializeField] Color _warningColor = new Color(1f, 0.86f, 0.2f, 1f);
        [SerializeField] Color _errorColor = new Color(1f, 0.27f, 0.27f, 1f);

        RectTransform _root;
        CanvasGroup _group;
        Sequence _sequence;
        Action _onComplete;

        void Awake()
        {
            _root = transform as RectTransform;
            _group = GetComponent<CanvasGroup>();
            if (_label == null)
                Debug.LogError("[LSDK.ToastSystem] ToastItem thiếu TMP_Text.");
            if (_root == null)
                Debug.LogError("[LSDK.ToastSystem] ToastItem phải nằm trên RectTransform.");
        }

        void OnDestroy()
        {
            _onComplete = null;
            KillTween();
        }

        /// <summary>Pool đưa object ra. Xóa chữ và tween cũ.</summary>
        public void OnObjectGet()
        {
            _onComplete = null;
            KillTween();
            ResetVisual();
        }

        /// <summary>Pool thu object về. Dừng tween, không gọi callback.</summary>
        public void OnObjectReturn()
        {
            _onComplete = null;
            KillTween();
            ResetVisual();
        }

        /// <summary>Hiện chữ và chạy pop rồi nổi lên mờ dần.</summary>
        internal void Play(string message, ToastType type, ToastMotionSettings motion, Action onComplete)
        {
            _onComplete = onComplete;
            if (_label != null)
            {
                _label.text = message ?? string.Empty;
                _label.color = ColorOf(type);
            }

            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }

            if (_root != null)
            {
                _root.localScale = motion.StartScale;
                _root.anchoredPosition = new Vector2(0f, motion.StartAnchorY);
            }

            float pop = Mathf.Max(0.01f, motion.PopDuration);
            float drift = Mathf.Max(0.01f, motion.FloatDuration);
            KillTween();

            var sequence = DOTween.Sequence();
            sequence.SetUpdate(true);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (_group != null)
                sequence.Join(Fade(_group, 1f, pop));
            if (_root != null)
                sequence.Join(Scale(_root, Vector3.one, pop).SetEase(Ease.OutBack));

            if (_root != null)
                sequence.Append(AnchorY(_root, motion.EndAnchorY, drift).SetEase(Ease.InCubic));
            else
                sequence.AppendInterval(drift);

            if (_group != null)
                sequence.Join(Fade(_group, 0f, drift).SetEase(Ease.InCirc));

            sequence.OnComplete(HandleComplete);
            _sequence = sequence;
        }

        /// <summary>Dừng tween và không báo panel.</summary>
        public void Cancel()
        {
            _onComplete = null;
            KillTween();
        }

        Color ColorOf(ToastType type)
        {
            switch (type)
            {
                case ToastType.Success: return _successColor;
                case ToastType.Warning: return _warningColor;
                case ToastType.Error: return _errorColor;
                default: return _normalColor;
            }
        }

        void HandleComplete()
        {
            _sequence = null;
            var callback = _onComplete;
            _onComplete = null;
            callback?.Invoke();
        }

        void KillTween()
        {
            if (_sequence != null && _sequence.IsActive())
                _sequence.Kill();
            _sequence = null;
        }

        void ResetVisual()
        {
            if (_label != null)
            {
                _label.text = string.Empty;
                _label.color = _normalColor;
            }

            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }

            if (_root != null)
            {
                _root.localScale = Vector3.one;
                _root.anchoredPosition = Vector2.zero;
            }
        }

        static Tween Scale(RectTransform rect, Vector3 end, float duration)
        {
            return DOTween.To(() => rect.localScale, value => rect.localScale = value, end, duration).SetTarget(rect);
        }

        static Tween AnchorY(RectTransform rect, float endY, float duration)
        {
            var end = new Vector2(0f, endY);
            return DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, end, duration).SetTarget(rect);
        }

        static Tween Fade(CanvasGroup group, float end, float duration)
        {
            return DOTween.To(() => group.alpha, value => group.alpha = value, end, duration).SetTarget(group);
        }
    }

    /// <summary>Thông số animation của một dòng toast. Panel điền, item chỉ chạy.</summary>
    internal struct ToastMotionSettings
    {
        public float PopDuration;
        public float FloatDuration;
        public float StartAnchorY;
        public float EndAnchorY;
        public Vector3 StartScale;
    }
}

using System.Runtime.CompilerServices;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[assembly: InternalsVisibleTo("LSDK.UISystem.Editor")]

namespace LSDK.UISystem
{
    [DisallowMultipleComponent]
    public class UIAnimator : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Nội dung cần animate. Để trống = chính object này.")]
        [SerializeField] RectTransform target;
        [Tooltip("Lớp nền mờ (optional), fade cùng panel.")]
        [SerializeField] Image background;
        [SerializeField, Range(0f, 1f)] float backgroundAlpha = 0.95f;

        [Header("Animation")]
        [Tooltip("Bảng thông số của mọi motion. Để trống thì dùng thông số mặc định.")]
        [SerializeField] UIAnimationPreset preset;
        [Tooltip("Kiểu animation lúc mở và đóng. Show/Hide bên dưới là thông số của kiểu này.")]
        [SerializeField] UIMotion motion = UIMotion.Scale;
        [ReadOnly, SerializeField] UIAnimConfig show = UIAnimConfig.DefaultShow();
        [ReadOnly, SerializeField] UIAnimConfig hide = UIAnimConfig.DefaultHide();
        [Tooltip("Chạy ngay cả khi Time.timeScale = 0.")]
        [SerializeField] bool ignoreTimeScale = true;
        [SerializeField] float fallbackSlideDistance = 1000f;

        RectTransform _rt;
        CanvasGroup _group;
        Vector2 _basePos;
        Vector3 _baseScale;
        bool _inited;
        Tween _tween;

        public bool IsPlaying => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        void Init()
        {
            if (_inited) return;
            _rt = target != null ? target : (RectTransform)transform;
            RefreshConfigs();
            _basePos = _rt.anchoredPosition;      // vị trí thiết kế = mốc
            _baseScale = _rt.localScale;
            _group = _rt.GetComponent<CanvasGroup>();
            if (_group == null) _group = _rt.gameObject.AddComponent<CanvasGroup>();
            _inited = true;
        }

        // Trả null nếu config không có gì để chạy (hiện/ẩn tức thì).
        public Tween PlayShow()
        {
            Init();
            Kill();
            RefreshConfigs();
            var cfg = show;
            var seq = DOTween.Sequence();
            bool any = false;

            _rt.anchoredPosition = _basePos;
            _rt.localScale = _baseScale;
            _group.alpha = 1f;

            Vector2 off = Offset(motion);
            if (off != Vector2.zero)
            {
                _rt.anchoredPosition = _basePos + off;
                seq.Join(AnchorPos(_rt, _basePos, cfg.duration).SetEase(cfg.ease));
                any = true;
            }
            else if (motion == UIMotion.Scale)
            {
                _rt.localScale = _baseScale * cfg.scale;
                seq.Join(_rt.DOScale(_baseScale, cfg.duration).SetEase(cfg.ease));
                any = true;
            }

            if (cfg.fade)
            {
                _group.alpha = 0f;
                seq.Join(Fade(_group, 1f, cfg.duration).SetEase(Ease.Linear));
                any = true;
            }

            if (background != null)
            {
                SetAlpha(background, 0f);
                seq.Join(Fade(background, backgroundAlpha, cfg.duration).SetEase(Ease.Linear));
                any = true;
            }

            return Finish(seq, cfg, any);
        }

        public Tween PlayHide()
        {
            Init();
            Kill();
            RefreshConfigs();
            var cfg = hide;
            var seq = DOTween.Sequence();
            bool any = false;

            Vector2 off = Offset(motion);
            if (off != Vector2.zero)
            {
                seq.Join(AnchorPos(_rt, _basePos + off, cfg.duration).SetEase(cfg.ease));
                any = true;
            }
            else if (motion == UIMotion.Scale)
            {
                seq.Join(_rt.DOScale(_baseScale * cfg.scale, cfg.duration).SetEase(cfg.ease));
                any = true;
            }

            if (cfg.fade)
            {
                seq.Join(Fade(_group, 0f, cfg.duration).SetEase(Ease.Linear));
                any = true;
            }

            if (background != null)
            {
                seq.Join(Fade(background, 0f, cfg.duration).SetEase(Ease.Linear));
                any = true;
            }

            return Finish(seq, cfg, any);
        }

        void OnValidate() => RefreshConfigs();

        internal void RefreshConfigs()
        {
            var nextShow = preset != null ? preset.ShowOf(motion).Clone() : UIAnimConfig.ForShow(motion);
            var nextHide = preset != null ? preset.HideOf(motion).Clone() : UIAnimConfig.ForHide(motion);
            if (show == null || !show.Matches(nextShow)) show = nextShow;
            if (hide == null || !hide.Matches(nextHide)) hide = nextHide;
        }

        public void Kill(bool complete = false)
        {
            _tween?.Kill(complete);
            _tween = null;
        }

        Tween Finish(Sequence seq, UIAnimConfig cfg, bool any)
        {
            if (!any) { seq.Kill(); return null; }
            if (cfg.delay > 0f) seq.PrependInterval(cfg.delay);
            seq.SetUpdate(ignoreTimeScale).SetLink(gameObject).OnComplete(() => _tween = null);
            _tween = seq;
            return seq;
        }

        Vector2 Offset(UIMotion m)
        {
            switch (m)
            {
                case UIMotion.SlideLeft:   return new Vector2(-Extent(true), 0f);
                case UIMotion.SlideRight:  return new Vector2(Extent(true), 0f);
                case UIMotion.SlideTop:    return new Vector2(0f, Extent(false));
                case UIMotion.SlideBottom: return new Vector2(0f, -Extent(false));
                default:                   return Vector2.zero;
            }
        }

        // Kích thước Canvas gốc, đủ để đẩy panel ra khỏi màn hình ở mọi resolution.
        float Extent(bool horizontal)
        {
            var canvas = _rt.GetComponentInParent<Canvas>();
            if (canvas == null) return fallbackSlideDistance;
            var rect = ((RectTransform)canvas.rootCanvas.transform).rect;
            return horizontal ? rect.width : rect.height;
        }

        // DOAnchorPos / DOFade(UI) nằm trong DOTweenModuleUI (Assembly-CSharp).
        // Assembly này không reference được module đó, nên tween bằng API trong DOTween.dll.
        static Tween AnchorPos(RectTransform rt, Vector2 end, float duration)
        {
            return DOTween.To(() => rt.anchoredPosition, v => rt.anchoredPosition = v, end, duration).SetTarget(rt);
        }

        static Tween Fade(CanvasGroup group, float end, float duration)
        {
            return DOTween.To(() => group.alpha, a => group.alpha = a, end, duration).SetTarget(group);
        }

        static Tween Fade(Graphic graphic, float end, float duration)
        {
            return DOTween.ToAlpha(() => graphic.color, c => graphic.color = c, end, duration).SetTarget(graphic);
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        void OnDestroy() => Kill();
    }
}

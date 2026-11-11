using System;
using DG.Tweening;
using UnityEngine;

namespace LSDK.UISystem
{
    // Thông số của một chiều animation (show hoặc hide). Không chọn motion ở đây.
    [Serializable]
    public class UIAnimConfig
    {
        [Tooltip("Fade alpha cùng lúc với motion.")]
        public bool fade = false;
        public Ease ease = Ease.OutQuad;
        [Min(0f)] public float duration = 0.3f;
        [Min(0f)] public float delay = 0f;
        [Tooltip("Scale lúc bắt đầu (Show) / kết thúc (Hide). Chỉ dùng cho motion Scale.")]
        [Range(0f, 1.5f)] public float scale = 0.6f;

        public static UIAnimConfig DefaultShow() => ForShow(UIMotion.Scale);
        public static UIAnimConfig DefaultHide() => ForHide(UIMotion.Scale);

        // None và Scale có mặc định riêng. Type mới dùng mặc định chung, rồi chỉnh trên preset.
        public static UIAnimConfig ForShow(UIMotion motion)
        {
            if (motion == UIMotion.None) return Make(Ease.Linear, 0f, 1f);
            if (motion == UIMotion.Scale) return Make(Ease.OutBack, 0.3f, 0.6f);
            return Make(Ease.OutCubic, 0.3f, 1f);
        }

        public static UIAnimConfig ForHide(UIMotion motion)
        {
            if (motion == UIMotion.None) return Make(Ease.Linear, 0f, 1f);
            if (motion == UIMotion.Scale) return Make(Ease.InBack, 0.3f, 0.6f);
            return Make(Ease.InCubic, 0.3f, 1f);
        }

        public UIAnimConfig Clone() => new UIAnimConfig
        {
            fade = fade,
            ease = ease,
            duration = duration,
            delay = delay,
            scale = scale
        };

        public bool Matches(UIAnimConfig other)
        {
            return other != null
                && fade == other.fade
                && ease == other.ease
                && Mathf.Approximately(duration, other.duration)
                && Mathf.Approximately(delay, other.delay)
                && Mathf.Approximately(scale, other.scale);
        }

        static UIAnimConfig Make(Ease ease, float duration, float scale) =>
            new UIAnimConfig { fade = false, ease = ease, duration = duration, delay = 0f, scale = scale };
    }
}

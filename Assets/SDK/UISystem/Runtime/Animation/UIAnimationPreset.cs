using System;
using UnityEngine;

namespace LSDK.UISystem
{
    [Serializable]
    public class UIMotionSetting
    {
        public UIMotion motion;
        public UIAnimConfig show = new UIAnimConfig();
        public UIAnimConfig hide = new UIAnimConfig();
    }

    [CreateAssetMenu(menuName = "LSDK/UI System/Animation Preset", fileName = "UIAnimationPreset")]
    public class UIAnimationPreset : ScriptableObject
    {
        [SerializeField] UIMotionSetting[] motions = CreateDefaults();

        public UIAnimConfig ShowOf(UIMotion motion)
        {
            var setting = Find(motion);
            return setting != null && setting.show != null ? setting.show : UIAnimConfig.ForShow(motion);
        }

        public UIAnimConfig HideOf(UIMotion motion)
        {
            var setting = Find(motion);
            return setting != null && setting.hide != null ? setting.hide : UIAnimConfig.ForHide(motion);
        }

        // Thêm type mới, bỏ type đã xoá. Giữ nguyên thông số của type còn lại.
        public void UpdateMotions()
        {
            var values = (UIMotion[])Enum.GetValues(typeof(UIMotion));
            var next = new UIMotionSetting[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                var existing = Find(values[i]);
                if (existing == null)
                    next[i] = CreateSetting(values[i]);
                else
                {
                    existing.motion = values[i];
                    if (existing.show == null) existing.show = UIAnimConfig.ForShow(values[i]);
                    if (existing.hide == null) existing.hide = UIAnimConfig.ForHide(values[i]);
                    next[i] = existing;
                }
            }
            motions = next;
        }

        // Danh sách type theo UIMotion, mọi thông số về mặc định.
        public void ResetMotions() => motions = CreateDefaults();

        UIMotionSetting Find(UIMotion motion)
        {
            if (motions == null) return null;
            for (int i = 0; i < motions.Length; i++)
                if (motions[i] != null && motions[i].motion == motion)
                    return motions[i];
            return null;
        }

        static UIMotionSetting[] CreateDefaults()
        {
            var values = (UIMotion[])Enum.GetValues(typeof(UIMotion));
            var list = new UIMotionSetting[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                list[i] = CreateSetting(values[i]);
            }
            return list;
        }

        static UIMotionSetting CreateSetting(UIMotion motion) => new UIMotionSetting
        {
            motion = motion,
            show = UIAnimConfig.ForShow(motion),
            hide = UIAnimConfig.ForHide(motion)
        };
    }
}

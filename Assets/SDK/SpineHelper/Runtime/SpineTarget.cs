using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace LSDK.SpineAnim
{
    /// <summary>Wrapper gom SkeletonAnimation (world) và SkeletonGraphic (UI) về 1 API.</summary>
    public sealed class SpineTarget
    {
        private readonly SkeletonAnimation _anim;
        private readonly SkeletonGraphic _graphic;

        private SpineTarget(SkeletonAnimation a, SkeletonGraphic g)
        {
            _anim = a;
            _graphic = g;
        }

        public static SpineTarget Find(GameObject go)
        {
            // Spine 4.3: SkeletonAnimation giữ AnimationState; SkeletonGraphic/SkeletonRenderer chỉ render.
            var a = go.GetComponent<SkeletonAnimation>();
            var g = go.GetComponent<SkeletonGraphic>();
            if (a == null && g == null) return null;
            return new SpineTarget(a, g);
        }

        public bool IsAlive => _anim != null || _graphic != null;

        public SkeletonDataAsset DataAsset =>
            _anim != null ? _anim.skeletonDataAsset : (_graphic != null ? _graphic.skeletonDataAsset : null);

        public bool IsInitialized =>
            _anim != null ? _anim.IsValid : (_graphic != null && _graphic.IsValid);

        public void EnsureInitialized()
        {
            if (_anim != null) { if (!_anim.IsValid) _anim.Initialize(false); }
            else if (_graphic != null) { if (!_graphic.IsValid) _graphic.Initialize(false); }
        }

        public Skeleton Skeleton =>
            _anim != null ? _anim.Skeleton : (_graphic != null ? _graphic.Skeleton : null);

        public AnimationState State => _anim != null ? _anim.AnimationState : null;

        public SkeletonData GetData()
        {
            var da = DataAsset;
            return da != null ? da.GetSkeletonData(true) : null;
        }

        /// <summary>Update + render thủ công (dùng cho preview ở edit mode).</summary>
        public void Tick(float dt)
        {
            if (!IsInitialized) return;
            if (_anim != null) _anim.Update(dt);
            if (_graphic != null) _graphic.LateUpdate();
            else if (_anim != null && _anim.Renderer != null) _anim.Renderer.LateUpdate();
        }
    }
}

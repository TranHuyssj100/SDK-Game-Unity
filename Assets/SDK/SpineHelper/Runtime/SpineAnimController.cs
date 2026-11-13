using System;
using System.Collections.Generic;
using Spine;
using UnityEngine;
using AnimationState = Spine.AnimationState;

namespace LSDK.SpineAnim
{
    public enum SpineAutoPlayMode { None, Animation, Sequence }

    /// <summary>
    /// Add thẳng vào object có SkeletonAnimation hoặc SkeletonGraphic (Spine 4.3).
    /// - Mix nhiều skin thành 1 skin combined
    /// - Play / mix animation (nhiều track, alpha, mix duration)
    /// - Sequence: chuỗi animation chạy liên tục
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LSDK/Spine/Spine Anim Controller")]
    public class SpineAnimController : MonoBehaviour
    {
        [SerializeField] private bool autoApplyOnStart = true;
        [SerializeField] private bool includeDefaultSkin = true;
        [SerializeField] private List<string> selectedSkins = new List<string>();
        [SerializeField] private float defaultMix = 0.2f;

        [SerializeField] private SpineAutoPlayMode autoPlayMode = SpineAutoPlayMode.None;
        [SerializeField] private string autoPlayAnimation;
        [SerializeField] private bool autoPlayLoop = true;
        [SerializeField] private string autoPlaySequence;

        [SerializeField] private List<SpineSequence> sequences = new List<SpineSequence>();

        public event Action<SpineSequence, int> SequenceStepStarted;
        public event Action<SpineSequence> SequenceCompleted;

        [NonSerialized] private SpineTarget _target;
        [NonSerialized] private SkeletonData _cachedData;
        [NonSerialized] private string[] _animNames = Array.Empty<string>();
        [NonSerialized] private string[] _skinNames = Array.Empty<string>();

        // sequence runtime
        private SpineSequence _seq;
        private int _stepIndex;
        private int _loopsDone;
        private TrackEntry _entry;

        #region Properties

        public SpineTarget Target
        {
            get
            {
                if (_target == null || !_target.IsAlive) _target = SpineTarget.Find(gameObject);
                return _target;
            }
        }

        public bool IncludeDefaultSkin
        {
            get => includeDefaultSkin;
            set { includeDefaultSkin = value; }
        }

        public float DefaultMix { get => defaultMix; set => defaultMix = Mathf.Max(0f, value); }
        public IReadOnlyList<string> SelectedSkins => selectedSkins;
        public IReadOnlyList<SpineSequence> Sequences => sequences;
        public bool IsSequencePlaying => _seq != null;
        public SpineSequence CurrentSequence => _seq;

        #endregion

        #region Unity

        private void Start()
        {
            if (Target == null) return;
            if (autoApplyOnStart) ApplySkins();

            switch (autoPlayMode)
            {
                case SpineAutoPlayMode.Animation:
                    if (!string.IsNullOrEmpty(autoPlayAnimation)) Play(autoPlayAnimation, autoPlayLoop);
                    break;
                case SpineAutoPlayMode.Sequence:
                    if (!string.IsNullOrEmpty(autoPlaySequence)) PlaySequence(autoPlaySequence);
                    break;
            }
        }

        private void OnDisable()
        {
            _seq = null;
            _entry = null;
        }

        #endregion

        #region Data query

        public SkeletonData GetData()
        {
            var t = Target;
            return t != null ? t.GetData() : null;
        }

        private void RebuildCache()
        {
            var d = GetData();
            if (d == _cachedData && d != null) return;
            _cachedData = d;
            if (d == null)
            {
                _animNames = Array.Empty<string>();
                _skinNames = Array.Empty<string>();
                return;
            }

            _animNames = new string[d.Animations.Count];
            for (int i = 0; i < _animNames.Length; i++) _animNames[i] = d.Animations.Items[i].Name;

            var skins = new List<string>();
            for (int i = 0; i < d.Skins.Count; i++)
            {
                var s = d.Skins.Items[i];
                if (s == d.DefaultSkin) continue; // default skin điều khiển bằng includeDefaultSkin
                skins.Add(s.Name);
            }
            _skinNames = skins.ToArray();
        }

        public string[] GetAnimationNames() { RebuildCache(); return _animNames; }

        /// <summary>Tên tất cả skin (không gồm "default").</summary>
        public string[] GetSkinNames() { RebuildCache(); return _skinNames; }

        public float GetAnimationDuration(string animationName)
        {
            var d = GetData();
            if (d == null || string.IsNullOrEmpty(animationName)) return 0f;
            var a = d.FindAnimation(animationName);
            return a != null ? a.Duration : 0f;
        }

        private bool HasAnimation(string animationName)
        {
            var d = GetData();
            return d != null && !string.IsNullOrEmpty(animationName) && d.FindAnimation(animationName) != null;
        }

        #endregion

        #region Skin mixing

        public bool IsSkinSelected(string skinName) => selectedSkins.Contains(skinName);

        public void SetSkinSelected(string skinName, bool selected, bool apply = true)
        {
            bool has = selectedSkins.Contains(skinName);
            if (selected && !has) selectedSkins.Add(skinName);
            else if (!selected && has) selectedSkins.Remove(skinName);
            else return;
            if (apply) ApplySkins();
        }

        public void ToggleSkin(string skinName, bool apply = true) =>
            SetSkinSelected(skinName, !IsSkinSelected(skinName), apply);

        public void SetSkins(IEnumerable<string> skinNames, bool apply = true)
        {
            selectedSkins.Clear();
            if (skinNames != null)
                foreach (var n in skinNames)
                    if (!string.IsNullOrEmpty(n) && !selectedSkins.Contains(n)) selectedSkins.Add(n);
            if (apply) ApplySkins();
        }

        public void SelectAllSkins(bool apply = true) => SetSkins(GetSkinNames(), apply);
        public void ClearSkins(bool apply = true) => SetSkins(null, apply);

        /// <summary>Gộp default (tuỳ chọn) + các skin đã chọn (theo thứ tự, skin sau đè skin trước) rồi set lên skeleton.</summary>
        public void ApplySkins()
        {
            if (!EnsureReady()) return;
            var skeleton = Target.Skeleton;
            var data = skeleton.Data;

            var mixed = new Skin("lsdk-mixed");
            if (includeDefaultSkin && data.DefaultSkin != null) mixed.AddSkin(data.DefaultSkin);

            foreach (var name in selectedSkins)
            {
                var s = data.FindSkin(name);
                if (s == null)
                {
                    Debug.LogWarning($"[LSDK.SpineAnim] Skin '{name}' không tồn tại trong SkeletonData.", this);
                    continue;
                }
                if (s == data.DefaultSkin && includeDefaultSkin) continue;
                mixed.AddSkin(s);
            }

            skeleton.SetSkin(mixed);
            skeleton.SetupPoseSlots();
            Target.State.Apply(skeleton);
        }

        #endregion

        #region Animation

        private bool EnsureReady()
        {
            var t = Target;
            if (t == null || !t.IsAlive || t.DataAsset == null) return false;
            t.EnsureInitialized();
            var st = t.State;
            if (st == null) return false;
            st.Data.DefaultMix = defaultMix;
            return true;
        }

        /// <summary>Chạy animation trên 1 track. Track khác nhau sẽ chồng lên nhau (mix bằng alpha).</summary>
        public TrackEntry Play(string animationName, bool loop = true, int track = 0,
            float mixDuration = -1f, float alpha = 1f, float timeScale = 1f)
        {
            if (!EnsureReady()) return null;
            if (!HasAnimation(animationName))
            {
                Debug.LogWarning($"[LSDK.SpineAnim] Animation '{animationName}' không tồn tại.", this);
                return null;
            }

            if (_seq != null && _seq.track == track) StopSequence();

            var e = Target.State.SetAnimation(track, animationName, loop);
            e.MixDuration = mixDuration >= 0f ? mixDuration : defaultMix;
            e.Alpha = alpha;
            e.TimeScale = timeScale;
            return e;
        }

        /// <summary>Xếp animation vào hàng đợi sau animation hiện tại của track.</summary>
        public TrackEntry Queue(string animationName, bool loop = false, int track = 0,
            float delay = 0f, float mixDuration = -1f)
        {
            if (!EnsureReady() || !HasAnimation(animationName)) return null;
            var e = Target.State.AddAnimation(track, animationName, loop, delay);
            e.MixDuration = mixDuration >= 0f ? mixDuration : defaultMix;
            return e;
        }

        public void Stop(int track = 0, float mixDuration = 0f)
        {
            if (!EnsureReady()) return;
            if (_seq != null && _seq.track == track) StopSequence();
            Target.State.SetEmptyAnimation(track, mixDuration);
        }

        /// <summary>Clear hết track, trả skeleton về setup pose (giữ skin hiện tại).</summary>
        public void ResetPose()
        {
            if (!EnsureReady()) return;
            StopSequence();
            Target.State.ClearTracks();
            Target.Skeleton.SetupPose();
            Target.Tick(0f);
        }

        /// <summary>Update + render ngay 1 frame (dùng sau khi đổi skin ở edit mode).</summary>
        public void RefreshImmediate()
        {
            if (EnsureReady()) Target.Tick(0f);
        }

#if UNITY_EDITOR
        /// <summary>Editor gọi để chạy preview khi không ở play mode.</summary>
        public void EditorTick(float dt)
        {
            var t = Target;
            if (t != null && t.IsAlive && t.IsInitialized) t.Tick(dt);
        }
#endif

        #endregion

        #region Sequence

        public SpineSequence FindSequence(string sequenceName)
        {
            for (int i = 0; i < sequences.Count; i++)
                if (sequences[i].name == sequenceName) return sequences[i];
            return null;
        }

        public bool PlaySequence(string sequenceName)
        {
            var s = FindSequence(sequenceName);
            if (s == null)
            {
                Debug.LogWarning($"[LSDK.SpineAnim] Sequence '{sequenceName}' không tồn tại.", this);
                return false;
            }
            return PlaySequence(s);
        }

        public bool PlaySequence(int index)
        {
            if (index < 0 || index >= sequences.Count) return false;
            return PlaySequence(sequences[index]);
        }

        public bool PlaySequence(SpineSequence sequence)
        {
            if (sequence == null || !EnsureReady()) return false;

            bool anyValid = false;
            foreach (var st in sequence.steps)
                if (HasAnimation(st.animation)) { anyValid = true; break; }
            if (!anyValid)
            {
                Debug.LogWarning($"[LSDK.SpineAnim] Sequence '{sequence.name}' không có bước hợp lệ.", this);
                return false;
            }

            _seq = sequence;
            _stepIndex = 0;
            StartStep(0);
            return true;
        }

        /// <summary>Dừng sequence (animation hiện tại vẫn chạy nốt trên track).</summary>
        public void StopSequence()
        {
            _seq = null;
            _entry = null;
        }

        private void StartStep(int index)
        {
            if (_seq == null) return;
            var step = _seq.steps[index];

            if (!HasAnimation(step.animation))
            {
                AdvanceStep();
                return;
            }

            bool loop = step.loopCount != 1; // 0 = vô hạn, >1 = lặp n lần (đếm bằng Complete)
            var e = Target.State.SetAnimation(_seq.track, step.animation, loop);
            e.MixDuration = step.mixDuration >= 0f ? step.mixDuration : defaultMix;
            e.TimeScale = step.timeScale;

            _entry = e;
            _loopsDone = 0;
            e.Complete += OnEntryComplete;

            SequenceStepStarted?.Invoke(_seq, index);
        }

        private void OnEntryComplete(TrackEntry e)
        {
            if (_seq == null || e != _entry) return;

            _loopsDone++;
            var step = _seq.steps[_stepIndex];
            if (step.loopCount == 0) return;               // lặp vô hạn
            if (_loopsDone < step.loopCount) return;       // chưa đủ số lần
            AdvanceStep();
        }

        private void AdvanceStep()
        {
            if (_seq == null) return;
            int next = _stepIndex + 1;
            if (next >= _seq.steps.Count)
            {
                if (_seq.loop) next = 0;
                else
                {
                    var finished = _seq;
                    _seq = null;
                    _entry = null;
                    SequenceCompleted?.Invoke(finished);
                    return;
                }
            }
            _stepIndex = next;
            StartStep(next);
        }

        #endregion
    }
}

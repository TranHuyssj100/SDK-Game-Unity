using System;
using System.Collections.Generic;
using Spine.Unity;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LSDK.SpineAnim.Editor
{
    [CustomEditor(typeof(SpineAnimController))]
    public class SpineAnimControllerEditor : UnityEditor.Editor
    {
        private const int TabSetup = 0;
        private const int TabSeq = 1;

        private static readonly string[] Tabs = { "Setup", "Sequences" };

        private int _tab;
        private string _skinFilter = "";
        private string _animFilter = "";
        private Vector2 _skinScroll, _animScroll, _orderScroll;

        private int _track;
        private bool _loop = true;
        private float _mix = -1f;
        private float _alpha = 1f;
        private float _speed = 1f;

        private bool _previewing;
        private double _last;
        private string _playing = "";

        private readonly Dictionary<string, ReorderableList> _stepLists = new Dictionary<string, ReorderableList>();

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            _last = EditorApplication.timeSinceStartup;
            _tab = SessionState.GetInt(TabKey(), 0);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SessionState.SetInt(TabKey(), _tab);
            if (_previewing && !Application.isPlaying && target != null)
                ((SpineAnimController)target).ResetPose();
            _previewing = false;
            _stepLists.Clear();
        }

        private string TabKey()
        {
#if UNITY_6000_3_OR_NEWER
            return "LSDK.SpineAnim.Tab2." + target.GetEntityId();
#else
            return "LSDK.SpineAnim.Tab2." + target.GetInstanceID();
#endif
        }

        private void OnEditorUpdate()
        {
            if (Application.isPlaying || !_previewing || target == null) return;
            double now = EditorApplication.timeSinceStartup;
            float dt = Mathf.Clamp((float)(now - _last), 0f, 0.1f);
            _last = now;
            ((SpineAnimController)target).EditorTick(dt);
            SceneView.RepaintAll();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            var c = (SpineAnimController)target;
            serializedObject.Update();

            DrawHeader(c);

            var spine = c.Target;
            if (spine == null || !spine.IsAlive)
            {
                EditorGUILayout.HelpBox("Thêm SkeletonAnimation hoặc SkeletonGraphic vào cùng GameObject.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (c.GetData() == null)
            {
                EditorGUILayout.HelpBox("SkeletonDataAsset chưa gán, hoặc file skeleton không load được.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (spine.State == null)
            {
                EditorGUILayout.HelpBox(
                    "Spine 4.3 cần SkeletonAnimation trên object này để phát animation. SkeletonGraphic chỉ đảm nhiệm phần vẽ.",
                    MessageType.Warning);
            }

            _tab = Mathf.Clamp(GUILayout.Toolbar(_tab, Tabs, GUILayout.Height(24f)), 0, Tabs.Length - 1);
            EditorGUILayout.Space(6f);

            switch (_tab)
            {
                case TabSeq: DrawSequences(c); break;
                case TabSetup: DrawSetupPage(c); break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        // ───────────────────────── Header

        private void DrawHeader(SpineAnimController c)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spine Anim", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (_previewing && !Application.isPlaying)
                EditorGUILayout.LabelField("Preview", SpineEditorSkin.Preview, GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            bool ui = c.GetComponent<SkeletonGraphic>() != null;
            var asset = c.Target != null ? c.Target.DataAsset : null;
            string assetName = asset != null ? asset.name : "chưa có data";
            string line = (ui ? "UI" : "World") + "   ·   " + assetName;
            if (!string.IsNullOrEmpty(_playing)) line += "   ·   " + _playing;
            EditorGUILayout.LabelField(line, EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        // ───────────────────────── Skins

        private void DrawSkins(SpineAnimController c)
        {
            var skins = c.GetSkinNames();
            int selected = 0;
            for (int i = 0; i < skins.Length; i++)
                if (c.IsSkinSelected(skins[i])) selected++;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("includeDefaultSkin"),
                new GUIContent("Gồm skin default", "Skin default luôn nằm dưới cùng, các skin chọn sau đè lên."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                c.ApplySkins();
                RefreshEditMode(c);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All", EditorStyles.miniButtonLeft)) ChangeSkins(c, () => c.SelectAllSkins());
            if (GUILayout.Button("None", EditorStyles.miniButtonMid)) ChangeSkins(c, () => c.ClearSkins());
            if (GUILayout.Button("Apply", EditorStyles.miniButtonRight)) ChangeSkins(c, () => c.ApplySkins());
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"{selected}/{skins.Length}", EditorStyles.miniLabel, GUILayout.Width(48f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2f);
            _skinFilter = SpineEditorSkin.Search(_skinFilter);

            if (skins.Length == 0)
            {
                EditorGUILayout.LabelField("Skeleton không có skin phụ.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                DrawSkinList(c, skins);
            }

            DrawSkinOrder(c);
        }

        private void DrawSkinList(SpineAnimController c, string[] skins)
        {
            var groups = new List<string>();
            var byGroup = new Dictionary<string, List<string>>();
            int shown = 0;
            for (int i = 0; i < skins.Length; i++)
            {
                string name = skins[i];
                if (!PassFilter(name, _skinFilter)) continue;
                shown++;
                int slash = name.IndexOf('/');
                string group = slash > 0 ? name.Substring(0, slash) : "";
                if (!byGroup.TryGetValue(group, out var list))
                {
                    list = new List<string>();
                    byGroup[group] = list;
                    groups.Add(group);
                }
                list.Add(name);
            }

            if (shown == 0)
            {
                EditorGUILayout.LabelField("Không khớp \"" + _skinFilter + "\".", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            bool scroll = shown > 12;
            if (scroll) _skinScroll = EditorGUILayout.BeginScrollView(_skinScroll, GUILayout.MaxHeight(280f));

            bool titled = byGroup.Count > 1 || (byGroup.Count == 1 && groups[0].Length > 0);
            for (int g = 0; g < groups.Count; g++)
            {
                string group = groups[g];
                if (titled)
                {
                    if (g > 0) EditorGUILayout.Space(2f);
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(group) ? "Other" : group, EditorStyles.miniBoldLabel);
                }

                var list = byGroup[group];
                for (int i = 0; i < list.Count; i++)
                {
                    string name = list[i];
                    string leaf = string.IsNullOrEmpty(group) || name.Length <= group.Length + 1
                        ? name
                        : name.Substring(group.Length + 1);
                    DrawSkinToggle(c, name, leaf);
                }
            }

            if (scroll) EditorGUILayout.EndScrollView();
        }

        private void DrawSkinToggle(SpineAnimController c, string name, string label)
        {
            bool cur = c.IsSkinSelected(name);
            Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            if (cur) EditorGUI.DrawRect(row, SpineEditorSkin.RowOn);
            bool next = EditorGUI.ToggleLeft(row, new GUIContent(label, name), cur);
            if (next != cur) ChangeSkins(c, () => c.SetSkinSelected(name, next));
        }

        private void DrawSkinOrder(SpineAnimController c)
        {
            var selected = c.SelectedSkins;
            if (!c.IncludeDefaultSkin && selected.Count == 0) return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Thứ tự đè  ·  dòng dưới đè dòng trên", EditorStyles.miniBoldLabel);

            if (c.IncludeDefaultSkin)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField("default", EditorStyles.miniLabel);
            }

            if (selected.Count == 0) return;

            bool scroll = selected.Count > 6;
            if (scroll) _orderScroll = EditorGUILayout.BeginScrollView(_orderScroll, GUILayout.MaxHeight(140f));

            for (int i = 0; i < selected.Count; i++)
            {
                int delta = 0;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(selected[i], EditorStyles.miniLabel);
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(22f))) delta = -1;
                }
                using (new EditorGUI.DisabledScope(i == selected.Count - 1))
                {
                    if (GUILayout.Button("↓", EditorStyles.miniButtonRight, GUILayout.Width(22f))) delta = 1;
                }
                EditorGUILayout.EndHorizontal();

                if (delta != 0)
                {
                    MoveSkin(c, i, delta);
                    break;
                }
            }

            if (scroll) EditorGUILayout.EndScrollView();
        }

        private void MoveSkin(SpineAnimController c, int index, int delta)
        {
            var list = new List<string>(c.SelectedSkins);
            int j = index + delta;
            if (j < 0 || j >= list.Count) return;
            string tmp = list[index];
            list[index] = list[j];
            list[j] = tmp;
            ChangeSkins(c, () => c.SetSkins(list));
        }

        private void ChangeSkins(SpineAnimController c, Action action)
        {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(c, "Change Spine Skins");
            action();
            EditorUtility.SetDirty(c);
            serializedObject.Update();
            RefreshEditMode(c);
        }

        // ───────────────────────── Animations

        private void DrawAnimations(SpineAnimController c)
        {
            DrawPlayback(c);

            var anims = c.GetAnimationNames();
            EditorGUILayout.Space(4f);
            _animFilter = SpineEditorSkin.Search(_animFilter);

            int shown = 0;
            for (int i = 0; i < anims.Length; i++)
                if (PassFilter(anims[i], _animFilter)) shown++;

            if (anims.Length == 0)
            {
                EditorGUILayout.LabelField("Không có animation.", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            if (shown == 0)
            {
                EditorGUILayout.LabelField("Không khớp \"" + _animFilter + "\".", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.LabelField($"{shown} animation", EditorStyles.miniLabel);

            bool scroll = shown > 10;
            if (scroll) _animScroll = EditorGUILayout.BeginScrollView(_animScroll, GUILayout.MaxHeight(320f));

            int row = 0;
            for (int i = 0; i < anims.Length; i++)
            {
                string name = anims[i];
                if (!PassFilter(name, _animFilter)) continue;
                DrawAnimRow(c, name, row++);
            }

            if (scroll) EditorGUILayout.EndScrollView();
        }

        private void DrawPlayback(SpineAnimController c)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Playback", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("Nhiều track chồng lên nhau. Alpha là độ đậm của track đang phát.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            _track = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Track"), _track));
            _loop = EditorGUILayout.ToggleLeft(new GUIContent("Loop"), _loop, GUILayout.Width(52f));
            EditorGUILayout.EndHorizontal();

            _mix = Mathf.Max(-1f, EditorGUILayout.FloatField(new GUIContent("Mix", "-1 = Default Mix"), _mix));
            _speed = EditorGUILayout.FloatField(new GUIContent("Speed", "Time scale"), _speed);
            _alpha = EditorGUILayout.Slider(new GUIContent("Alpha"), _alpha, 0f, 1f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop track"))
            {
                c.Stop(_track, _mix >= 0f ? _mix : 0f);
                _playing = "";
                StartPreview();
            }
            if (GUILayout.Button("Reset pose"))
            {
                c.ResetPose();
                _previewing = false;
                _playing = "";
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawAnimRow(SpineAnimController c, string name, int index)
        {
            float h = EditorGUIUtility.singleLineHeight + 2f;
            Rect row = EditorGUILayout.GetControlRect(false, h);
            bool on = _playing == name;
            if (on) EditorGUI.DrawRect(row, SpineEditorSkin.RowOn);
            else if (index % 2 == 0) EditorGUI.DrawRect(row, SpineEditorSkin.Zebra);

            var play = new Rect(row.x + 2f, row.y + 1f, 22f, row.height - 2f);
            var dur = new Rect(row.xMax - 52f, row.y, 50f, row.height);
            var label = new Rect(play.xMax + 4f, row.y, Mathf.Max(0f, dur.x - play.xMax - 8f), row.height);

            if (GUI.Button(play, "▶", EditorStyles.miniButton)) PlayAnim(c, name);

            var evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 0 && label.Contains(evt.mousePosition))
            {
                PlayAnim(c, name);
                evt.Use();
            }

            EditorGUI.LabelField(label, new GUIContent(name, name));
            EditorGUI.LabelField(dur, $"{c.GetAnimationDuration(name):0.00}s", SpineEditorSkin.RightMini);
        }

        private void PlayAnim(SpineAnimController c, string name)
        {
            StartPreview();
            c.Play(name, _loop, _track, _mix, _alpha, _speed);
            _playing = name;
        }

        // ───────────────────────── Sequences

        private void DrawSequences(SpineAnimController c)
        {
            var seqProp = serializedObject.FindProperty("sequences");

            EditorGUILayout.LabelField(
                "Loop 0 = lặp vô hạn và dừng tại bước đó. Mix -1 = Default Mix. Kéo bước để đổi thứ tự.",
                EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("+ Sequence", GUILayout.Height(22f)))
                AddSequence(seqProp);

            if (seqProp.arraySize == 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox("Chưa có sequence. Bấm + Sequence để tạo chuỗi animation.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4f);
            for (int i = 0; i < seqProp.arraySize; i++)
            {
                if (!DrawSequenceCard(c, seqProp, i)) break;
                EditorGUILayout.Space(4f);
            }
        }

        private void AddSequence(SerializedProperty seqProp)
        {
            int index = seqProp.arraySize;
            seqProp.arraySize++;
            var el = seqProp.GetArrayElementAtIndex(index);
            el.FindPropertyRelative("name").stringValue = "Sequence " + (index + 1);
            el.FindPropertyRelative("track").intValue = 0;
            el.FindPropertyRelative("loop").boolValue = false;
            el.FindPropertyRelative("steps").ClearArray();
            el.isExpanded = true;
        }

        /// <returns>false khi list vừa bị xoá hoặc đổi chỗ — dừng vẽ các card phía sau.</returns>
        private bool DrawSequenceCard(SpineAnimController c, SerializedProperty seqProp, int index)
        {
            var el = seqProp.GetArrayElementAtIndex(index);
            var nameProp = el.FindPropertyRelative("name");
            var steps = el.FindPropertyRelative("steps");
            bool playing = c.IsSequencePlaying && c.CurrentSequence != null && c.CurrentSequence.name == nameProp.stringValue;

            Color bg = GUI.backgroundColor;
            if (playing) GUI.backgroundColor = SpineEditorSkin.CardOn;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = bg;

            int action = 0;
            EditorGUILayout.BeginHorizontal();
            el.isExpanded = GUILayout.Toggle(el.isExpanded, GUIContent.none, EditorStyles.foldout, GUILayout.Width(16f));
            EditorGUILayout.PropertyField(nameProp, GUIContent.none);
            EditorGUILayout.LabelField(steps.arraySize.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(22f));

            using (new EditorGUI.DisabledScope(steps.arraySize == 0))
            {
                if (GUILayout.Button(new GUIContent("▶", "Phát sequence này"), EditorStyles.miniButtonLeft, GUILayout.Width(24f)))
                    action = 1;
            }
            if (GUILayout.Button(new GUIContent("■", "Dừng sequence, animation hiện tại vẫn chạy"), EditorStyles.miniButtonMid, GUILayout.Width(24f)))
                action = 2;
            using (new EditorGUI.DisabledScope(index == 0))
            {
                if (GUILayout.Button("↑", EditorStyles.miniButtonMid, GUILayout.Width(22f))) action = 3;
            }
            using (new EditorGUI.DisabledScope(index == seqProp.arraySize - 1))
            {
                if (GUILayout.Button("↓", EditorStyles.miniButtonMid, GUILayout.Width(22f))) action = 4;
            }
            if (GUILayout.Button(new GUIContent("×", "Xoá sequence"), EditorStyles.miniButtonRight, GUILayout.Width(22f)))
                action = 5;
            EditorGUILayout.EndHorizontal();

            if (el.isExpanded)
            {
                EditorGUILayout.PropertyField(el.FindPropertyRelative("track"), new GUIContent("Track"));
                EditorGUILayout.PropertyField(el.FindPropertyRelative("loop"), new GUIContent("Lặp chuỗi", "Hết bước cuối thì quay lại bước đầu."));

                GetStepList(steps).DoLayoutList();

                if (GUILayout.Button(new GUIContent("Thêm tất cả animation", "Nối mọi animation của skeleton vào cuối sequence.")))
                {
                    bool ok = steps.arraySize == 0 || EditorUtility.DisplayDialog(
                        "Thêm animation",
                        "Sequence đang có " + steps.arraySize + " bước. Thêm toàn bộ animation vào cuối?",
                        "Thêm", "Huỷ");
                    if (ok) FillAllAnimations(c, steps);
                }
            }

            EditorGUILayout.EndVertical();

            switch (action)
            {
                case 1:
                    serializedObject.ApplyModifiedProperties();
                    StartPreview();
                    c.PlaySequence(index);
                    _playing = nameProp.stringValue;
                    serializedObject.Update();
                    return true;
                case 2:
                    c.StopSequence();
                    return true;
                case 3:
                    seqProp.MoveArrayElement(index, index - 1);
                    _stepLists.Clear();
                    return false;
                case 4:
                    seqProp.MoveArrayElement(index, index + 1);
                    _stepLists.Clear();
                    return false;
                case 5:
                    _stepLists.Clear();
                    seqProp.DeleteArrayElementAtIndex(index);
                    return false;
                default:
                    return true;
            }
        }

        private ReorderableList GetStepList(SerializedProperty steps)
        {
            string key = steps.propertyPath;
            if (!_stepLists.TryGetValue(key, out var list))
            {
                list = new ReorderableList(steps.serializedObject, steps, true, true, true, true);
                list.elementHeight = SpineSequenceStepDrawer.Height + 2f;
                list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Steps");
                list.drawElementCallback = (rect, index, active, focused) =>
                {
                    if (index < 0 || index >= steps.arraySize) return;
                    rect.y += 1f;
                    rect.height = SpineSequenceStepDrawer.Height;
                    SpineSequenceStepDrawer.Draw(rect, steps.GetArrayElementAtIndex(index));
                };
                list.onAddCallback = l =>
                {
                    int i = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;
                    if (i != 0) return;
                    var e = l.serializedProperty.GetArrayElementAtIndex(i);
                    e.FindPropertyRelative("animation").stringValue = "";
                    e.FindPropertyRelative("loopCount").intValue = 1;
                    e.FindPropertyRelative("mixDuration").floatValue = -1f;
                    e.FindPropertyRelative("timeScale").floatValue = 1f;
                };
                _stepLists[key] = list;
            }
            return list;
        }

        private static void FillAllAnimations(SpineAnimController c, SerializedProperty steps)
        {
            var anims = c.GetAnimationNames();
            for (int a = 0; a < anims.Length; a++)
            {
                int i = steps.arraySize;
                steps.arraySize++;
                var e = steps.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("animation").stringValue = anims[a];
                e.FindPropertyRelative("loopCount").intValue = 1;
                e.FindPropertyRelative("mixDuration").floatValue = -1f;
                e.FindPropertyRelative("timeScale").floatValue = 1f;
            }
        }

        // ───────────────────────── Setup

        private void DrawSetupPage(SpineAnimController c)
        {
            DrawSetup();
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Skins", EditorStyles.boldLabel);
            DrawSkins(c);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
            DrawAnimations(c);
        }

        private void DrawSetup()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Khi vào Play", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("autoApplyOnStart"),
                new GUIContent("Áp skin lúc Start", "Gộp các skin đang chọn ngay khi object Start."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("defaultMix"),
                new GUIContent("Default Mix", "Thời gian trộn mặc định giữa hai animation. Bước sequence dùng -1 sẽ lấy giá trị này."));

            var modeProp = serializedObject.FindProperty("autoPlayMode");
            EditorGUILayout.PropertyField(modeProp, new GUIContent("Auto Play", "Tự phát khi Start."));
            var mode = (SpineAutoPlayMode)modeProp.enumValueIndex;
            var c = (SpineAnimController)target;

            if (mode == SpineAutoPlayMode.Animation)
            {
                SpineSequenceStepDrawer.DrawAnimationField(
                    EditorGUILayout.GetControlRect(),
                    new GUIContent("Animation"),
                    serializedObject.FindProperty("autoPlayAnimation"),
                    c.GetAnimationNames());
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoPlayLoop"), new GUIContent("Loop"));
            }
            else if (mode == SpineAutoPlayMode.Sequence)
            {
                var sp = serializedObject.FindProperty("autoPlaySequence");
                var names = new string[c.Sequences.Count];
                for (int i = 0; i < names.Length; i++) names[i] = c.Sequences[i].name;
                sp.stringValue = NamePopup("Sequence", sp.stringValue, names);
            }

            EditorGUILayout.EndVertical();
        }

        // ───────────────────────── Helpers

        private void StartPreview()
        {
            if (Application.isPlaying) return;
            _previewing = true;
            _last = EditorApplication.timeSinceStartup;
        }

        private static void RefreshEditMode(SpineAnimController c)
        {
            if (Application.isPlaying) return;
            c.RefreshImmediate();
            SceneView.RepaintAll();
        }

        private static bool PassFilter(string name, string filter)
        {
            return string.IsNullOrEmpty(filter)
                || name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NamePopup(string label, string current, string[] names)
        {
            if (names.Length == 0)
            {
                EditorGUILayout.LabelField(label, "(trống)");
                return current;
            }

            int idx = Array.IndexOf(names, current);
            if (idx < 0)
            {
                var opts = new string[names.Length + 1];
                opts[0] = string.IsNullOrEmpty(current) ? "None" : "Missing: " + current;
                Array.Copy(names, 0, opts, 1, names.Length);
                int n = EditorGUILayout.Popup(label, 0, opts);
                return n > 0 ? opts[n] : current;
            }

            int sel = EditorGUILayout.Popup(label, idx, names);
            return names[sel];
        }
    }

    static class SpineEditorSkin
    {
        private static GUIStyle _preview;
        private static GUIStyle _rightMini;

        public static GUIStyle Preview
        {
            get
            {
                if (_preview == null)
                {
                    _preview = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleRight };
                    _preview.normal.textColor = new Color(1f, 0.68f, 0.32f);
                }
                return _preview;
            }
        }

        public static GUIStyle RightMini
        {
            get
            {
                if (_rightMini == null)
                    _rightMini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                return _rightMini;
            }
        }

        public static Color RowOn => EditorGUIUtility.isProSkin
            ? new Color(0.98f, 0.55f, 0.28f, 0.28f)
            : new Color(0.93f, 0.45f, 0.16f, 0.22f);

        public static Color Zebra => EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.035f)
            : new Color(0f, 0f, 0f, 0.035f);

        public static Color CardOn => EditorGUIUtility.isProSkin
            ? new Color(1f, 0.82f, 0.55f)
            : new Color(1f, 0.9f, 0.75f);

        public static string Search(string value, string controlName = null)
        {
            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(controlName))
                GUI.SetNextControlName(controlName);
            value = EditorGUILayout.TextField(value ?? "", EditorStyles.toolbarSearchField);
            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20f)))
            {
                value = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            return value;
        }
    }
}

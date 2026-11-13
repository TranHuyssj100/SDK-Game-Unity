using System;
using UnityEditor;
using UnityEngine;

namespace LSDK.SpineAnim.Editor
{
    [CustomPropertyDrawer(typeof(SpineSequenceStep))]
    public class SpineSequenceStepDrawer : PropertyDrawer
    {
        public static float Height => EditorGUIUtility.singleLineHeight * 2f + 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => Height;

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            Draw(pos, prop);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect pos, SerializedProperty prop)
        {
            var anim = prop.FindPropertyRelative("animation");
            var loop = prop.FindPropertyRelative("loopCount");
            var mix = prop.FindPropertyRelative("mixDuration");
            var speed = prop.FindPropertyRelative("timeScale");

            float line = EditorGUIUtility.singleLineHeight;
            var row1 = new Rect(pos.x, pos.y, pos.width, line);
            var row2 = new Rect(pos.x, pos.y + line + 2f, pos.width, line);

            var controller = prop.serializedObject.targetObject as SpineAnimController;
            string[] names = controller != null ? controller.GetAnimationNames() : Array.Empty<string>();
            DrawAnimationField(row1, GUIContent.none, anim, names);

            float gap = 6f;
            float col = (row2.width - gap * 2f) / 3f;
            var rLoop = new Rect(row2.x, row2.y, col, line);
            var rMix = new Rect(row2.x + col + gap, row2.y, col, line);
            var rSpeed = new Rect(row2.x + (col + gap) * 2f, row2.y, col, line);

            int indent = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            EditorGUIUtility.labelWidth = 34f;
            EditorGUI.PropertyField(rLoop, loop, new GUIContent("Loop", "1 = một lần, n = lặp n lần, 0 = vô hạn (sequence dừng tại bước này)."));
            if (loop.intValue < 0) loop.intValue = 0;

            EditorGUIUtility.labelWidth = 28f;
            EditorGUI.PropertyField(rMix, mix, new GUIContent("Mix", "Giây trộn từ animation trước. -1 = Default Mix."));
            if (mix.floatValue < -1f) mix.floatValue = -1f;

            EditorGUIUtility.labelWidth = 42f;
            EditorGUI.PropertyField(rSpeed, speed, new GUIContent("Speed", "Time scale. Âm = chạy ngược."));

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indent;
        }

        public static void DrawAnimationField(Rect row, GUIContent label, SerializedProperty prop, string[] names)
        {
            Rect field = label == null || label == GUIContent.none
                ? row
                : EditorGUI.PrefixLabel(row, label);

            string current = prop.stringValue;
            bool missing = !string.IsNullOrEmpty(current) && Array.IndexOf(names, current) < 0;
            string shown = string.IsNullOrEmpty(current) ? "None" : (missing ? "Missing: " + current : current);

            if (!GUI.Button(field, new GUIContent(shown, current), EditorStyles.popup)) return;

            var so = prop.serializedObject;
            string path = prop.propertyPath;
            PopupWindow.Show(field, new AnimSearchPopup(names, current, picked =>
            {
                so.Update();
                var p = so.FindProperty(path);
                if (p == null) return;
                p.stringValue = picked ?? "";
                so.ApplyModifiedProperties();
            }));
        }
    }

    sealed class AnimSearchPopup : PopupWindowContent
    {
        private readonly string[] _names;
        private readonly string _current;
        private readonly Action<string> _onPick;
        private string _filter = "";
        private Vector2 _scroll;
        private bool _focus = true;

        public AnimSearchPopup(string[] names, string current, Action<string> onPick)
        {
            _names = names ?? Array.Empty<string>();
            _current = current ?? "";
            _onPick = onPick;
        }

        public override Vector2 GetWindowSize() => new Vector2(300f, 340f);

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginArea(new Rect(6f, 6f, rect.width - 12f, rect.height - 12f));
            _filter = SpineEditorSkin.Search(_filter, "lsdkSpineAnimSearch");
            if (_focus)
            {
                EditorGUI.FocusTextInControl("lsdkSpineAnimSearch");
                _focus = false;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            bool noneShown = Pass("None");
            if (noneShown)
                DrawRow("", string.IsNullOrEmpty(_current) ? "●  None" : "None");

            int shown = 0;
            string first = null;
            for (int i = 0; i < _names.Length; i++)
            {
                string name = _names[i];
                if (!Pass(name)) continue;
                if (first == null) first = name;
                shown++;
                string label = name == _current ? "●  " + name : name;
                DrawRow(name, label);
            }

            if (shown == 0 && !noneShown)
                EditorGUILayout.LabelField("Không khớp.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && first != null)
            {
                Pick(first);
                Event.current.Use();
            }
        }

        private void DrawRow(string value, string label)
        {
            if (!GUILayout.Button(new GUIContent(label, value), EditorStyles.toolbarButton)) return;
            Pick(value);
        }

        private void Pick(string value)
        {
            _onPick?.Invoke(value);
            editorWindow.Close();
        }

        private bool Pass(string name)
        {
            return string.IsNullOrEmpty(_filter)
                || name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

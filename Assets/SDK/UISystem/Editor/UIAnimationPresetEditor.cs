using UnityEditor;
using UnityEngine;

namespace LSDK.UISystem.EditorTools
{
    [CustomEditor(typeof(UIAnimationPreset))]
    public class UIAnimationPresetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var preset = (UIAnimationPreset)target;
            EditorGUILayout.HelpBox(
                "Update thêm type mới và giữ số đã chỉnh. Refresh đưa mọi type về mặc định.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Update"))
                {
                    Undo.RecordObject(preset, "Update UI Motions");
                    preset.UpdateMotions();
                    EditorUtility.SetDirty(preset);
                }
                if (GUILayout.Button("Refresh"))
                {
                    Undo.RecordObject(preset, "Refresh UI Motions");
                    preset.ResetMotions();
                    EditorUtility.SetDirty(preset);
                }
            }

            serializedObject.Update();

            var motions = serializedObject.FindProperty("motions");
            for (int i = 0; i < motions.arraySize; i++)
            {
                var entry = motions.GetArrayElementAtIndex(i);
                var motion = entry.FindPropertyRelative("motion");
                string title = motion.enumDisplayNames[motion.enumValueIndex];

                EditorGUILayout.Space(4);
                entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, title, true, EditorStyles.foldoutHeader);
                if (!entry.isExpanded) continue;

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("show"), true);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("hide"), true);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(UIAnimator))]
    public class UIAnimatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            ((UIAnimator)target).RefreshConfigs();
            serializedObject.Update();
            DrawDefaultInspector();
        }
    }
}

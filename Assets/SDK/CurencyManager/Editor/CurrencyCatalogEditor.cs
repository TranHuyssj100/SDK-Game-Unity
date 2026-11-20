// Assets/SDK/CurencyManager/Editor/CurrencyCatalogEditor.cs
using UnityEditor;
using UnityEngine;

namespace LSDK.Currency.Editor
{
    /// <summary>Inspector của catalog: sửa danh sách và sinh lại enum.</summary>
    [CustomEditor(typeof(CurrencyCatalog))]
    public sealed class CurrencyCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_definitions"), true);
            serializedObject.ApplyModifiedProperties();

            var list = serializedObject.FindProperty("_definitions");
            Warn(list);

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate CurrencyType"))
            {
                CurrencyCatalog.ClearCache();
                CurrencyEnumGenerator.Generate((CurrencyCatalog)target);
            }
        }

        static void Warn(SerializedProperty list)
        {
            if (list == null || !list.isArray) return;
            for (int i = 0; i < list.arraySize; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                int id = entry.FindPropertyRelative("_id").intValue;
                bool regen = entry.FindPropertyRelative("_useRegen").boolValue;
                long max = entry.FindPropertyRelative("_maxAmount").longValue;
                int interval = entry.FindPropertyRelative("_regenIntervalSeconds").intValue;
                string name = entry.FindPropertyRelative("_name").stringValue;

                for (int j = 0; j < i; j++)
                {
                    int other = list.GetArrayElementAtIndex(j).FindPropertyRelative("_id").intValue;
                    if (other == id)
                        EditorGUILayout.HelpBox("Trùng id " + id + ".", MessageType.Error);
                }

                if (string.IsNullOrWhiteSpace(name))
                    EditorGUILayout.HelpBox("Dòng " + i + " chưa có tên enum.", MessageType.Warning);
                if (regen && (max < 1 || interval < 1))
                    EditorGUILayout.HelpBox(name + " bật hồi nhưng trần hoặc chu kỳ nhỏ hơn 1. Hồi sẽ tắt.", MessageType.Warning);
            }
        }
    }
}

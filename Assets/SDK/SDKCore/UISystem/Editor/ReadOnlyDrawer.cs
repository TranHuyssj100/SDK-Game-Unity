using UnityEditor;
using UnityEngine;

namespace LSDK.UISystem.EditorTools
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;

            var child = property.Copy();
            var end = child.GetEndProperty();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, false);
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var header = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.BeginDisabledGroup(true);
                float y = header.yMax + EditorGUIUtility.standardVerticalSpacing;
                var child = property.Copy();
                var end = child.GetEndProperty();
                bool enter = true;
                EditorGUI.indentLevel++;
                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;
                    float h = EditorGUI.GetPropertyHeight(child, false);
                    var row = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, h));
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    EditorGUI.PropertyField(row, child, false);
                    EditorGUI.indentLevel = indent;
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }

            EditorGUI.EndProperty();
        }
    }
}

// Assets/SDK/CurencyManager/Editor/CurrencyDefinitionDrawer.cs
using UnityEditor;
using UnityEngine;

namespace LSDK.Currency.Editor
{
    /// <summary>
    /// Vẽ <see cref="CurrencyDefinition"/> trong inspector. Chỉ hiện trường liên quan
    /// theo <c>Use Regen</c>: bật thì hiện trần, chu kỳ, unlimited; tắt thì hiện số dư ban đầu.
    /// </summary>
    [CustomPropertyDrawer(typeof(CurrencyDefinition))]
    public sealed class CurrencyDefinitionDrawer : PropertyDrawer
    {
        const string Id = "_id";
        const string Name = "_name";
        const string Icon = "_icon";
        const string UseRegen = "_useRegen";
        const string InitialAmount = "_initialAmount";
        const string MaxAmount = "_maxAmount";
        const string RegenInterval = "_regenIntervalSeconds";
        const string AllowUnlimited = "_allowUnlimited";

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded) return line;

            int rows = 4; // id, name, icon, useRegen
            var useRegen = property.FindPropertyRelative(UseRegen);
            rows += useRegen != null && useRegen.boolValue ? 3 : 1;
            return line + rows * (line + space);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float space = EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.BeginProperty(position, label, property);

            var rect = new Rect(position.x, position.y, position.width, line);
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                Draw(ref rect, property, Id, line, space);
                Draw(ref rect, property, Name, line, space);
                Draw(ref rect, property, Icon, line, space);
                Draw(ref rect, property, UseRegen, line, space);

                var useRegen = property.FindPropertyRelative(UseRegen);
                if (useRegen != null && useRegen.boolValue)
                {
                    Draw(ref rect, property, MaxAmount, line, space);
                    Draw(ref rect, property, RegenInterval, line, space);
                    Draw(ref rect, property, AllowUnlimited, line, space);
                }
                else
                {
                    Draw(ref rect, property, InitialAmount, line, space);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        static void Draw(ref Rect rect, SerializedProperty parent, string field, float line, float space)
        {
            var child = parent.FindPropertyRelative(field);
            rect.y += line + space;
            rect.height = line;
            if (child != null) EditorGUI.PropertyField(rect, child, true);
        }
    }
}

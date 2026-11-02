using UnityEditor;
using UnityEngine;

namespace LSDK.Mediation.EditorTools
{
    [CustomEditor(typeof(MediationConfig))]
    [CanEditMultipleObjects]
    public sealed class MediationConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("Define Symbols", GUILayout.Height(28)))
                MediationConfigMenu.DefineSymbols();
        }
    }
}

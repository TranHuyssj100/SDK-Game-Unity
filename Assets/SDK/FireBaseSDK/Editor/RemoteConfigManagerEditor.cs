// Assets/SDK/FireBaseSDK/Editor/RemoteConfigManagerEditor.cs
using UnityEditor;
using UnityEngine;

namespace LSDK.Firebase.Editor
{
    /// <summary>Inspector của RemoteConfigManager. Danh sách module nằm ở Remote Config Window.</summary>
    [CustomEditor(typeof(RemoteConfigManager))]
    public sealed class RemoteConfigManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(4f);

            if (GUILayout.Button("Open Remote Config Window"))
                RemoteConfigWindow.Open();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Mở Remote Config Window để xem các RemoteConfigModule đã khai báo và giá trị của chúng.",
                    MessageType.Info);
            }
        }
    }
}

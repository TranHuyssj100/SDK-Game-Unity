using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LSDK.GameData.EditorTools
{
    /// <summary>Inspector cho GameDataManager: xem JSON live của từng module + nút Save/Reload/Reset.</summary>
    [CustomEditor(typeof(GameDataManager))]
    public class GameDataManagerEditor : UnityEditor.Editor
    {
        private readonly Dictionary<Type, bool> _foldouts = new Dictionary<Type, bool>();

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("Open GameData Window"))
                GameDataWindow.Open();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Vào Play mode để xem data live. Sửa data đã lưu: dùng GameData Window.", MessageType.Info);
                return;
            }

            var manager = (GameDataManager)target;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save All")) manager.SaveAll(true);
                if (GUILayout.Button("Reload All")) manager.ReloadAll();
                if (GUILayout.Button("Reset All") &&
                    EditorUtility.DisplayDialog("Reset All", "Reset toàn bộ module về mặc định?", "Reset", "Hủy"))
                {
                    manager.ResetAll();
                }
            }

            EditorGUILayout.Space();
            foreach (var module in manager.AllModules)
            {
                var type = module.GetType();
                _foldouts.TryGetValue(type, out bool open);

                string label = type.Name + (module.IsDirty ? " *" : string.Empty);
                open = EditorGUILayout.Foldout(open, label, true);
                _foldouts[type] = open;

                if (!open) continue;

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextArea(manager.ToJson(module));
            }
        }
    }
}

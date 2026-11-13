using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LSDK.UISystem.EditorTools
{
    [CustomEditor(typeof(UIRegistry))]
    public class UIRegistryEditor : Editor
    {
        SerializedProperty _entries, _nextId, _enumName, _ns, _folder, _auto;
        bool _entriesChanged;
        bool _upToDate = true;

        void OnEnable()
        {
            _entries = serializedObject.FindProperty("entries");
            _nextId = serializedObject.FindProperty("nextId");
            _enumName = serializedObject.FindProperty("enumName");
            _ns = serializedObject.FindProperty("enumNamespace");
            _folder = serializedObject.FindProperty("outputFolder");
            _auto = serializedObject.FindProperty("autoGenerate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _entriesChanged = false;

            DrawDropArea();
            DrawEntries();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Enum Generation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enumName);
            EditorGUILayout.PropertyField(_ns);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(_folder);
            EditorGUILayout.PropertyField(_auto, new GUIContent("Auto Generate"));

            serializedObject.ApplyModifiedProperties();

            var reg = (UIRegistry)target;
            if (_entriesChanged && reg.autoGenerate)
                EditorApplication.delayCall += () => { if (reg != null) UIEnumGenerator.Generate(reg); };

            if (Event.current.type == EventType.Layout)
                _upToDate = UIEnumGenerator.IsUpToDate(reg);

            if (!_upToDate)
                EditorGUILayout.HelpBox("Enum file is missing or out of date.", MessageType.Info);
            if (GUILayout.Button("Generate Enum"))
                UIEnumGenerator.Generate(reg);
        }

        // ---------- Drop area ----------
        void DrawDropArea()
        {
            var rect = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
            var style = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            GUI.Box(rect, "Drag & drop UI prefabs (or folders) here", style);

            var e = Event.current;
            if ((e.type != EventType.DragUpdated && e.type != EventType.DragPerform) || !rect.Contains(e.mousePosition))
                return;

            var panels = DragAndDrop.objectReferences.SelectMany(o => Collect(o)).Distinct().ToList();
            DragAndDrop.visualMode = panels.Count > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (panels.Count == 0) Debug.LogWarning("[UI] No prefab with a UIPanel component in the dropped objects.");
                foreach (var p in panels) AddPanel(p);
            }
            e.Use();
        }

        static IEnumerable<UIPanel> Collect(UnityEngine.Object o)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) yield break;   // object trong scene -> bỏ

            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (go != null && go.TryGetComponent(out UIPanel p)) yield return p;
                }
            }
            else if (o is GameObject g)
            {
                if (g.TryGetComponent(out UIPanel panel)) yield return panel;
            }
            else if (o is UIPanel up)
            {
                yield return up;
            }
        }

        void AddPanel(UIPanel panel)
        {
            for (int i = 0; i < _entries.arraySize; i++)
                if (_entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == panel)
                    return;   // đã có

            int idx = _entries.arraySize;
            _entries.arraySize++;
            var el = _entries.GetArrayElementAtIndex(idx);
            el.FindPropertyRelative("id").intValue = _nextId.intValue++;
            el.FindPropertyRelative("name").stringValue = UIEnumGenerator.Sanitize(panel.name);
            el.FindPropertyRelative("prefab").objectReferenceValue = panel;
            _entriesChanged = true;
        }

        // ---------- Entries ----------
        void DrawEntries()
        {
            EditorGUILayout.LabelField($"Entries ({_entries.arraySize})", EditorStyles.boldLabel);

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < _entries.arraySize; i++)
            {
                string n = UIEnumGenerator.Sanitize(_entries.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
                counts[n] = counts.TryGetValue(n, out var c) ? c + 1 : 1;
            }

            int remove = -1;
            for (int i = 0; i < _entries.arraySize; i++)
            {
                var el = _entries.GetArrayElementAtIndex(i);
                var pName = el.FindPropertyRelative("name");
                var pPrefab = el.FindPropertyRelative("prefab");
                var pId = el.FindPropertyRelative("id");

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUILayout.DelayedTextField(pName.stringValue, GUILayout.MinWidth(80));
                    if (EditorGUI.EndChangeCheck()) { pName.stringValue = newName; _entriesChanged = true; }

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(pPrefab, GUIContent.none);
                    if (EditorGUI.EndChangeCheck()) _entriesChanged = true;

                    GUILayout.Label($"#{pId.intValue}", GUILayout.Width(36));
                    if (GUILayout.Button("X", GUILayout.Width(22))) remove = i;
                }

                if (pPrefab.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Prefab missing - this entry is skipped in the enum.", MessageType.Error);
                else if (counts[UIEnumGenerator.Sanitize(pName.stringValue)] > 1)
                    EditorGUILayout.HelpBox("Duplicate name - a number suffix will be added in the enum.", MessageType.Warning);
            }

            if (remove >= 0)
            {
                _entries.DeleteArrayElementAtIndex(remove);
                _entriesChanged = true;
            }
        }
    }
}

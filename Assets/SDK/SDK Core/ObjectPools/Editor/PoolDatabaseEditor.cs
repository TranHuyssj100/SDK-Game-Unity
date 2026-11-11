using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LSDK.Core.ObjectPools.EditorTools
{
    [CustomEditor(typeof(PoolDatabaseSO))]
    public class PoolDatabaseEditor : Editor
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
            EnsureIds();
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
            EditorGUILayout.PropertyField(_folder);
            EditorGUILayout.PropertyField(_auto, new GUIContent("Auto Generate"));

            serializedObject.ApplyModifiedProperties();

            var db = (PoolDatabaseSO)target;
            if (_entriesChanged && db.autoGenerate)
                EditorApplication.delayCall += () => { if (db != null) PoolEnumGenerator.Generate(db); };

            if (Event.current.type == EventType.Layout)
                _upToDate = PoolEnumGenerator.IsUpToDate(db);

            if (!_upToDate)
                EditorGUILayout.HelpBox("Enum file is missing or out of date.", MessageType.Info);
            if (GUILayout.Button("Generate Enum"))
                PoolEnumGenerator.Generate(db);
        }

        void EnsureIds()
        {
            serializedObject.Update();
            bool changed = false;
            int next = Mathf.Max(1, _nextId.intValue);
            for (int i = 0; i < _entries.arraySize; i++)
            {
                var el = _entries.GetArrayElementAtIndex(i);
                var id = el.FindPropertyRelative("id");
                if (id.intValue <= 0)
                {
                    id.intValue = next++;
                    changed = true;
                }
                else if (id.intValue >= next)
                {
                    next = id.intValue + 1;
                }

                var name = el.FindPropertyRelative("name");
                if (string.IsNullOrWhiteSpace(name.stringValue))
                {
                    var prefab = el.FindPropertyRelative("prefab").objectReferenceValue;
                    if (prefab != null)
                    {
                        name.stringValue = PoolEnumGenerator.Sanitize(prefab.name);
                        changed = true;
                    }
                }
            }

            if (_nextId.intValue != next)
            {
                _nextId.intValue = next;
                changed = true;
            }

            if (!changed) return;
            serializedObject.ApplyModifiedProperties();
            var db = (PoolDatabaseSO)target;
            if (db.autoGenerate)
                EditorApplication.delayCall += () => { if (db != null) PoolEnumGenerator.Generate(db); };
        }

        void DrawDropArea()
        {
            var rect = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
            var style = new GUIStyle(EditorStyles.helpBox) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            GUI.Box(rect, "Drag & drop prefabs (or folders) here", style);

            var e = Event.current;
            if ((e.type != EventType.DragUpdated && e.type != EventType.DragPerform) || !rect.Contains(e.mousePosition))
                return;

            var prefabs = DragAndDrop.objectReferences.SelectMany(Collect).Distinct().ToList();
            DragAndDrop.visualMode = prefabs.Count > 0 ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                if (prefabs.Count == 0) Debug.LogWarning("[Pool] No prefab in the dropped objects.");
                foreach (var p in prefabs) AddPrefab(p);
            }
            e.Use();
        }

        static IEnumerable<GameObject> Collect(Object o)
        {
            string path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) yield break;

            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    if (go != null) yield return go;
                }
            }
            else if (o is GameObject g && PrefabUtility.IsPartOfPrefabAsset(g))
            {
                yield return g;
            }
        }

        void AddPrefab(GameObject prefab)
        {
            for (int i = 0; i < _entries.arraySize; i++)
                if (_entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue == prefab)
                    return;

            int idx = _entries.arraySize;
            _entries.arraySize++;
            var el = _entries.GetArrayElementAtIndex(idx);
            el.FindPropertyRelative("id").intValue = _nextId.intValue++;
            el.FindPropertyRelative("name").stringValue = PoolEnumGenerator.Sanitize(prefab.name);
            el.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            el.FindPropertyRelative("initialSize").intValue = 8;
            el.FindPropertyRelative("maxSize").intValue = 0;
            el.FindPropertyRelative("expandable").boolValue = true;
            el.FindPropertyRelative("collectionCheck").boolValue = true;
            _entriesChanged = true;
        }

        void DrawEntries()
        {
            EditorGUILayout.LabelField($"Entries ({_entries.arraySize})", EditorStyles.boldLabel);

            var counts = new Dictionary<string, int>();
            for (int i = 0; i < _entries.arraySize; i++)
            {
                string n = PoolEnumGenerator.Sanitize(_entries.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
                counts[n] = counts.TryGetValue(n, out var c) ? c + 1 : 1;
            }

            int remove = -1;
            for (int i = 0; i < _entries.arraySize; i++)
            {
                var el = _entries.GetArrayElementAtIndex(i);
                var pName = el.FindPropertyRelative("name");
                var pPrefab = el.FindPropertyRelative("prefab");
                var pId = el.FindPropertyRelative("id");
                var pInitial = el.FindPropertyRelative("initialSize");
                var pMax = el.FindPropertyRelative("maxSize");
                var pExpand = el.FindPropertyRelative("expandable");
                var pCheck = el.FindPropertyRelative("collectionCheck");

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
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

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(pInitial, new GUIContent("Warm"));
                        EditorGUILayout.PropertyField(pMax, new GUIContent("Max"));
                        pExpand.boolValue = EditorGUILayout.ToggleLeft("Expand", pExpand.boolValue, GUILayout.Width(70));
                        pCheck.boolValue = EditorGUILayout.ToggleLeft("Check", pCheck.boolValue, GUILayout.Width(64));
                    }
                }

                if (pPrefab.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("Prefab missing - this entry is skipped in the enum.", MessageType.Error);
                else if (counts[PoolEnumGenerator.Sanitize(pName.stringValue)] > 1)
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

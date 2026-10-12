using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LSDK.GameData.EditorTools
{
    /// <summary>
    /// Xem / sửa GameData bằng inspector.
    /// - Edit mode: đọc/ghi thẳng PlayerPrefs.
    /// - Play mode: đọc/ghi module đang chạy trong GameDataManager.
    /// </summary>
    public class GameDataWindow : EditorWindow
    {
        private enum Source { Default, Saved, Live, Corrupt }

        private readonly IGameDataStorage _storage = new PlayerPrefsStorage();
        private readonly IDataSerializer _serializer = new JsonDataSerializer(true);

        private List<Type> _types = new List<Type>();
        private int _selected;
        private GameDataHolder _holder;
        private SerializedObject _so;
        private Source _source;
        private Vector2 _listScroll;
        private Vector2 _bodyScroll;
        private bool _showJson = true;

        public static void Open()
        {
            var window = GetWindow<GameDataWindow>("GameData");
            window.minSize = new Vector2(520f, 320f);
            window.Show();
        }

        public static List<Type> FindModuleTypes()
        {
            return TypeCache.GetTypesDerivedFrom<GameDataModule>()
                .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.Name)
                .ToList();
        }

        public void Reload()
        {
            _types = FindModuleTypes();
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _types.Count - 1));
            LoadSelected();
            Repaint();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            _types = FindModuleTypes();
            LoadSelected();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            DestroyHolder();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                Reload();
        }

        private Type SelectedType
        {
            get { return _selected >= 0 && _selected < _types.Count ? _types[_selected] : null; }
        }

        private void DestroyHolder()
        {
            if (_holder != null) DestroyImmediate(_holder);
            _holder = null;
            _so = null;
        }

        private void SetHolder(GameDataModule module)
        {
            DestroyHolder();
            _holder = CreateInstance<GameDataHolder>();
            // HideAndDontSave gồm NotEditable → PropertyField vẽ được nhưng khóa mọi field.
            _holder.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.DontUnloadUnusedAsset;
            _holder.module = module;
            _so = new SerializedObject(_holder);
        }

        private GameDataModule CreateDefault(Type type)
        {
            var module = (GameDataModule)Activator.CreateInstance(type);
            module.InitializeDefaults();
            return module;
        }

        private void LoadSelected()
        {
            DestroyHolder();
            var type = SelectedType;
            if (type == null) return;

            var module = (GameDataModule)Activator.CreateInstance(type);
            var manager = GameDataManager.HasInstance ? GameDataManager.Instance : null;

            if (Application.isPlaying && manager != null && manager.TryGetModule(type, out var live))
            {
                _serializer.Populate(manager.ToJson(live), module);
                _source = Source.Live;
            }
            else if (_storage.HasKey(module.Key))
            {
                try
                {
                    _serializer.Populate(_storage.Read(module.Key), module);
                    _source = Source.Saved;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameData] Đọc '{module.Key}' lỗi: {e.Message}");
                    module = CreateDefault(type);
                    _source = Source.Corrupt;
                }
            }
            else
            {
                module.InitializeDefaults();
                _source = Source.Default;
            }

            SetHolder(module);
        }

        #region GUI
        private void OnGUI()
        {
            DrawToolbar();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawModuleList();
                DrawModuleBody();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Reload();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Register", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                {
                    GenerateRegisterCode();
                    GUIUtility.ExitGUI();
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(Application.isPlaying ? "Play mode (live)" : "Edit mode (PlayerPrefs)", EditorStyles.miniLabel);
            }
        }

        private void DrawModuleList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(170f)))
            {
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                for (int i = 0; i < _types.Count; i++)
                {
                    bool isSelected = i == _selected;
                    if (GUILayout.Toggle(isSelected, _types[i].Name, "Button") && !isSelected)
                    {
                        _selected = i;
                        LoadSelected();
                        GUIUtility.ExitGUI();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawModuleBody()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                var type = SelectedType;
                if (type == null || _so == null || _holder == null || _holder.module == null)
                {
                    EditorGUILayout.HelpBox("Không tìm thấy GameDataModule nào.", MessageType.Info);
                    return;
                }

                _bodyScroll = EditorGUILayout.BeginScrollView(_bodyScroll);

                EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Key", _holder.module.Key);
                EditorGUILayout.LabelField("Nguồn", SourceLabel());
                EditorGUILayout.Space();

                _so.Update();
                var prop = _so.FindProperty("module");
                prop.isExpanded = true;
                EditorGUILayout.PropertyField(prop, new GUIContent("Data"), true);
                _so.ApplyModifiedProperties();

                EditorGUILayout.Space();
                DrawButtons(type);
                DrawJson();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawButtons(Type type)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply")) Apply(type);

                // if (GUILayout.Button("Revert"))
                // {
                //     LoadSelected();
                //     GUIUtility.ExitGUI();
                // }

                if (GUILayout.Button("Reset"))
                {
                    SetHolder(CreateDefault(type)); // chưa ghi, bấm Apply mới lưu
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Delete")) DeleteSaved(type);

            }

            // using (new EditorGUILayout.HorizontalScope())
            // {
                // if (GUILayout.Button("Copy JSON"))
                //     EditorGUIUtility.systemCopyBuffer = CurrentJson();

                // if (GUILayout.Button("Paste JSON")) PasteJson(type);

                // if (GUILayout.Button("Delete saved")) DeleteSaved(type);
            // }
        }

        private void DrawJson()
        {
            EditorGUILayout.Space();
            _showJson = EditorGUILayout.Foldout(_showJson, "JSON preview", true);
            if (!_showJson) return;

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextArea(CurrentJson());
        }
        #endregion

        #region Actions
        private string CurrentJson()
        {
            return _serializer.Serialize(_holder.module);
        }

        private string SourceLabel()
        {
            switch (_source)
            {
                case Source.Saved: return "PlayerPrefs (đã lưu)";
                case Source.Live: return "Runtime (đang chạy)";
                case Source.Corrupt: return "JSON lỗi → đang hiển thị mặc định";
                default: return "Chưa có data → giá trị mặc định";
            }
        }

        private void Apply(Type type)
        {
            string key = _holder.module.Key;
            string json = CurrentJson();

            var manager = GameDataManager.HasInstance ? GameDataManager.Instance : null;
            bool applied = Application.isPlaying && manager != null && manager.ApplyJson(type, json);

            if (!applied)
            {
                _storage.Write(key, json);
                _storage.Flush();
            }

            LoadSelected();
            GUIUtility.ExitGUI();
        }

        private void PasteJson(Type type)
        {
            try
            {
                var module = (GameDataModule)Activator.CreateInstance(type);
                _serializer.Populate(EditorGUIUtility.systemCopyBuffer, module);
                SetHolder(module); // chưa ghi, bấm Apply mới lưu
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Paste JSON", "JSON không hợp lệ:\n" + e.Message, "OK");
            }
            GUIUtility.ExitGUI();
        }

        private void DeleteSaved(Type type)
        {
            if (!EditorUtility.DisplayDialog("Delete saved", $"Xóa data đã lưu của {type.Name}?", "Xóa", "Hủy"))
                return;

            var manager = GameDataManager.HasInstance ? GameDataManager.Instance : null;
            if (Application.isPlaying && manager != null && manager.ResetModule(type))
            {
                // module runtime đã về mặc định và được ghi lại
            }
            else
            {
                string key = _holder.module.Key;
                _storage.Delete(key);
                _storage.Delete(key + GameDataKeys.CorruptSuffix);
                _storage.Flush();
            }

            LoadSelected();
            GUIUtility.ExitGUI();
        }

        private void GenerateRegisterCode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Register", "Thoát Play mode trước khi sinh code đăng ký.", "OK");
                return;
            }

            string assetPath = FindScriptPath(typeof(GameDataManager));
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("Register", "Không tìm thấy file GameDataManager.cs.", "OK");
                return;
            }

            string fullPath = Path.GetFullPath(assetPath);
            string source = File.ReadAllText(fullPath);
            if (!TryGetMethodBody(source, "RegisterModules", out int bodyStart, out int bodyEnd, out string body))
            {
                EditorUtility.DisplayDialog("Register", "Không tìm thấy method RegisterModules().", "OK");
                return;
            }

            var assembly = typeof(GameDataManager).Assembly;
            var types = FindModuleTypes().Where(t => t.Assembly == assembly).ToList();
            var registerLine = new Regex(@"^\s*Register<([^>]+)>\(\);\s*(?://.*)?$");

            var existingNames = new List<string>();
            var customLines = new List<string>();
            foreach (string line in body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var match = registerLine.Match(line);
                if (match.Success)
                {
                    existingNames.Add(match.Groups[1].Value.Trim());
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                    customLines.Add(line);
            }

            var ordered = new List<Type>();
            foreach (string name in existingNames)
            {
                var type = types.FirstOrDefault(t => MatchesRegisterName(t, name));
                if (type != null && !ordered.Contains(type))
                    ordered.Add(type);
            }

            foreach (var type in types)
            {
                if (!ordered.Contains(type))
                    ordered.Add(type);
            }

            var added = ordered.Where(t => existingNames.All(name => !MatchesRegisterName(t, name))).Select(t => t.Name).ToList();
            var removed = existingNames.Where(name => types.All(t => !MatchesRegisterName(t, name))).ToList();
            if (added.Count == 0 && removed.Count == 0)
            {
                EditorUtility.DisplayDialog("Register", "Mọi module đã có trong RegisterModules().", "OK");
                return;
            }

            string nl = source.Contains("\r\n") ? "\r\n" : "\n";
            string indent = "            ";
            var indentMatch = Regex.Match(body, @"(?m)^([ \t]+)Register<");
            if (indentMatch.Success) indent = indentMatch.Groups[1].Value;

            string closeIndent = "        ";
            int lineStart = source.LastIndexOf('\n', bodyEnd - 1);
            if (lineStart >= bodyStart)
            {
                string tail = source.Substring(lineStart + 1, bodyEnd - lineStart - 1);
                if (tail.Trim().Length == 0) closeIndent = tail;
            }

            var generated = new List<string>();
            var sb = new StringBuilder();
            sb.Append(nl);
            foreach (var type in ordered)
            {
                string line = indent + "Register<" + RegisterTypeName(type) + ">();";
                generated.Add(line.Trim());
                sb.Append(line).Append(nl);
            }

            foreach (string line in customLines)
                sb.Append(line).Append(nl);
            sb.Append(closeIndent);

            var message = new StringBuilder();
            message.AppendLine("Ghi vào GameDataManager.RegisterModules()?");
            message.AppendLine();
            foreach (string line in generated)
                message.AppendLine(line);
            if (added.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Thêm: " + string.Join(", ", added));
            }
            if (removed.Count > 0)
            {
                message.AppendLine();
                message.AppendLine("Gỡ: " + string.Join(", ", removed));
            }

            if (!EditorUtility.DisplayDialog("Register", message.ToString().TrimEnd(), "Ghi", "Hủy"))
                return;

            string updated = source.Substring(0, bodyStart) + sb + source.Substring(bodyEnd);
            File.WriteAllText(fullPath, updated, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath);
            Debug.Log("[GameData] Đã cập nhật RegisterModules:\n" + string.Join("\n", generated));
        }

        private static bool MatchesRegisterName(Type type, string name)
        {
            return name == type.Name
                || name == type.FullName
                || name == "global::" + type.FullName;
        }

        private static string RegisterTypeName(Type type)
        {
            if (type.Namespace == typeof(GameDataManager).Namespace)
                return type.Name;
            return "global::" + type.FullName.Replace('+', '.');
        }

        private static string FindScriptPath(Type type)
        {
            foreach (string guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                    return path;
            }

            return null;
        }

        private static bool TryGetMethodBody(string source, string methodName, out int bodyStart, out int bodyEnd, out string body)
        {
            bodyStart = 0;
            bodyEnd = 0;
            body = null;

            int signature = source.IndexOf("void " + methodName + "(", StringComparison.Ordinal);
            if (signature < 0) return false;

            int open = source.IndexOf('{', signature);
            if (open < 0) return false;

            int depth = 0;
            for (int i = open; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        bodyStart = open + 1;
                        bodyEnd = i;
                        body = source.Substring(bodyStart, bodyEnd - bodyStart);
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion
    }
}

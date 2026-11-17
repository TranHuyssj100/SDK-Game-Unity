// Assets/SDK/FireBaseSDK/Editor/RemoteConfigWindow.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LSDK.Firebase.Editor
{
    /// <summary>
    /// Cửa sổ xem Remote Config.
    /// Edit mode: mọi class kế thừa RemoteConfigModule và giá trị mặc định trong code.
    /// Play mode: giá trị đã populate từ Firebase.
    /// </summary>
    public sealed class RemoteConfigWindow : EditorWindow
    {
        private enum Source { Default, Pending, Live }

        private List<Type> _types = new List<Type>();
        private int _selected;
        private RemoteConfigModule _module;
        private Source _source;
        private bool _waitingPopulate;
        private Vector2 _listScroll;
        private Vector2 _bodyScroll;
        private bool _showJson = true;

        [MenuItem("Tools/LSDK/Firebase/Open Remote Config Window", false, 0)]
        public static void Open()
        {
            var window = GetWindow<RemoteConfigWindow>("Remote Config");
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        public static List<Type> FindModuleTypes()
        {
            return TypeCache.GetTypesDerivedFrom<RemoteConfigModule>()
                .Where(t => !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => DisplayName(t))
                .ToList();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Reload();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            _module = null;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
                Reload();
        }

        private void Reload()
        {
            _types = FindModuleTypes();
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _types.Count - 1));
            LoadSelected();
            Repaint();
        }

        private Type SelectedType
        {
            get { return _selected >= 0 && _selected < _types.Count ? _types[_selected] : null; }
        }

        private void LoadSelected()
        {
            Type type = SelectedType;
            if (type == null)
            {
                _module = null;
                return;
            }

            var module = (RemoteConfigModule)Activator.CreateInstance(type);
            _source = Source.Default;
            _waitingPopulate = false;

            if (Application.isPlaying && RemoteConfigManager.HasInstance)
            {
                RemoteConfigModule live = FindLive(type);
                if (live != null && RemoteConfigManager.Instance.IsPopulated)
                {
                    CopyFields(live, module);
                    _source = Source.Live;
                }
                else
                {
                    _source = Source.Pending;
                    _waitingPopulate = live == null || !RemoteConfigManager.Instance.IsPopulated;
                }
            }

            _module = module;
        }

        private static RemoteConfigModule FindLive(Type type)
        {
            IReadOnlyList<RemoteConfigModule> modules = RemoteConfigManager.Instance.AllModules;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] != null && modules[i].GetType() == type)
                    return modules[i];
            }

            return null;
        }

        private static void CopyFields(RemoteConfigModule from, RemoteConfigModule to)
        {
            FieldInfo[] fields = from.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.IsStatic || field.IsInitOnly) continue;
                if (typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                field.SetValue(to, field.GetValue(from));
            }
        }

        private void OnGUI()
        {
            if (_waitingPopulate && Application.isPlaying && RemoteConfigManager.HasInstance && RemoteConfigManager.Instance.IsPopulated)
            {
                _waitingPopulate = false;
                LoadSelected();
            }

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

                if (Application.isPlaying && RemoteConfigManager.HasInstance &&
                    GUILayout.Button("Populate All", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    RemoteConfigManager.Instance.PopulateAll();
                    Reload();
                    GUIUtility.ExitGUI();
                }

                GUILayout.FlexibleSpace();
                string mode = Application.isPlaying ? "Play mode (Firebase)" : "Edit mode (giá trị mặc định)";
                GUILayout.Label(mode, EditorStyles.miniLabel);
            }
        }

        private void DrawModuleList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(180f)))
            {
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                for (int i = 0; i < _types.Count; i++)
                {
                    bool isSelected = i == _selected;
                    if (GUILayout.Toggle(isSelected, DisplayName(_types[i]), "Button") && !isSelected)
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
                Type type = SelectedType;
                if (type == null || _module == null)
                {
                    EditorGUILayout.HelpBox(
                        "Không tìm thấy RemoteConfigModule nào.\n" +
                        "Tạo class kế thừa RemoteConfigModule, gắn [Serializable], đặt trong assembly của game.",
                        MessageType.Info);
                    return;
                }

                _bodyScroll = EditorGUILayout.BeginScrollView(_bodyScroll);

                EditorGUILayout.LabelField(DisplayName(type), EditorStyles.boldLabel);
                string prefix = _module.Prefix;
                EditorGUILayout.LabelField("Prefix", string.IsNullOrEmpty(prefix) ? "(không prefix)" : prefix);
                EditorGUILayout.LabelField("Nguồn", SourceLabel());
                EditorGUILayout.Space(6f);

                DrawKeys();
                DrawJson();

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawKeys()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Key", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Giá trị", EditorStyles.miniLabel);
                GUILayout.Space(156f);
            }

            FieldInfo[] fields = SerializedFields(_module);
            if (fields.Length == 0)
            {
                EditorGUILayout.HelpBox("Module này không có field [SerializeField].", MessageType.Info);
                return;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                string firebaseKey = FirebaseKey(_module, field);
                object value = field.GetValue(_module);
                string valueText = RemoteConfigJson.ToJson(value);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(firebaseKey, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.SelectableLabel(valueText, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                    if (GUILayout.Button("Copy Key", GUILayout.Width(72f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = firebaseKey;
                        Debug.Log("[LSDK.Firebase] Đã copy key: " + firebaseKey);
                    }

                    if (GUILayout.Button("Copy Value", GUILayout.Width(80f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = valueText;
                        Debug.Log("[LSDK.Firebase] Đã copy value: " + valueText);
                    }
                }
            }
        }

        private void DrawJson()
        {
            EditorGUILayout.Space(6f);
            _showJson = EditorGUILayout.Foldout(_showJson, "JSON preview", true);
            if (!_showJson || _module == null) return;

            string json = BuildModuleJson();
            float height = Mathf.Clamp(LineCount(json) * EditorGUIUtility.singleLineHeight + 8f, 64f, 240f);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextArea(json, GUILayout.Height(height));
        }

        private string BuildModuleJson()
        {
            var map = new Dictionary<string, object>();
            FieldInfo[] fields = SerializedFields(_module);
            for (int i = 0; i < fields.Length; i++)
                map[FirebaseKey(_module, fields[i])] = fields[i].GetValue(_module);
            return RemoteConfigJson.ToJson(map, true);
        }

        private string SourceLabel()
        {
            switch (_source)
            {
                case Source.Live: return "Runtime (đã lấy từ Firebase)";
                case Source.Pending: return "Play mode, chưa fetch — đang hiện giá trị mặc định";
                default: return "Giá trị mặc định trong code";
            }
        }

        /// <summary>CurrencyRemoteConfig → Currency. Giữ nguyên nếu sau khi cắt còn rỗng.</summary>
        private static string DisplayName(Type type)
        {
            string name = type.Name ?? string.Empty;
            string trimmed = name.Replace("RemoteConfig", string.Empty);
            return string.IsNullOrEmpty(trimmed) ? name : trimmed;
        }

        private static string FirebaseKey(RemoteConfigModule module, FieldInfo field)
        {
            var attr = field.GetCustomAttribute<RCKeyAttribute>();
            string name = attr != null && !string.IsNullOrEmpty(attr.Key) ? attr.Key : field.Name;
            string prefix = module.Prefix;
            return string.IsNullOrEmpty(prefix) ? name : prefix + "_" + name;
        }

        private static bool IsSerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly) return false;
            if (field.IsDefined(typeof(NonSerializedAttribute), false)) return false;
            if (typeof(Delegate).IsAssignableFrom(field.FieldType)) return false;
            if (field.IsPublic) return true;
            return field.IsDefined(typeof(SerializeField), false);
        }

        private static FieldInfo[] SerializedFields(RemoteConfigModule module)
        {
            FieldInfo[] fields = module.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var list = new List<FieldInfo>(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                if (IsSerializedField(fields[i])) list.Add(fields[i]);
            }

            return list.ToArray();
        }

        private static int LineCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
            }

            return count;
        }
    }
}

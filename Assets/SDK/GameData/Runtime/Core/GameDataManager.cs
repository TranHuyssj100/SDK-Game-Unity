using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LSDK.GameData
{
    /// <summary>
    /// Singleton quản lý toàn bộ GameDataModule: load / save (debounce bằng Coroutine) / reset.
    /// Dùng: GameDataManager.Instance.Get&lt;CurrencyData&gt;().AddCoin(10);
    /// </summary>
    [DisallowMultipleComponent]
    public class GameDataManager : MonoBehaviour
    {
        #region Singleton
        private static GameDataManager _instance;
        private static bool _quitting;

        public static bool HasInstance => _instance != null;

        public static GameDataManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (_quitting) return null;

#if UNITY_2023_1_OR_NEWER
                var found = FindFirstObjectByType<GameDataManager>();
#else
                var found = FindObjectOfType<GameDataManager>();
#endif
                // Awake của component trên scene có thể chưa chạy (OnEnable của UI khác chạy trước)
                if (found != null) found.Setup();
                else CreateInstance();

                return _instance;
            }
        }

        // Phòng trường hợp tắt Domain Reload trong Enter Play Mode Options
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _quitting = false;
        }

        /// <summary>Tạo + đăng ký module trước khi scene đầu tiên load, để Awake/OnEnable của mọi script đều thấy data.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance == null) CreateInstance();
        }

        private static void CreateInstance()
        {
            new GameObject("[GameDataManager]").AddComponent<GameDataManager>(); // Awake sẽ gán _instance
        }
        #endregion

        [Header("Settings")]
        [SerializeField] private bool loadOnAwake = true;

        [Tooltip("Thời gian gom thay đổi trước khi ghi xuống storage. 0 = ghi ngay.")]
        [SerializeField, Min(0f)] private float saveDelay = 1.5f;

        private readonly Dictionary<Type, GameDataModule> _modules = new Dictionary<Type, GameDataModule>();
        private readonly List<GameDataModule> _ordered = new List<GameDataModule>();

        private IGameDataStorage _storage;
        private IDataSerializer _serializer;
        private Coroutine _saveRoutine;
        private bool _initialized;
        private bool _loaded;

        public bool IsLoaded => _loaded;
        public IReadOnlyList<GameDataModule> AllModules => _ordered;

        #region Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                // Bản đặt trên scene (vd Loading) đến sau bản Bootstrap: giữ lại cấu hình Inspector
                _instance.saveDelay = saveDelay;

                // Chỉ xóa cả GameObject khi nó không chứa component nào khác ngoài Transform
                if (GetComponents<Component>().Length <= 2) Destroy(gameObject);
                else Destroy(this);
                return;
            }

            Setup();
        }

        private void Setup()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _loaded) SaveAll();
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
            if (_loaded) SaveAll();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_storage == null) _storage = new PlayerPrefsStorage();
            if (_serializer == null) _serializer = new JsonDataSerializer();

            RegisterDiscoveredModules();
            if (loadOnAwake) LoadAll();
        }

        /// <summary>
        /// Tìm mọi class kế thừa GameDataModule ở các assembly tham chiếu Runtime.
        /// Module trong Assets (sample đã import) được đăng ký mà không sửa file này.
        /// </summary>
        private void RegisterDiscoveredModules()
        {
            var moduleType = typeof(GameDataModule);
            var runtimeName = moduleType.Assembly.GetName().Name;
            var found = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || IsEditorAssembly(assembly.GetName().Name)) continue;
                if (!ReferencesAssembly(assembly, runtimeName)) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }

                if (types == null) continue;
                for (int i = 0; i < types.Length; i++)
                {
                    var type = types[i];
                    if (type == null || type.IsAbstract || !type.IsClass) continue;
                    if (!moduleType.IsAssignableFrom(type)) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        Debug.LogWarning($"[GameData] Bỏ qua {type.FullName}: cần constructor không tham số.");
                        continue;
                    }
                    found.Add(type);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            for (int i = 0; i < found.Count; i++)
                Register(found[i]);
        }

        private static bool ReferencesAssembly(Assembly assembly, string assemblyName)
        {
            if (assembly.GetName().Name == assemblyName) return true;
            var refs = assembly.GetReferencedAssemblies();
            for (int i = 0; i < refs.Length; i++)
                if (refs[i].Name == assemblyName) return true;
            return false;
        }

        private static bool IsEditorAssembly(string name)
        {
            return name.EndsWith(".Editor", StringComparison.Ordinal)
                || name.EndsWith("-Editor", StringComparison.Ordinal);
        }

        /// <summary>Đổi backend (file, cloud...). Nếu đã load thì load lại từ backend mới.</summary>
        public void SetBackend(IGameDataStorage storage, IDataSerializer serializer)
        {
            if (storage != null) _storage = storage;
            if (serializer != null) _serializer = serializer;
            if (_loaded) LoadAll();
        }
        #endregion

        #region Module access
        public T Register<T>() where T : GameDataModule, new()
        {
            return (T)Register(typeof(T));
        }

        public GameDataModule Register(Type type)
        {
            if (type == null || type.IsAbstract || !typeof(GameDataModule).IsAssignableFrom(type))
            {
                Debug.LogError("[GameData] Type không phải GameDataModule cụ thể.");
                return null;
            }

            if (_modules.TryGetValue(type, out var existing)) return existing;

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogError($"[GameData] {type.Name} cần constructor không tham số.");
                return null;
            }

            var module = (GameDataModule)Activator.CreateInstance(type);
            for (int i = 0; i < _ordered.Count; i++)
            {
                if (_ordered[i].Key == module.Key)
                    Debug.LogWarning($"[GameData] {type.Name} trùng key '{module.Key}' với {_ordered[i].GetType().Name}.");
            }

            _modules.Add(type, module);
            _ordered.Add(module);
            module.Changed += HandleModuleChanged;

            if (_loaded)
            {
                LoadModule(module);
                if (module.IsDirty) ScheduleSave();
            }
            return module;
        }

        public T Get<T>() where T : GameDataModule
        {
            if (_modules.TryGetValue(typeof(T), out var module)) return (T)module;
            Debug.LogError($"[GameData] Module {typeof(T).Name} chưa được đăng ký.");
            return null;
        }

        public bool TryGetModule(Type type, out GameDataModule module)
        {
            return _modules.TryGetValue(type, out module);
        }
        #endregion

        #region Load
        public void LoadAll()
        {
            foreach (var module in _ordered) LoadModule(module);

            _loaded = true;
            SaveAll(); // ghi các module mới tạo / vừa migrate
            DataEvents.RaiseAllLoaded();
        }

        public void ReloadAll()
        {
            LoadAll();
        }

        private void LoadModule(GameDataModule module)
        {
            string key = module.Key;
            bool restored = false;

            if (_storage.HasKey(key))
            {
                string json = _storage.Read(key);
                try
                {
                    _serializer.Populate(json, module);
                    if (DataMigration.Run(module)) module.IsDirty = true;
                    restored = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameData] Load '{key}' lỗi, reset về mặc định. Bản gốc được backup ở '{key}{GameDataKeys.CorruptSuffix}'.\n{e}");
                    _storage.Write(key + GameDataKeys.CorruptSuffix, json);
                }
            }

            if (!restored) ResetToDefaults(module);

            module.OnLoaded();
            DataEvents.RaiseModuleLoaded(module);
        }
        #endregion

        #region Save
        public void Save<T>() where T : GameDataModule
        {
            if (!_modules.TryGetValue(typeof(T), out var module)) return;
            WriteModule(module);
            _storage.Flush();
        }

        /// <param name="force">true = ghi hết, bỏ qua cờ dirty.</param>
        public void SaveAll(bool force = false)
        {
            bool any = false;
            foreach (var module in _ordered)
            {
                if (!force && !module.IsDirty) continue;
                WriteModule(module);
                any = true;
            }
            if (any) _storage.Flush();
        }

        private void WriteModule(GameDataModule module)
        {
            module.OnBeforeSave();
            _storage.Write(module.Key, _serializer.Serialize(module));
            module.IsDirty = false;
            DataEvents.RaiseModuleSaved(module);
        }

        private void HandleModuleChanged(GameDataModule module)
        {
            DataEvents.RaiseModuleChanged(module);
            ScheduleSave();
        }

        private void ScheduleSave()
        {
            if (!_loaded) return; // LoadAll sẽ SaveAll ở cuối

            if (saveDelay <= 0f)
            {
                SaveAll();
                return;
            }

            if (_saveRoutine == null) _saveRoutine = StartCoroutine(SaveAfterDelay());
        }

        private IEnumerator SaveAfterDelay()
        {
            yield return new WaitForSecondsRealtime(saveDelay);
            _saveRoutine = null;
            SaveAll();
        }
        #endregion

        #region Reset
        public void ResetModule<T>() where T : GameDataModule
        {
            if (_modules.TryGetValue(typeof(T), out var module)) ResetModule(module);
        }

        public bool ResetModule(Type type)
        {
            if (!_modules.TryGetValue(type, out var module)) return false;
            ResetModule(module);
            SaveAll();
            return true;
        }

        public void ResetAll()
        {
            foreach (var module in _ordered) ResetModule(module);
            SaveAll(true);
        }

        private void ResetModule(GameDataModule module)
        {
            ResetToDefaults(module);
            module.OnLoaded();
            module.Save();
        }

        /// <summary>Tạo bản mặc định rồi đổ ngược vào instance hiện tại (giữ nguyên reference cho UI đang subscribe).</summary>
        private void ResetToDefaults(GameDataModule module)
        {
            var fresh = (GameDataModule)Activator.CreateInstance(module.GetType());
            fresh.InitializeDefaults();
            _serializer.Populate(_serializer.Serialize(fresh), module);
            module.IsDirty = true;
        }
        #endregion

        #region Editor support
        public string ToJson(GameDataModule module)
        {
            return _serializer.Serialize(module);
        }

        /// <summary>Ghi đè data của module đang chạy bằng JSON (dùng cho GameData Window ở Play mode).</summary>
        public bool ApplyJson(Type type, string json)
        {
            if (!_modules.TryGetValue(type, out var module)) return false;

            try { _serializer.Populate(json, module); }
            catch (Exception e)
            {
                Debug.LogError($"[GameData] ApplyJson '{type.Name}' lỗi: {e.Message}");
                return false;
            }

            module.Save();
            SaveAll();
            return true;
        }
        #endregion
    }
}

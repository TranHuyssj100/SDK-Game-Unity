using System;
using System.Collections;
using System.Collections.Generic;
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
                _instance = FindFirstObjectByType<GameDataManager>();
#else
                _instance = FindObjectOfType<GameDataManager>();
#endif
                if (_instance == null)
                    new GameObject("[GameDataManager]").AddComponent<GameDataManager>(); // Awake sẽ gán _instance

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
                Destroy(gameObject);
                return;
            }

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

            RegisterModules();
            if (loadOnAwake) LoadAll();
        }

        /// <summary>Thêm module mới của game ở đây.</summary>
        private void RegisterModules()
        {
            Register<PlayerData>();
            Register<CurrencyData>();
            Register<InventoryData>();
            Register<SettingsData>();
            Register<GameplayData>();
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
            var type = typeof(T);
            if (_modules.TryGetValue(type, out var existing)) return (T)existing;

            var module = new T();
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

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace LSDK.Firebase
{
    /// <summary>
    /// Quản lý toàn bộ RemoteConfigModule: tự khám phá, giữ default, populate sau khi Firebase fetch.
    /// Dùng: RemoteConfigManager.Instance.Get&lt;EconomyConfig&gt;().CoinReward
    /// </summary>
    [DefaultExecutionOrder(-40)]
    [DisallowMultipleComponent]
    public sealed class RemoteConfigManager : MonoBehaviour
    {
        // ─────────────────────────── Singleton ───────────────────────────
        private static RemoteConfigManager _instance;
        private static bool _isQuitting;

        public static bool HasInstance => _instance != null;

        public static RemoteConfigManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (_isQuitting) return null;
                var go = new GameObject("[LSDK] RemoteConfigManager");
                _instance = go.AddComponent<RemoteConfigManager>();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _isQuitting = false;
        }

        // ─────────────────────────── State ───────────────────────────
        private readonly Dictionary<Type, RemoteConfigModule> _modules =
            new Dictionary<Type, RemoteConfigModule>();

        private readonly List<RemoteConfigModule> _ordered = new List<RemoteConfigModule>();

        /// <summary>true sau khi tất cả module đã được Populate ít nhất một lần.</summary>
        [field: SerializeField]
        [field: Tooltip("Bật sau khi tất cả module đã Populate từ Firebase.")]
        public bool IsPopulated { get; private set; }

        public IReadOnlyList<RemoteConfigModule> AllModules => _ordered;

        // ─────────────────────────── Unity ───────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            DiscoverModules();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        // ─────────────────────────── Discovery ───────────────────────────
        private void DiscoverModules()
        {
            var baseType = typeof(RemoteConfigModule);
            var baseAsm = baseType.Assembly.GetName().Name;
            var found = new List<Type>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                string name = asm.GetName().Name;
                if (name.EndsWith(".Editor", StringComparison.Ordinal)) continue;
                if (!ReferencesAssembly(asm, baseAsm)) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                if (types == null) continue;

                foreach (Type t in types)
                {
                    if (t == null || t.IsAbstract || !t.IsClass) continue;
                    if (!baseType.IsAssignableFrom(t)) continue;
                    if (t.GetConstructor(Type.EmptyTypes) == null)
                    {
                        FirebaseLog.Warning("Bỏ qua " + t.FullName + ": cần constructor không tham số.");
                        continue;
                    }

                    found.Add(t);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            foreach (Type t in found) Register(t);
            FirebaseLog.Info("RemoteConfigManager: đã khám phá " + _ordered.Count + " module(s).");
        }

        private static bool ReferencesAssembly(Assembly asm, string target)
        {
            if (asm.GetName().Name == target) return true;
            foreach (AssemblyName r in asm.GetReferencedAssemblies())
                if (r.Name == target) return true;
            return false;
        }

        // ─────────────────────────── Register / Get ───────────────────────────
        /// <summary>Đăng ký thủ công nếu assembly không được auto-discover.</summary>
        public T Register<T>() where T : RemoteConfigModule, new()
        {
            return (T)Register(typeof(T));
        }

        public RemoteConfigModule Register(Type type)
        {
            if (type == null || type.IsAbstract || !typeof(RemoteConfigModule).IsAssignableFrom(type))
            {
                FirebaseLog.Error("Register: type không phải RemoteConfigModule cụ thể.");
                return null;
            }

            if (_modules.TryGetValue(type, out RemoteConfigModule existing)) return existing;
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                FirebaseLog.Error("Register: " + type.Name + " cần constructor không tham số.");
                return null;
            }

            var module = (RemoteConfigModule)Activator.CreateInstance(type);
            _modules[type] = module;
            _ordered.Add(module);
            module.Updated += m => RemoteConfigEvents.RaiseModuleUpdated(m);
            return module;
        }

        /// <summary>Lấy module. Trả null nếu chưa đăng ký.</summary>
        public T Get<T>() where T : RemoteConfigModule
        {
            if (_modules.TryGetValue(typeof(T), out RemoteConfigModule m)) return (T)m;
            FirebaseLog.Error("Get: module " + typeof(T).Name + " chưa được đăng ký.");
            return null;
        }

        public bool TryGet<T>(out T module) where T : RemoteConfigModule
        {
            if (_modules.TryGetValue(typeof(T), out RemoteConfigModule m))
            {
                module = (T)m;
                return true;
            }

            module = null;
            return false;
        }

        // ─────────────────────────── Defaults ───────────────────────────
        /// <summary>Thu gom default từ tất cả module để đẩy vào Firebase.SetDefaultsAsync.</summary>
        public Dictionary<string, object> CollectAllDefaults()
        {
            var result = new Dictionary<string, object>();
            foreach (RemoteConfigModule module in _ordered)
            {
                foreach (KeyValuePair<string, object> pair in module.GetDefaults())
                {
                    if (!result.ContainsKey(pair.Key))
                        result[pair.Key] = pair.Value;
                    else
                        FirebaseLog.Warning("CollectAllDefaults: key '" + pair.Key + "' trùng giữa các module.");
                }
            }

            return result;
        }

        // ─────────────────────────── Populate ───────────────────────────
        /// <summary>Gọi sau khi Firebase FetchAndActivate xong. Điền giá trị remote vào từng module.</summary>
        public void PopulateAll()
        {
            foreach (RemoteConfigModule module in _ordered)
            {
                try
                {
                    module.Populate();
                }
                catch (Exception ex)
                {
                    FirebaseLog.Exception(ex);
                }
            }

            IsPopulated = true;
            RemoteConfigEvents.RaiseAllUpdated();
            FirebaseLog.Info("RemoteConfigManager: đã populate " + _ordered.Count + " module(s).");
        }

        /// <summary>Populate một module đơn lẻ (khi cần refresh riêng lẻ).</summary>
        public void Populate<T>() where T : RemoteConfigModule
        {
            if (!_modules.TryGetValue(typeof(T), out RemoteConfigModule m))
            {
                FirebaseLog.Warning("Populate: module " + typeof(T).Name + " chưa đăng ký.");
                return;
            }

            m.Populate();
        }
    }
}

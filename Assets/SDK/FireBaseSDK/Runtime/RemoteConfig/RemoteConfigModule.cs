using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Firebase.RemoteConfig;
using UnityEngine;

namespace LSDK.Firebase
{
    /// <summary>
    /// Base cho mọi nhóm Remote Config. Kế thừa class này, khai báo field serializable,
    /// Firebase key mặc định là field name. Gắn [RCKey("ten_key")] để đổi tên.
    /// <code>
    /// [Preserve]
    /// public sealed class EconomyConfig : RemoteConfigModule
    /// {
    ///     [SerializeField] private int coinReward = 10;
    ///     [SerializeField] private bool showBanner = true;
    ///     public int  CoinReward  => coinReward;
    ///     public bool ShowBanner  => showBanner;
    /// }
    /// </code>
    /// </summary>
    [Serializable]
    public abstract class RemoteConfigModule
    {
        /// <summary>Nhóm prefix. Mặc định rỗng (không prefix). Override để nhóm key theo module.</summary>
        public virtual string Prefix => string.Empty;

        /// <summary>Bắn sau khi Populate chạy xong (đã lấy giá trị remote).</summary>
        public event Action<RemoteConfigModule> Updated;

        /// <summary>Trả về dictionary default: fieldName (hoặc [RCKey]) → giá trị từ field.</summary>
        public Dictionary<string, object> GetDefaults()
        {
            var result = new Dictionary<string, object>();
            foreach (FieldInfo field in GetFields())
            {
                string key = GetKey(field);
                object value = field.GetValue(this);
                if (value == null) continue;
                result[key] = ToRemoteValue(value);
            }

            return result;
        }

        /// <summary>Đọc giá trị từ Firebase RemoteConfig và ghi vào field.</summary>
        public void Populate()
        {
            foreach (FieldInfo field in GetFields())
            {
                string key = GetKey(field);
                try
                {
                    ConfigValue cv = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
                    FirebaseLog.Info(key + " = " + cv.StringValue + " (" + cv.Source + ")");
                    if (cv.Source == ValueSource.StaticValue) continue;
                    SetField(this, field, cv);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LSDK.Firebase] Populate field '" + field.Name + "' (key=" + key + "): " + ex.Message);
                }
            }

            Updated?.Invoke(this);
        }

        // ─────────────── helpers ───────────────

        private string GetKey(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<RCKeyAttribute>();
            string name = attr != null ? attr.Key : field.Name;
            return string.IsNullOrEmpty(Prefix) ? name : Prefix + "_" + name;
        }

        private IEnumerable<FieldInfo> GetFields()
        {
            return GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        private static object ToRemoteValue(object value)
        {
            if (value is bool b) return b;
            if (value is long l) return l;
            if (value is int i) return (long)i;
            if (value is double d) return d;
            if (value is float f) return (double)f;
            return value.ToString();
        }

        private static void SetField(object target, FieldInfo field, ConfigValue cv)
        {
            Type t = field.FieldType;
            if (t == typeof(string))   { field.SetValue(target, cv.StringValue);            return; }
            if (t == typeof(bool))     { field.SetValue(target, cv.BooleanValue);           return; }
            if (t == typeof(long))     { field.SetValue(target, cv.LongValue);              return; }
            if (t == typeof(int))      { field.SetValue(target, (int)cv.LongValue);         return; }
            if (t == typeof(double))   { field.SetValue(target, cv.DoubleValue);            return; }
            if (t == typeof(float))    { field.SetValue(target, (float)cv.DoubleValue);     return; }
        }
    }
}

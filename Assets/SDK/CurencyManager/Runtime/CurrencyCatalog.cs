// Assets/SDK/CurencyManager/Runtime/CurrencyCatalog.cs
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>
    /// Danh sách loại tiền. Đặt asset trong Resources tại <c>LSDK/Currency/CurrencyCatalog</c>.
    /// Tạo bằng Tools → LSDK → Currency → Create Catalog.
    /// </summary>
    [CreateAssetMenu(fileName = "CurrencyCatalog", menuName = "LSDK/Currency/Catalog", order = 0)]
    public sealed class CurrencyCatalog : ScriptableObject
    {
        /// <summary>Đường dẫn Resources, không gồm đuôi asset.</summary>
        public const string ResourcePath = "LSDK/Currency/CurrencyCatalog";

        static CurrencyCatalog _cached;
        static bool _missing;

        [SerializeField] private List<CurrencyDefinition> _definitions = new List<CurrencyDefinition>();

        /// <summary>Mọi loại tiền đã khai báo, theo thứ tự trong asset.</summary>
        public IReadOnlyList<CurrencyDefinition> Definitions => _definitions;

        /// <summary>Xóa cache. Gọi sau khi tạo hoặc sửa asset trong Editor.</summary>
        public static void ClearCache()
        {
            _cached = null;
            _missing = false;
        }

        /// <summary>
        /// Load catalog từ Resources. Null nếu chưa tạo asset.
        /// Thiếu asset thì chỉ thử một lần, gọi <see cref="ClearCache"/> để thử lại.
        /// </summary>
        public static CurrencyCatalog Load()
        {
            if (_cached != null) return _cached;
            if (_missing) return null;

            _cached = Resources.Load<CurrencyCatalog>(ResourcePath);
            if (_cached == null) _missing = true;
            return _cached;
        }

        /// <summary>Tìm định nghĩa theo enum.</summary>
        public bool TryGet(CurrencyType type, out CurrencyDefinition definition)
        {
            return TryGet((int)type, out definition);
        }

        /// <summary>Tìm định nghĩa theo id.</summary>
        public bool TryGet(int id, out CurrencyDefinition definition)
        {
            definition = null;
            if (_definitions == null) return false;
            for (int i = 0; i < _definitions.Count; i++)
            {
                var entry = _definitions[i];
                if (entry != null && entry.Id == id)
                {
                    definition = entry;
                    return true;
                }
            }
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            _cached = null;
            _missing = false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace LSDK.IAP
{
    [CreateAssetMenu(menuName = "SDK/IAP Catalog", fileName = "IAPCatalog")]
    public class IAPProductCatalog : ScriptableObject
    {
        public List<IAPProductDefinition> products = new List<IAPProductDefinition>();

        Dictionary<string, IAPProductDefinition> _map;

        public IAPProductDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_map == null) Rebuild();
            _map.TryGetValue(id, out var def);
            return def;
        }

        void Rebuild()
        {
            _map = new Dictionary<string, IAPProductDefinition>();
            foreach (var p in products)
            {
                if (p == null || string.IsNullOrEmpty(p.id)) continue;
                _map[p.id] = p;
            }
        }

        void OnValidate() => _map = null;
    }
}

using UnityEngine;
using SDK.Core;
using System.Collections.Generic;
namespace SDK.ObjectPool
{
    public class PoolServer : SingletonBehaviour<PoolServer>
    {
        [SerializeField] private PoolDatabaseSO _poolDatabase;
        private Dictionary<string, IObjectPool> _dictPools = new Dictionary<string, IObjectPool>();

        protected override void OnInitialize()
        {
            if (_poolDatabase == null)
            {
                Debug.LogError("[PoolServer] chưa gán PoolDatabase.");
                return;
            }
            foreach (var entry in _poolDatabase.Entries)
            {
                if (entry == null || entry.Prefab == null || string.IsNullOrEmpty(entry.Id))
                {
                    continue;
                }
                if (_dictPools.ContainsKey(entry.Id))
                {
                    Debug.LogError($"[PoolServer] pool {entry.Id} đã tồn tại.");
                    continue;
                }
                var container = new GameObject($"Pool_{entry.Id}").transform;
                container.SetParent(transform, false);
                _dictPools[entry.Id] = new ObjectPool(entry, container);
            }
        }
        public GameObject GetObject(string id)
        {
            if (!_dictPools.TryGetValue(id, out var pool))
            {
                Debug.LogError($"[PoolServer] pool {id} không tồn tại.");
                return null;
            }
            return pool.GetObject();
        }
        public T GetObject<T>(string id) where T : Component
        {
            var obj = GetObject(id);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        public void ReturnObject(string id, GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError("[PoolServer] object is null.");
                return;
            }
            if (!_dictPools.TryGetValue(id, out var pool))
            {
                Debug.LogError($"[PoolServer] pool {id} không tồn tại.");
                return;
            }
            pool.ReturnObject(obj);
        }
        public void ReturnObject(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError("[PoolServer] object is null.");
                return;
            }
            var maker = obj.GetComponent<PoolInstance>();
            if (maker == null || string.IsNullOrEmpty(maker.PoolId))
            {
                Debug.LogError("[PoolServer] object is not a pooled object.");
                return;
            }
            ReturnObject(maker.PoolId, obj);
        }
        public bool HasPool(string id) => _dictPools.ContainsKey(id);

    }
}
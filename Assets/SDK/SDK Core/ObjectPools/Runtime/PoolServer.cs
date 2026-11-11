using System;
using System.Collections.Generic;
using LSDK.Core.Base;
using UnityEngine;

namespace LSDK.Core.ObjectPools
{
    public class PoolServer : SingletonBehaviour<PoolServer>
    {
        [SerializeField] private PoolDatabaseSO _poolDatabase;
        private readonly Dictionary<int, IObjectPool> _dictPools = new Dictionary<int, IObjectPool>();

        protected override void OnInitialize()
        {
            if (_poolDatabase == null)
            {
                Debug.LogError("[PoolServer] chưa gán PoolDatabase.");
                return;
            }
            foreach (var entry in _poolDatabase.Entries)
            {
                if (entry == null || entry.Prefab == null || entry.id <= 0)
                    continue;
                if (_dictPools.ContainsKey(entry.id))
                {
                    Debug.LogError($"[PoolServer] pool {entry.Name} (#{entry.id}) đã tồn tại.");
                    continue;
                }
                var label = string.IsNullOrEmpty(entry.Name) ? entry.id.ToString() : entry.Name;
                var container = new GameObject($"Pool_{label}").transform;
                container.SetParent(transform, false);
                _dictPools[entry.id] = new ObjectPool(entry, container);
            }
        }

        public GameObject GetObject(Enum id)
        {
            if (id == null)
            {
                Debug.LogError("[PoolServer] id is null.");
                return null;
            }
            return GetById(Convert.ToInt32(id));
        }

        public T GetObject<T>(Enum id) where T : Component
        {
            var obj = GetObject(id);
            return obj != null ? obj.GetComponent<T>() : null;
        }

        public void ReturnObject(Enum id, GameObject obj)
        {
            if (id == null)
            {
                Debug.LogError("[PoolServer] id is null.");
                return;
            }
            ReturnById(Convert.ToInt32(id), obj);
        }

        public void ReturnObject(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError("[PoolServer] object is null.");
                return;
            }
            var maker = obj.GetComponent<PoolInstance>();
            if (maker == null || maker.PoolId <= 0)
            {
                Debug.LogError("[PoolServer] object is not a pooled object.");
                return;
            }
            ReturnById(maker.PoolId, obj);
        }

        public bool HasPool(Enum id) => id != null && _dictPools.ContainsKey(Convert.ToInt32(id));

        GameObject GetById(int id)
        {
            if (!_dictPools.TryGetValue(id, out var pool))
            {
                Debug.LogError($"[PoolServer] pool #{id} không tồn tại.");
                return null;
            }
            return pool.GetObject();
        }

        void ReturnById(int id, GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError("[PoolServer] object is null.");
                return;
            }
            if (!_dictPools.TryGetValue(id, out var pool))
            {
                Debug.LogError($"[PoolServer] pool #{id} không tồn tại.");
                return;
            }
            pool.ReturnObject(obj);
        }
    }
}

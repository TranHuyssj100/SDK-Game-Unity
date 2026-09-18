using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SDK.ObjectPool
{
    public class ObjectPool : IObjectPool
    {
        private readonly PoolEntry _entry;
        private readonly Transform _container;
        private readonly Queue<GameObject> _inactive = new Queue<GameObject>();
        private readonly HashSet<GameObject> _inactiveSet = new HashSet<GameObject>();

        private readonly Dictionary<GameObject, IPooledObject[]> _handlers = new Dictionary<GameObject, IPooledObject[]>();


        private int _created;

        public string Id => _entry.Id;
        public GameObject Prefab => _entry.Prefab;
        public int CountInActive => _inactive.Count;
        public int CountActive => _inactiveSet.Count;

        public ObjectPool(PoolEntry entry, Transform container)
        {
            _entry = entry;
            _container = container;
            Prewarm(entry.InitialSize);
        }

        public GameObject GetObject()
        {
            GameObject go = _inactive.Count > 0 ? _inactive.Dequeue() : CreateNew();
            if (go == null)
            {
                Debug.LogError($"Failed to create new object for pool {Id}");
                return null;
            }
            go.SetActive(true);
            return go;
        }
        public int InitPool(int size, GameObject prefab)
        {
            throw new NotImplementedException();
        }
        public void ReturnObject(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogError($"Object is null for pool {Id}");
                return;
            }
            if (_entry.CollectionCheck && _inactiveSet.Contains(obj))
            {
                Debug.LogError($"Object {obj.name} is already in the pool {Id}");
                return;
            }
            NotifyReturn(obj);
            obj.SetActive(false);
            obj.transform.SetParent(_container, false);
            _inactive.Enqueue(obj);
            _inactiveSet.Add(obj);
        }

        public void Clear(bool destroyObjects)
        {
            if (destroyObjects)
            {
                while (_inactive.Count > 0)
                {
                    GameObject.Destroy(Dequeue());
                }
            }
            _inactive.Clear();
            _inactiveSet.Clear();
            _handlers.Clear();
            _created = 0;
        }

        private GameObject Dequeue()
        {
            var go = _inactive.Dequeue();
            _inactiveSet.Remove(go);
            return go;
        }

        private void NotifyGet(GameObject obj)
        {
            if (!_handlers.TryGetValue(obj, out var handlers))
            {
                Debug.LogError($"No handlers for object {obj.name} in pool {Id}");
                return;
            }
            foreach (var handler in handlers)
            {
                handler.OnObjectGet();
            }
        }
        private void NotifyReturn(GameObject obj)
        {
            if (!_handlers.TryGetValue(obj, out var handlers))
            {
                Debug.LogError($"No handlers for object {obj.name} in pool {Id}");
                return;
            }
            foreach (var handler in handlers)
            {
                handler.OnObjectReturn();
            }
        }

        private GameObject CreateNew()
        {
            if (_entry.Prefab == null)
            {
                Debug.LogError($"No prefab for pool {Id}");
                return null;
            }
            if (_entry.MaxSize > 0 && _created >= _entry.MaxSize)
            {
                Debug.Log($"Max size reached for pool {Id}");
                return null;
            }
            if (!_entry.Expandable && _created >= _entry.InitialSize && _inactive.Count == 0)
            {
                Debug.Log($"Pool {Id} is not expandable and max size reached");
                return null;
            }
            var go = UnityEngine.Object.Instantiate(_entry.Prefab, _container);
            go.name = $"{_entry.Prefab.name}_{_created}";
            var maker = go.GetComponent<PoolInstance>();
            if (maker == null)
            {
                maker = go.AddComponent<PoolInstance>();
            }
            maker.PoolId = Id;
            _handlers[go] = go.GetComponentsInChildren<IPooledObject>(true);
            _created++;
            return go;
        }



        private void Prewarm(int initialSize)
        {
            for (int i = 0; i < initialSize; i++)
            {
                var go = CreateNew();
                if (go == null)
                {
                    Debug.LogError($"Failed to create new object for pool {Id}");
                    return;
                }
                go.SetActive(false);
                _inactive.Enqueue(go);
                _inactiveSet.Add(go);
            }
        }

    }
    public sealed class PoolInstance : MonoBehaviour
    {
        public string PoolId;
    }
}
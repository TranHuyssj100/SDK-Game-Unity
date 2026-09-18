using UnityEngine;
using System.Collections.Generic;
using System.Data.Common;

namespace SDK.ObjectPool
{
    [CreateAssetMenu(fileName = "PoolDatabase", menuName = "SDK/Scriptable Object/Pool Database")]
    public class PoolDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<PoolEntry> _entries = new List<PoolEntry>();
        public List<PoolEntry> Entries => _entries;

    }
}
[System.Serializable]
public class PoolEntry
{
    [SerializeField] private string _id;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _initialSize = 8;
    [SerializeField] private int _maxSize = 0;
    [SerializeField] private bool _expandable = true;
    [SerializeField] private bool collectionCheck = true;

    public string Id => string.IsNullOrEmpty(_id) ? _prefab.name : _id;
    public GameObject Prefab => _prefab;
    public int InitialSize => Mathf.Max(0, _initialSize);
    public int MaxSize => Mathf.Max(0, _maxSize);
    public bool Expandable => _expandable;
    public bool CollectionCheck => collectionCheck;

}
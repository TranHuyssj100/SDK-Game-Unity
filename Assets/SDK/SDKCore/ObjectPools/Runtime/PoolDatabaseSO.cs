using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LSDK.Core.ObjectPools
{
    [CreateAssetMenu(menuName = "LSDK/Object Pools/Pool Database", fileName = "PoolDatabase")]
    public class PoolDatabaseSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public int id;
            [FormerlySerializedAs("_id")] public string name;
            [FormerlySerializedAs("_prefab")] public GameObject prefab;
            [FormerlySerializedAs("_initialSize")] public int initialSize = 8;
            [FormerlySerializedAs("_maxSize")] public int maxSize;
            [FormerlySerializedAs("_expandable")] public bool expandable = true;
            public bool collectionCheck = true;

            public string Name => string.IsNullOrWhiteSpace(name) && prefab != null ? prefab.name : name;
            public GameObject Prefab => prefab;
            public int InitialSize => Mathf.Max(0, initialSize);
            public int MaxSize => Mathf.Max(0, maxSize);
            public bool Expandable => expandable;
            public bool CollectionCheck => collectionCheck;
        }

        [FormerlySerializedAs("_entries")]
        public List<Entry> entries = new List<Entry>();
        [HideInInspector] public int nextId = 1;

        /// <summary>Thư mục cố định ghi enum. Không đổi theo game.</summary>
        public const string EnumOutputFolder = "Assets/Resources/LSDK/ObjectPools/EnumID";

        public string enumName = "PoolId";
        public string enumNamespace = "LSDK.Core.ObjectPools";
        [Tooltip("Luôn ghi vào Assets/Resources/LSDK/ObjectPools/EnumID.")]
        public string outputFolder = EnumOutputFolder;
        public bool autoGenerate = true;
        [HideInInspector] public string generatedPath;

        public List<Entry> Entries => entries;

        Dictionary<int, Entry> _byId;

        void OnEnable() { _byId = null; }
        void OnValidate() { _byId = null; }

        void Build()
        {
            _byId = new Dictionary<int, Entry>();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null || e.prefab == null || e.id <= 0) continue;
                _byId[e.id] = e;
            }
        }

        public bool TryGet(int id, out Entry entry)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out entry);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.UISystem
{
    [CreateAssetMenu(menuName = "LSDK/UI System/UI Registry", fileName = "UIRegistry")]
    public class UIRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public int id;          // ổn định, không đổi khi xoá/thêm entry khác
            public string name;     // tên member trong enum
            public UIPanel prefab;
        }

        public List<Entry> entries = new List<Entry>();
        [HideInInspector] public int nextId = 1;

        /// <summary>Thư mục cố định ghi enum. Không đổi theo game.</summary>
        public const string EnumOutputFolder = "Assets/Resources/LSDK/UI/EnumID";

        // Enum generation (dùng bởi Editor)
        public string enumName = "UIId";
        public string enumNamespace = "LSDK.UISystem";
        [Tooltip("Luôn ghi vào Assets/Resources/LSDK/UI/EnumID.")]
        public string outputFolder = EnumOutputFolder;
        public bool autoGenerate = true;
        [HideInInspector] public string generatedPath;

        Dictionary<int, UIPanel> _byId;
        Dictionary<Type, int> _idByType;

        void OnEnable() { _byId = null; _idByType = null; }
        void OnValidate() { _byId = null; _idByType = null; }

        void Build()
        {
            _byId = new Dictionary<int, UIPanel>();
            _idByType = new Dictionary<Type, int>();
            foreach (var e in entries)
            {
                if (e == null || e.prefab == null) continue;
                _byId[e.id] = e.prefab;
                var t = e.prefab.GetType();
                if (!_idByType.ContainsKey(t)) _idByType[t] = e.id;
            }
        }

        public bool TryGetPrefab(int id, out UIPanel prefab)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out prefab);
        }

        public bool TryGetId(Type panelType, out int id)
        {
            if (_idByType == null) Build();
            return _idByType.TryGetValue(panelType, out id);
        }
    }
}

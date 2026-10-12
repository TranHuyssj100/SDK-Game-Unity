using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.GameData
{
    [Serializable]
    public class InventoryItem
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class InventoryData : GameDataModule
    {
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        public override string Key => GameDataKeys.Inventory;

        public IReadOnlyList<InventoryItem> Items => items;

        public int GetCount(string id)
        {
            var item = Find(id);
            return item != null ? item.count : 0;
        }

        public void Add(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;

            var item = Find(id);
            if (item == null) items.Add(new InventoryItem { id = id, count = amount });
            else item.count += amount;

            Save();
        }

        public bool Remove(string id, int amount = 1)
        {
            var item = Find(id);
            if (item == null || amount <= 0 || item.count < amount) return false;

            item.count -= amount;
            if (item.count == 0) items.Remove(item);

            Save();
            return true;
        }

        private InventoryItem Find(string id)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].id == id) return items[i];
            return null;
        }
    }
}

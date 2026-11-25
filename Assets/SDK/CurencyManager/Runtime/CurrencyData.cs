// Assets/SDK/CurencyManager/Runtime/CurrencyData.cs
using System;
using System.Collections.Generic;
using LSDK.GameData;
using UnityEngine;

namespace LSDK.Currency
{
    /// <summary>
    /// Save số dư mọi loại tiền. GameDataManager tự đăng ký class này.
    /// Đọc và ghi số dư qua <see cref="CurrencyManager"/>.
    /// </summary>
    [Serializable]
    public sealed class CurrencyData : GameDataModule
    {
        [SerializeField] private List<CurrencyRecord> _currencyRecords = new List<CurrencyRecord>();

        /// <summary>Key PlayerPrefs của ví.</summary>
        public override string Key => GameDataKeys.Currency;

        /// <summary>Profile mới: currency hồi thì đầy trần và chưa có lịch, currency thường lấy số khởi tạo.</summary>
        protected override void OnCreated()
        {
            if (_currencyRecords == null) _currencyRecords = new List<CurrencyRecord>();
            _currencyRecords.Clear();

            var catalog = CurrencyCatalog.Load();
            if (catalog == null) return;

            var definitions = catalog.Definitions;
            if (definitions == null) return;
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null) continue;
                var record = new CurrencyRecord(def.Id);
                long amount = def.UseRegen ? def.MaxAmount : def.InitialAmount;
                if (amount < 0) amount = 0;
                record.SetAmount(amount);
                record.SetNextRegenUnix(0);
                record.SetUnlimitedUntilUnix(0);
                record.SetSeeded(true);
                _currencyRecords.Add(record);
            }
        }

        /// <summary>Sau khi load: seed bản ghi thiếu, rồi bắt kịp thời gian offline.</summary>
        public override void OnLoaded()
        {
            CatchUp(CurrencyClock.UtcNow());
        }

        internal void CatchUp(long now)
        {
            var catalog = CurrencyCatalog.Load();
            if (catalog == null)
            {
                CurrencyLog.Warning("Không thấy CurrencyCatalog. Tools → LSDK → Currency → Create Catalog.");
                return;
            }

            var definitions = catalog.Definitions;
            if (definitions == null) return;

            bool dirty = false;
            for (int i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                if (def == null) continue;
                var record = GetOrCreate(def.Id);
                if (RegenWallet.Seed(record, def)) dirty = true;
                bool amountChanged;
                if (RegenWallet.Refresh(record, def, now, out amountChanged)) dirty = true;
            }

            if (dirty) Save();
        }

        internal bool TryGet(int typeId, out CurrencyRecord record)
        {
            record = null;
            if (_currencyRecords == null) return false;
            for (int i = 0; i < _currencyRecords.Count; i++)
            {
                var entry = _currencyRecords[i];
                if (entry != null && entry.TypeId == typeId)
                {
                    record = entry;
                    return true;
                }
            }
            return false;
        }

        internal CurrencyRecord GetOrCreate(int typeId)
        {
            if (TryGet(typeId, out var found)) return found;
            if (_currencyRecords == null) _currencyRecords = new List<CurrencyRecord>();
            var created = new CurrencyRecord(typeId);
            _currencyRecords.Add(created);
            return created;
        }
    }
}

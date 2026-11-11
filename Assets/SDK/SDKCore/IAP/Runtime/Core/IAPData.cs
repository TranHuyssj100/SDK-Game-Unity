using System;
using System.Collections.Generic;
using LSDK.GameData;
using UnityEngine;

namespace LSDK.IAP
{
    /// <summary>Trang thai 1 goi subscription. expireUtcTicks = 0 neu store khong tra ngay het han.</summary>
    [Serializable]
    public class SubscriptionRecord
    {
        public string productId;
        public bool active;
        public long expireUtcTicks;
    }

    /// <summary>
    /// GameDataModule luu trang thai IAP: non-consumable da so huu, subscription con han, lich su transaction.
    /// GameDataManager tu tim class nay luc khoi tao. IAPManager.Init goi Register lai de lay instance.
    /// Xem trong GameData Window nhu cac module khac.
    /// </summary>
    [Serializable]
    public class IAPData : GameDataModule
    {
        const int MaxTransactionHistory = 200;

        [SerializeField] private List<string> ownedNonConsumables = new List<string>();
        [SerializeField] private List<string> processedTransactionIds = new List<string>();
        [SerializeField] private List<SubscriptionRecord> subscriptions = new List<SubscriptionRecord>();

        public override string Key => GameDataKeys.Prefix + "IAP";

        public IReadOnlyList<string> OwnedNonConsumables => ownedNonConsumables;
        public IReadOnlyList<SubscriptionRecord> Subscriptions => subscriptions;

        public bool IsOwned(string productId) => ownedNonConsumables.Contains(productId);

        /// <summary>
        /// true khi store bao con hieu luc va (neu co ngay het han) chua qua han theo UTC.
        /// Qua han giua session van tra false, khong can doi refresh.
        /// </summary>
        public bool IsSubscribed(string productId)
        {
            var rec = FindSubscription(productId);
            if (rec == null || !rec.active) return false;
            if (rec.expireUtcTicks > 0 && DateTime.UtcNow.Ticks >= rec.expireUtcTicks) return false;
            return true;
        }

        public DateTime? GetSubscriptionExpireUtc(string productId)
        {
            var rec = FindSubscription(productId);
            if (rec == null || rec.expireUtcTicks <= 0) return null;
            return new DateTime(rec.expireUtcTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Ghi trang thai tu store. expireUtcTicks &lt;= 0 = khong biet han: giu ngay cu neu con trong tuong lai,
        /// nguoc lai bo ngay (de active quyet dinh). Tra true neu data doi.
        /// </summary>
        public bool SetSubscription(string productId, bool active, long expireUtcTicks)
        {
            if (string.IsNullOrEmpty(productId)) return false;

            var rec = FindSubscription(productId);
            if (rec == null)
            {
                if (!active) return false;
                rec = new SubscriptionRecord { productId = productId };
                subscriptions.Add(rec);
            }

            long nextExpire = expireUtcTicks > 0
                ? expireUtcTicks
                : (rec.expireUtcTicks > DateTime.UtcNow.Ticks ? rec.expireUtcTicks : 0);
            if (rec.active == active && rec.expireUtcTicks == nextExpire) return false;

            rec.active = active;
            rec.expireUtcTicks = nextExpire;
            Save();
            return true;
        }

        SubscriptionRecord FindSubscription(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            for (int i = 0; i < subscriptions.Count; i++)
            {
                if (subscriptions[i] != null && subscriptions[i].productId == productId)
                    return subscriptions[i];
            }
            return null;
        }

        public bool IsProcessed(string transactionId) =>
            !string.IsNullOrEmpty(transactionId) && processedTransactionIds.Contains(transactionId);

        public void AddOwned(string productId)
        {
            if (string.IsNullOrEmpty(productId) || ownedNonConsumables.Contains(productId)) return;
            ownedNonConsumables.Add(productId);
            Save();
        }

        public void MarkProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            processedTransactionIds.Add(transactionId);
            if (processedTransactionIds.Count > MaxTransactionHistory)
                processedTransactionIds.RemoveRange(0, processedTransactionIds.Count - MaxTransactionHistory);
            Save();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.IAP
{
    public interface IStoreAdapter
    {
        /// <summary>true = san sang ban hang.</summary>
        event Action<bool> OnInitialized;

        /// <summary>Store da xac nhan thanh toan, dang cho confirm. receipt.productId = ID noi bo.</summary>
        event Action<IAPReceipt> OnPurchaseSucceeded;

        /// <summary>(productId, reason)</summary>
        event Action<string, IAPFailReason> OnPurchaseFailed;

        /// <summary>
        /// Non-consumable da confirm tu truoc (cai lai game, may moi), khong di qua OnPurchaseSucceeded.
        /// Khong can confirm lai.
        /// </summary>
        event Action<IAPReceipt> OnPurchaseRestored;

        /// <summary>
        /// (productId, active, expireUtcTicks) trang thai subscription tu store (kiem tra han, don da confirm).
        /// expireUtcTicks = 0 neu khong biet han.
        /// </summary>
        event Action<string, bool, long> OnEntitlementUpdated;

        void Initialize(IEnumerable<IAPProductDefinition> products, MonoBehaviour runner);
        void Purchase(string productId);

        /// <summary>Finish transaction (consume / acknowledge). Goi sau khi da cap qua + luu.</summary>
        void ConfirmPurchase(string productId);

        void RestorePurchases(Action<bool> done);
        string GetLocalizedPrice(string productId);

        /// <summary>Hoi store quyen so huu hien tai. Ket qua ban qua OnEntitlementUpdated.</summary>
        void RefreshEntitlements();

        /// <summary>Han UTC cua goi vua mua / dang pending. 0 neu store khong tra.</summary>
        long GetSubscriptionExpireUtcTicks(string productId);
    }
}

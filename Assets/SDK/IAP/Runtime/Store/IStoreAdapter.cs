using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.IAP
{
    public interface IStoreAdapter
    {
        /// <summary>true = san sang ban hang.</summary>
        event Action<bool> OnInitialized;

        /// <summary>(transactionId, productId) - store da xac nhan, dang cho confirm.</summary>
        event Action<string, string> OnPurchaseSucceeded;

        /// <summary>(productId, reason)</summary>
        event Action<string, IAPFailReason> OnPurchaseFailed;

        /// <summary>
        /// (productId, active, expireUtcTicks) cho non-consumable / subscription da so huu tren store
        /// nhung khong di qua OnPurchaseSucceeded (da confirm tu truoc, cai lai game, kiem tra han).
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

using System;

namespace LSDK.IAP
{
    public static class IAPStoreName
    {
        public const string GooglePlay = "GooglePlay";
        public const string AppleAppStore = "AppleAppStore";
        public const string Mock = "Mock";
        public const string Unknown = "Unknown";
    }

    /// <summary>
    /// Du lieu gui len server de verify. Serialize bang JsonUtility nen chi dung field public.
    /// Server KHONG duoc tin storeProductId client gui: phai tu tra catalog theo productId
    /// roi so voi productId ma Google / Apple tra ve.
    /// </summary>
    [Serializable]
    public class IAPReceipt
    {
        /// <summary>ID noi bo trong IAPProductCatalog, vd "coin_pack_1".</summary>
        public string productId;

        /// <summary>SKU ky vong theo catalog (googleId / appleId).</summary>
        public string storeProductId;

        public string productType;

        /// <summary>IAPStoreName.*</summary>
        public string store;

        /// <summary>Google: purchase token. Apple: transaction id.</summary>
        public string transactionId;

        /// <summary>Google Play purchase token, dung cho purchases.products.get / subscriptionsv2.get.</summary>
        public string purchaseToken;

        public string orderId;

        /// <summary>Apple StoreKit 2 JWS (signedTransactionInfo), verify chu ky bang root cert Apple.</summary>
        public string signedTransaction;

        public string originalTransactionId;

        /// <summary>Package name Android / bundle id iOS cua ban build dang chay.</summary>
        public string packageName;

        /// <summary>ID tai khoan game (IAPManager.UserId) de server gan don hang voi nguoi choi.</summary>
        public string userId;

        /// <summary>Receipt JSON goc cua Unity ({"Store","TransactionID","Payload"}), de server log / doi chieu.</summary>
        public string unityReceipt;

        /// <summary>true = don khoi phuc (non-consumable da confirm tu truoc), khong phai mua moi.</summary>
        public bool restored;
    }
}

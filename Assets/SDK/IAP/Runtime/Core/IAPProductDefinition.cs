using System;
using UnityEngine;

namespace LSDK.IAP
{
    public enum IAPProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    [Serializable]
    public class IAPProductDefinition
    {
        [Tooltip("ID noi bo dung trong code, vd: remove_ads")]
        public IAPProductType type;
        public string id;

        public string googleId;   // SKU tren Google Play
        public string appleId;    // Product ID tren App Store

        [Tooltip("Khoa qua tang game tu dinh nghia, vd: \"Coin\", \"Gem\", \"Gold\", \"RemoveAds\". " +
                 "Dung hang so trong IAPRewardType (sample GameDataIntegration) hoac tu khai bao.")]
        public string rewardKey;
        public int rewardAmount;

        public string GetStoreId()
        {
#if UNITY_IOS
            return string.IsNullOrEmpty(appleId) ? id : appleId;
#else
            return string.IsNullOrEmpty(googleId) ? id : googleId;
#endif
        }
    }
}
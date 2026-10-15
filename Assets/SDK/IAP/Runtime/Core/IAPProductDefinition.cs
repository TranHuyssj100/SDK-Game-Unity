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

        public IAPRewardType rewardType;
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
[Serializable]
public enum IAPRewardType
{
    Coin,
    Gem,
    Item,
    RemoveAds,
    Other
}
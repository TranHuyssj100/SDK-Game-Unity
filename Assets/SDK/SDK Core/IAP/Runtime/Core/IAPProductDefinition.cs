using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.IAP
{
    public enum IAPProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    /// <summary>Mot phan thuong trong goi. Mot san pham co the co nhieu phan.</summary>
    [Serializable]
    public class IAPReward
    {
        [Tooltip("Khoa qua tang, vd: \"Coin\", \"Gem\". Hang so nam trong IAPRewardType (sample GameDataIntegration).")]
        public string rewardKey;
        public int rewardAmount;
    }

    [Serializable]
    public class IAPProductDefinition
    {
        [Tooltip("ID noi bo dung trong code, vd: remove_ads")]

        public IAPProductType type;
        public string id;
        [Space()]
        public string googleId;   // SKU tren Google Play
        public string appleId;    // Product ID tren App Store

        [Tooltip("Bat khi goi nay tat quang cao. Khong dat trong rewards. Chi co hieu luc voi NonConsumable (IsOwned) hoac Subscription con han.")]
        public bool removeAds;

        public List<IAPReward> rewards = new List<IAPReward>();

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
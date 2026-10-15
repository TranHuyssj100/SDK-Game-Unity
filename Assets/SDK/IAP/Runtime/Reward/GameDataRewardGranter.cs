using System;
using LSDK.GameData;
using UnityEngine;

namespace LSDK.IAP
{
    /// <summary>
    /// Granter mac dinh, cap qua vao CurrencyData cua GameData.
    ///   rewardKey "coin" -> CurrencyData.AddCoin(rewardAmount)
    ///   rewardKey "gem"  -> CurrencyData.AddGem(rewardAmount)
    /// Key khac (vd "no_ads", "starter_pack") -> goi onCustomReward neu co.
    /// San pham non-consumable da duoc IAPData danh dau so huu, nen "no_ads" chi can
    /// doc IAPManager.Instance.IsOwned("no_ads") la du.
    /// </summary>
    public class GameDataRewardGranter : IRewardGranter
    {
        readonly Action<IAPProductDefinition> _onCustomReward;

        public GameDataRewardGranter(Action<IAPProductDefinition> onCustomReward = null)
        {
            _onCustomReward = onCustomReward;
        }

        public void Grant(IAPProductDefinition product)
        {
            var gdm = GameDataManager.Instance;

            switch (product.rewardType)
            {
                case IAPRewardType.Coin:
                    gdm.Get<CurrencyData>().AddCoin(product.rewardAmount);
                    break;
                case IAPRewardType.Gem:
                    gdm.Get<CurrencyData>().AddGem(product.rewardAmount);
                    break;
                default:
                    if (_onCustomReward != null)
                    {
                        _onCustomReward(product);
                    }
                    else if (product.rewardType != IAPRewardType.Other)
                    {
                        Debug.LogWarning($"[IAP] Chua xu ly rewardType '{product.rewardType}' cua '{product.id}'.");
                    }
                    break;
            }
        }
    }
}

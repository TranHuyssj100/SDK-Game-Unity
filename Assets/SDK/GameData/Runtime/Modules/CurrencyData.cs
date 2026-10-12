using System;
using UnityEngine;

namespace LSDK.GameData
{
    [Serializable]
    public class CurrencyData : GameDataModule
    {
        [SerializeField] private int coin;
        [SerializeField] private int gem;

        public override string Key => GameDataKeys.Currency;

        public int Coin => coin;
        public int Gem => gem;

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            coin += amount;
            Save();
        }

        public bool SpendCoin(int amount)
        {
            if (amount <= 0 || coin < amount) return false;
            coin -= amount;
            Save();
            return true;
        }

        public void AddGem(int amount)
        {
            if (amount <= 0) return;
            gem += amount;
            Save();
        }

        public bool SpendGem(int amount)
        {
            if (amount <= 0 || gem < amount) return false;
            gem -= amount;
            Save();
            return true;
        }
    }
}

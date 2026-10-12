using System;
using UnityEngine;

namespace LSDK.GameData
{
    [Serializable]
    public class PlayerData : GameDataModule
    {
        [SerializeField] private string playerName;
        [SerializeField] private int level;
        [SerializeField] private int exp;
        [SerializeField] private long firstLaunchUnix;

        public override string Key => GameDataKeys.Player;

        public string PlayerName
        {
            get => playerName;
            set
            {
                if (playerName == value)
                    return;
                playerName = value;
                Save();
            }
        }

        public int Level
        {
            get => level;
            set
            {
                if (level == value)
                    return;
                level = value;
                Save();
            }
        }

        public int Exp => exp;
        public long FirstLaunchUnix => firstLaunchUnix;

        protected override void OnCreated()
        {
            playerName = "Player";
            level = 1;
            exp = 0;
            firstLaunchUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            exp += amount;
            Save();
        }
    }
}

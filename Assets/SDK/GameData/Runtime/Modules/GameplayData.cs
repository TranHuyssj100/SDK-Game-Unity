using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LSDK.GameData
{
    public class GameplayData : GameDataModule
    {
        public override string Key => GameDataKeys.Gameplay;
        [SerializeField] private int currentLevel;
        [SerializeField] private Dictionary<int, int> levelProgress = new Dictionary<int, int>();

        public int CurrentLevel
        {
            get => currentLevel;
            set
            {
                if (currentLevel == value)
                    return;
                currentLevel = value;
                Save();
            }
        }
        public int GetLevelProgress(int level)
        {
            return levelProgress.TryGetValue(level, out int progress) ? progress : 0;
        }
        public void SetLevelProgress(int level, int progress)
        {
            levelProgress[level] = progress;
            Save();
        }
    }
}

using System;
using UnityEngine;

namespace LSDK.GameData
{
    [Serializable]
    public class SettingsData : GameDataModule
    {
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField] private bool vibration = true;
        [SerializeField] private string language = "en";

        public override string Key => GameDataKeys.Settings;

        public float MusicVolume
        {
            get => musicVolume;
            set { value = Mathf.Clamp01(value); if (Mathf.Approximately(musicVolume, value)) return; musicVolume = value; Save(); }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set { value = Mathf.Clamp01(value); if (Mathf.Approximately(sfxVolume, value)) return; sfxVolume = value; Save(); }
        }

        public bool Vibration
        {
            get => vibration;
            set { if (vibration == value) return; vibration = value; Save(); }
        }

        public string Language
        {
            get => language;
            set { if (language == value) return; language = value; Save(); }
        }
    }
}

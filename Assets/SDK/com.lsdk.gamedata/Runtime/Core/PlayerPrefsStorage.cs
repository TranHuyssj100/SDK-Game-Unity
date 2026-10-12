using UnityEngine;

namespace LSDK.GameData
{
    public sealed class PlayerPrefsStorage : IGameDataStorage
    {
        public bool HasKey(string key) { return PlayerPrefs.HasKey(key); }
        public string Read(string key) { return PlayerPrefs.GetString(key, string.Empty); }
        public void Write(string key, string value) { PlayerPrefs.SetString(key, value); }
        public void Delete(string key) { PlayerPrefs.DeleteKey(key); }
        public void Flush() { PlayerPrefs.Save(); }
    }
}

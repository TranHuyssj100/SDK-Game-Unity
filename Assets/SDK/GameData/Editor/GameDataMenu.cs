using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LSDK.GameData.EditorTools
{
    public static class GameDataMenu
    {
        private const string Root = "Tools/LSDK/GameData/";

        [MenuItem(Root + "Open Window", false, 0)]
        private static void OpenWindow()
        {
            GameDataWindow.Open();
        }

        [MenuItem(Root + "Print All JSON", false, 20)]
        private static void PrintAll()
        {
            var storage = new PlayerPrefsStorage();
            var sb = new StringBuilder();

            foreach (var type in GameDataWindow.FindModuleTypes())
            {
                var module = (GameDataModule)Activator.CreateInstance(type);
                sb.AppendLine($"[{type.Name}] {module.Key}");
                sb.AppendLine(storage.HasKey(module.Key) ? storage.Read(module.Key) : "(chưa có data)");
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem(Root + "Clear All Saved Data", false, 40)]
        private static void ClearAll()
        {
            if (!EditorUtility.DisplayDialog("Clear All Saved Data",
                    "Xóa toàn bộ GameData trong PlayerPrefs? Không hoàn tác được.", "Xóa", "Hủy"))
                return;

            var storage = new PlayerPrefsStorage();
            foreach (var type in GameDataWindow.FindModuleTypes())
            {
                var module = (GameDataModule)Activator.CreateInstance(type);
                storage.Delete(module.Key);
                storage.Delete(module.Key + GameDataKeys.CorruptSuffix);
            }
            storage.Flush();

            if (Application.isPlaying && GameDataManager.HasInstance)
                GameDataManager.Instance.ResetAll();

            foreach (var window in Resources.FindObjectsOfTypeAll<GameDataWindow>())
                window.Reload();
        }
    }
}

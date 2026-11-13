// Assets/SDK/SDKCore/GameManager/Runtime/Config/GameManagerConfig.cs
using UnityEngine;

namespace LSDK.GameManager
{
    /// <summary>
    /// Cấu hình tổng của GameManager. Tạo asset qua
    /// Tools → LSDK → GameManager → Create Config.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameManagerConfig",
        menuName  = "LSDK/GameManager/Config",
        order     = 0)]
    public class GameManagerConfig : ScriptableObject
    {
        // ── Scenes ──────────────────────────────────────────────

        [Header("Scenes")]
        [Tooltip("Tên scene Boot (khởi động lần đầu). Có thể để trống nếu GameManager nằm ở scene đầu tiên.")]
        public string bootSceneName = "";

        [Tooltip("Tên scene Main Menu.")]
        public string mainMenuSceneName = "MainMenu";

        [Tooltip("Danh sách scene của từng level, theo thứ tự index.")]
        public string[] levelSceneNames = new string[0];

        // ── Behaviour ───────────────────────────────────────────

        [Header("Behaviour")]
        [Tooltip("Tự động Pause khi app mất focus (thiết bị di động).")]
        public bool autoPauseOnFocusLost = false;

        [Tooltip("Phát event OnLevelStarted ngay khi vào PlayingState.")]
        public bool publishLevelStartedEvent = true;

        // ── Helpers ─────────────────────────────────────────────

        /// <summary>Trả về tên scene của level theo index. Null nếu ngoài phạm vi.</summary>
        public string GetLevelScene(int index)
        {
            if (levelSceneNames == null
                || index < 0
                || index >= levelSceneNames.Length)
                return null;

            return string.IsNullOrWhiteSpace(levelSceneNames[index])
                ? null
                : levelSceneNames[index].Trim();
        }

        /// <summary>Tổng số level đã đăng ký.</summary>
        public int LevelCount => levelSceneNames == null ? 0 : levelSceneNames.Length;
    }
}

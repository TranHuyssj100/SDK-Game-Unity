// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/BootingState.cs
using UnityEngine;
using LSDK.Loading;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>BootingState</b> — Trạng thái khởi động đầu tiên khi game mở.
    /// <para>
    /// Nhiệm vụ: load scene MainMenu qua <see cref="LoadingManager"/>.
    /// Nếu bootSceneName trống (GameManager đã nằm trong scene đầu), chuyển thẳng sang MainMenu.
    /// </para>
    /// </summary>
    public sealed class BootingState : IGameState
    {
        public GameStateType StateType => GameStateType.Booting;

        public void Enter(GameStateContext ctx)
        {
            Debug.Log("[LSDK.GameManager] BootingState: Bắt đầu khởi động...");

            var gm      = GameManager.Instance;
            var config  = gm.Config;
            var menuScene = config != null ? config.mainMenuSceneName : "";

            if (string.IsNullOrWhiteSpace(menuScene))
            {
                // Không có scene nào được cấu hình → chuyển thẳng MainMenu
                Debug.LogWarning("[LSDK.GameManager] BootingState: mainMenuSceneName trống, chuyển thẳng sang MainMenu.");
                gm.GoToMainMenu();
                return;
            }

            if (LoadingManager.Instance == null)
            {
                Debug.LogError("[LSDK.GameManager] BootingState: LoadingManager chưa có trong scene.");
                return;
            }

            LoadingManager.Instance.LoadScene(
                menuScene,
                onCompleted: () => gm.GoToMainMenu(),
                onFailed:    err =>
                {
                    Debug.LogError($"[LSDK.GameManager] BootingState: Load scene thất bại — {err}");
                });
        }

        public void Tick(float deltaTime) { }

        public void Exit()
        {
            Debug.Log("[LSDK.GameManager] BootingState: Xong.");
        }
    }
}

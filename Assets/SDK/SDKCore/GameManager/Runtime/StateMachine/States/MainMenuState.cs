// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/MainMenuState.cs
using UnityEngine;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>MainMenuState</b> — Đang hiển thị màn hình chính.
    /// <para>
    /// Chờ người dùng bấm Play → <see cref="GameManager.StartLevel"/>.
    /// </para>
    /// </summary>
    public sealed class MainMenuState : IGameState
    {
        public GameStateType StateType => GameStateType.MainMenu;

        public void Enter(GameStateContext ctx)
        {
            Debug.Log("[LSDK.GameManager] MainMenuState: Vào Main Menu.");
            // Mở UI MainMenu nếu có: UIManager.Instance.Open<MainMenuPanel>();
        }

        public void Tick(float deltaTime) { }

        public void Exit()
        {
            Debug.Log("[LSDK.GameManager] MainMenuState: Rời Main Menu.");
            // Đóng UI MainMenu nếu cần: UIManager.Instance.Close<MainMenuPanel>();
        }
    }
}

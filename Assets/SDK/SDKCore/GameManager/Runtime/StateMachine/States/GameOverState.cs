// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/GameOverState.cs
using UnityEngine;
using LSDK.Core.Observer;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>GameOverState</b> — Player thua / game over.
    /// <para>
    /// Phát event <see cref="GameEvents.OnGameOver"/> để UI GameOver Popup
    /// và hệ thống reward / ads phản ứng.
    /// Từ đây, game có thể gọi:
    /// <list type="bullet">
    ///   <item><see cref="GameManager.Retry"/> — chơi lại level hiện tại.</item>
    ///   <item><see cref="GameManager.GoToMainMenu"/> — về menu chính.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class GameOverState : IGameState
    {
        public GameStateType StateType => GameStateType.GameOver;

        public void Enter(GameStateContext ctx)
        {
            Debug.Log($"[LSDK.GameManager] GameOverState: Level {ctx.LevelIndex} — Game Over.");
            Observer.Publish(GameEvents.OnGameOver,
                new LevelEventData { LevelIndex = ctx.LevelIndex });
            // Mở GameOver UI: UIManager.Instance.Open<GameOverPopup>();
        }

        public void Tick(float deltaTime) { }

        public void Exit()
        {
            Debug.Log("[LSDK.GameManager] GameOverState: Xong.");
            // Đóng GameOver UI nếu chưa đóng: UIManager.Instance.Close<GameOverPopup>();
        }
    }
}

// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/LevelCompleteState.cs
using UnityEngine;
using LSDK.Core.Observer;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>LevelCompleteState</b> — Player hoàn thành level.
    /// <para>
    /// Phát event <see cref="GameEvents.OnLevelCompleted"/> để UI Win Popup
    /// và hệ thống reward phản ứng.
    /// Từ đây, game có thể gọi:
    /// <list type="bullet">
    ///   <item><see cref="GameManager.NextLevel"/> — chuyển level tiếp theo.</item>
    ///   <item><see cref="GameManager.GoToMainMenu"/> — về menu chính.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class LevelCompleteState : IGameState
    {
        public GameStateType StateType => GameStateType.LevelComplete;

        public void Enter(GameStateContext ctx)
        {
            Debug.Log($"[LSDK.GameManager] LevelCompleteState: Level {ctx.LevelIndex} hoàn thành!");
            Observer.Publish(GameEvents.OnLevelCompleted,
                new LevelEventData { LevelIndex = ctx.LevelIndex });
            // Mở Win UI: UIManager.Instance.Open<LevelCompletePopup>();
        }

        public void Tick(float deltaTime) { }

        public void Exit()
        {
            Debug.Log("[LSDK.GameManager] LevelCompleteState: Xong.");
            // Đóng Win UI nếu chưa đóng: UIManager.Instance.Close<LevelCompletePopup>();
        }
    }
}

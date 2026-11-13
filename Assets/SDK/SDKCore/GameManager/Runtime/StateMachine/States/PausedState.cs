// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/PausedState.cs
using UnityEngine;
using LSDK.Core.Observer;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>PausedState</b> — Game tạm dừng.
    /// <para>
    /// Đặt <c>Time.timeScale = 0</c> khi vào và khôi phục khi thoát.
    /// Phát event <see cref="GameEvents.OnGamePaused"/> / <see cref="GameEvents.OnGameResumed"/>
    /// để UI Pause Panel và âm thanh phản ứng.
    /// </para>
    /// </summary>
    public sealed class PausedState : IGameState
    {
        public GameStateType StateType => GameStateType.Paused;

        public void Enter(GameStateContext ctx)
        {
            Time.timeScale = 0f;
            Debug.Log("[LSDK.GameManager] PausedState: Game đã Pause.");
            Observer.Publish(GameEvents.OnGamePaused);
            // Mở Pause UI: UIManager.Instance.Open<PausePopup>();
        }

        public void Tick(float deltaTime)
        {
            // deltaTime = 0 vì timeScale = 0.
            // Có thể kiểm tra input mở/đóng pause ở đây nếu muốn.
        }

        public void Exit()
        {
            Time.timeScale = 1f;
            Debug.Log("[LSDK.GameManager] PausedState: Game đã Resume.");
            Observer.Publish(GameEvents.OnGameResumed);
            // Đóng Pause UI: UIManager.Instance.Close<PausePopup>();
        }
    }
}

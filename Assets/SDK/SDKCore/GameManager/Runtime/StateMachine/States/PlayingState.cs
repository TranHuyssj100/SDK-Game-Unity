// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/PlayingState.cs
using System;
using UnityEngine;
using LSDK.Core.Observer;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>PlayingState</b> — Gameplay đang chạy.
    /// <para>
    /// Lắng nghe event từ gameplay (không cần gameplay biết đến GameManager):
    /// <list type="bullet">
    ///   <item><see cref="GameEvents.RequestGameOver"/>      → chuyển GameOver.</item>
    ///   <item><see cref="GameEvents.RequestLevelComplete"/>  → chuyển LevelComplete.</item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class PlayingState : IGameState
    {
        public GameStateType StateType => GameStateType.Playing;

        // Cache Action instance để Subscribe / Unsubscribe đúng cặp
        private readonly Action _onRequestGameOver;
        private readonly Action _onRequestLevelComplete;

        public PlayingState()
        {
            _onRequestGameOver      = HandleRequestGameOver;
            _onRequestLevelComplete = HandleRequestLevelComplete;
        }

        public void Enter(GameStateContext ctx)
        {
            Time.timeScale = 1f;
            Debug.Log($"[LSDK.GameManager] PlayingState: Level {ctx.LevelIndex} bắt đầu (retry={ctx.IsRetry}).");

            // Đăng ký lắng nghe request từ gameplay
            Observer.Subscribe(GameEvents.RequestGameOver,      _onRequestGameOver);
            Observer.Subscribe(GameEvents.RequestLevelComplete, _onRequestLevelComplete);

            // Phát event để HUD, Analytics v.v. biết level bắt đầu
            var config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null || config.publishLevelStartedEvent)
            {
                Observer.Publish(GameEvents.OnLevelStarted,
                    new LevelEventData { LevelIndex = ctx.LevelIndex });
            }
        }

        public void Tick(float deltaTime)
        {
            // Mở rộng: kiểm tra input, đếm thời gian chơi, v.v.
        }

        public void Exit()
        {
            // Hủy đăng ký trước khi rời state để không nhận event thừa
            Observer.Unsubscribe(GameEvents.RequestGameOver,      _onRequestGameOver);
            Observer.Unsubscribe(GameEvents.RequestLevelComplete, _onRequestLevelComplete);
            Debug.Log("[LSDK.GameManager] PlayingState: Rời gameplay.");
        }

        // ── Handlers ────────────────────────────────────────────

        private void HandleRequestGameOver()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }

        private void HandleRequestLevelComplete()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerLevelComplete();
        }
    }
}

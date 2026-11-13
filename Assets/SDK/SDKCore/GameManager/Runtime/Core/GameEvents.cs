// Assets/SDK/SDKCore/GameManager/Runtime/Core/GameEvents.cs
namespace LSDK.GameManager
{
    /// <summary>
    /// Hằng số tên event dùng với <c>LSDK.Core.Observer.Observer.Publish/Subscribe</c>.
    /// Luôn dùng những hằng số này thay vì magic string để tránh typo.
    /// </summary>
    public static class GameEvents
    {
        // ── State Machine ────────────────────────────────────────

        /// <summary>
        /// Phát khi GameStateMachine chuyển state.
        /// Data: <see cref="GameStateChangedData"/>
        /// </summary>
        public const string OnGameStateChanged  = "LSDK.Game.OnStateChanged";

        // ── Gameplay Flow ────────────────────────────────────────

        /// <summary>
        /// Phát khi PlayingState.Enter() được gọi.
        /// Data: <see cref="LevelEventData"/>
        /// </summary>
        public const string OnLevelStarted      = "LSDK.Game.OnLevelStarted";

        /// <summary>
        /// Phát khi TriggerLevelComplete() được gọi.
        /// Data: <see cref="LevelEventData"/>
        /// </summary>
        public const string OnLevelCompleted    = "LSDK.Game.OnLevelCompleted";

        /// <summary>
        /// Phát khi TriggerGameOver() được gọi.
        /// Data: <see cref="LevelEventData"/>
        /// </summary>
        public const string OnGameOver          = "LSDK.Game.OnGameOver";

        // ── Pause / Resume ───────────────────────────────────────

        /// <summary>Phát khi vào PausedState. Không có data kèm.</summary>
        public const string OnGamePaused        = "LSDK.Game.OnPaused";

        /// <summary>Phát khi thoát PausedState. Không có data kèm.</summary>
        public const string OnGameResumed       = "LSDK.Game.OnResumed";

        // ── Gameplay input events (Gameplay gửi lên GameManager) ─

        /// <summary>
        /// Gameplay gửi khi player thua (hết mạng, thua v.v.).
        /// GameManager lắng nghe để chuyển sang GameOverState.
        /// Không có data kèm.
        /// </summary>
        public const string RequestGameOver     = "LSDK.Game.RequestGameOver";

        /// <summary>
        /// Gameplay gửi khi player hoàn thành level.
        /// GameManager lắng nghe để chuyển sang LevelCompleteState.
        /// Không có data kèm.
        /// </summary>
        public const string RequestLevelComplete = "LSDK.Game.RequestLevelComplete";
    }

    // ── Event data structs ───────────────────────────────────────

    /// <summary>Data đi kèm <see cref="GameEvents.OnGameStateChanged"/>.</summary>
    public struct GameStateChangedData
    {
        public GameStateType From;
        public GameStateType To;
    }

    /// <summary>Data đi kèm các event liên quan level.</summary>
    public struct LevelEventData
    {
        public int LevelIndex;
    }
}

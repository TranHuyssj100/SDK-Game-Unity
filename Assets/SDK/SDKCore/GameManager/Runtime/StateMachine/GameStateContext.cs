// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/GameStateContext.cs
namespace LSDK.GameManager
{
    /// <summary>
    /// Dữ liệu ngữ cảnh truyền vào State khi <c>Enter()</c> được gọi.
    /// Mở rộng thêm field khi cần (difficulty, score, v.v.).
    /// </summary>
    public class GameStateContext
    {
        /// <summary>Index của level hiện tại (0-based).</summary>
        public int LevelIndex;

        /// <summary>True nếu đây là lần retry (không phải lần chơi mới).</summary>
        public bool IsRetry;
    }
}

// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/IGameState.cs
namespace LSDK.GameManager
{
    /// <summary>
    /// Interface chung cho tất cả Game State.
    /// <list type="bullet">
    ///   <item><see cref="Enter"/> — gọi một lần khi state được kích hoạt.</item>
    ///   <item><see cref="Tick"/>  — gọi mỗi frame (Update) trong khi state đang active.</item>
    ///   <item><see cref="Exit"/>  — gọi một lần ngay trước khi state bị thay thế.</item>
    /// </list>
    /// </summary>
    public interface IGameState
    {
        /// <summary>Loại state này.</summary>
        GameStateType StateType { get; }

        /// <summary>
        /// Khởi tạo state với context đầu vào.
        /// Luôn được gọi sau <see cref="Exit"/> của state trước.
        /// </summary>
        void Enter(GameStateContext ctx);

        /// <summary>
        /// Cập nhật mỗi frame (deltaTime tính theo <c>Time.deltaTime</c>).
        /// Không được gọi khi state không active.
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Dọn dẹp trước khi rời state.
        /// Luôn được gọi trước <see cref="Enter"/> của state kế tiếp.
        /// </summary>
        void Exit();
    }
}

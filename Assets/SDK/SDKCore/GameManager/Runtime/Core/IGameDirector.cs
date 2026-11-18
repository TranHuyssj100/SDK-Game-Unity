// Assets/SDK/SDKCore/GameManager/Runtime/Core/IGameDirector.cs
namespace LSDK.GameManager
{
    /// <summary>
    /// Mặt nạ không generic của manager đang chạy.
    /// State trong SDK gọi qua interface này để class con vẫn nhận callback.
    /// </summary>
    public interface IGameDirector
    {
        /// <summary>Config của manager đang chạy.</summary>
        GameManagerConfig Config { get; }

        /// <summary>Chuyển về Main Menu.</summary>
        void GoToMainMenu();

        /// <summary>Scene level đã load xong, bắt đầu chơi.</summary>
        void BeginPlay();

        /// <summary>Player thua.</summary>
        void TriggerGameOver();

        /// <summary>Player hoàn thành level.</summary>
        void TriggerLevelComplete();
    }
}

// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/BootingState.cs
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
            DebugUtil.Info("BootingState: Bắt đầu khởi động...");

            var gm = GameDirector.Current;
            if (gm == null)
            {
                DebugUtil.Error("BootingState: chưa có GameManager.");
                return;
            }

            var config = gm.Config;

            var menuScene = config != null ? config.mainMenuSceneName : "";

            if (string.IsNullOrWhiteSpace(menuScene))
            {
                // Không có scene nào được cấu hình → chuyển thẳng MainMenu
                DebugUtil.Warning("BootingState: mainMenuSceneName trống, chuyển thẳng sang MainMenu.");
                gm.GoToMainMenu();
                return;
            }

            if (LoadingManager.Instance == null)
            {
                DebugUtil.Error("BootingState: LoadingManager chưa có trong scene.");
                return;
            }

            LoadingManager.Instance.LoadScene(
                menuScene,
                onCompleted: () => gm.GoToMainMenu(),
                onFailed:    err =>
                {
                    DebugUtil.Error("BootingState: Load scene thất bại — " + err);
                });
        }

        public void Tick(float deltaTime) { }

        public void Exit()
        {
            DebugUtil.Info("BootingState: Xong.");
        }
    }
}

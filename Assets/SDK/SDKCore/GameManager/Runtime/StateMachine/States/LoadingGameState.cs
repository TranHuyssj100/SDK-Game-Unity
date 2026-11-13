// Assets/SDK/SDKCore/GameManager/Runtime/StateMachine/States/LoadingGameState.cs
using UnityEngine;
using LSDK.Loading;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>LoadingGameState</b> — Đang tải scene level.
    /// <para>
    /// Dùng <see cref="LoadingManager"/> để load scene theo <c>LevelIndex</c> từ config.
    /// Sau khi load xong → gọi <see cref="GameManager.BeginPlay"/>.
    /// Nếu thất bại → quay về <see cref="GameManager.GoToMainMenu"/>.
    /// </para>
    /// </summary>
    public sealed class LoadingGameState : IGameState
    {
        public GameStateType StateType => GameStateType.LoadingGame;

        public void Enter(GameStateContext ctx)
        {
            var gm     = GameManager.Instance;
            var config = gm.Config;

            string sceneName = config != null
                ? config.GetLevelScene(ctx.LevelIndex)
                : null;

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[LSDK.GameManager] LoadingGameState: Không tìm thấy scene cho level {ctx.LevelIndex}. Kiểm tra GameManagerConfig.");
                gm.GoToMainMenu();
                return;
            }

            if (LoadingManager.Instance == null)
            {
                Debug.LogError("[LSDK.GameManager] LoadingGameState: LoadingManager chưa có trong scene.");
                gm.GoToMainMenu();
                return;
            }

            Debug.Log($"[LSDK.GameManager] LoadingGameState: Đang load '{sceneName}' (level {ctx.LevelIndex})...");

            LoadingManager.Instance.LoadScene(
                sceneName,
                onCompleted: () => gm.BeginPlay(),
                onFailed:    err =>
                {
                    Debug.LogError($"[LSDK.GameManager] LoadingGameState: Lỗi load scene — {err}");
                    gm.GoToMainMenu();
                });
        }

        public void Tick(float deltaTime) { }

        public void Exit()
        {
            Debug.Log("[LSDK.GameManager] LoadingGameState: Xong.");
        }
    }
}

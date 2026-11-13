// Assets/SDK/SDKCore/GameManager/Runtime/Core/GameManager.cs
using UnityEngine;
using LSDK.Core.Base;

namespace LSDK.GameManager
{
    /// <summary>
    /// <b>GameManager</b> — Singleton tổng điều phối vòng đời game.
    /// <para>
    /// Trách nhiệm:
    /// <list type="number">
    ///   <item>Khởi tạo <see cref="GameStateMachine"/> và đăng ký tất cả state mặc định.</item>
    ///   <item>Cung cấp Public API để các class khác kích hoạt chuyển state.</item>
    ///   <item>Forward <c>Update</c> → <see cref="GameStateMachine.Tick"/> mỗi frame.</item>
    ///   <item>Lắng nghe application focus/pause nếu cấu hình yêu cầu.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Cách dùng từ gameplay:</b>
    /// <code>
    /// // Bắt đầu level 0
    /// GameManager.Instance.StartLevel(0);
    ///
    /// // Gameplay báo thua (không cần gọi thẳng GameManager)
    /// Observer.Publish(GameEvents.RequestGameOver);
    /// </code>
    /// </para>
    /// </summary>
    [AddComponentMenu("LSDK/Game Manager")]
    [DisallowMultipleComponent]
    public class GameManager : SingletonBehaviour<GameManager>
    {
        // ── Inspector ───────────────────────────────────────────

        [Header("Config")]
        [Tooltip("ScriptableObject chứa cấu hình (scene names, v.v.). Tạo qua LSDK/GameManager/Config.")]
        [SerializeField] private GameManagerConfig _config;

        // ── State Machine ───────────────────────────────────────

        private GameStateMachine _sm;

        // ── Runtime data ────────────────────────────────────────

        private int _currentLevelIndex;

        // ── Properties ──────────────────────────────────────────

        /// <summary>Config hiện tại của GameManager.</summary>
        public GameManagerConfig Config => _config;

        /// <summary>State đang active.</summary>
        public GameStateType CurrentState => _sm?.CurrentStateType ?? GameStateType.None;

        /// <summary>Index level đang chơi (0-based).</summary>
        public int CurrentLevelIndex => _currentLevelIndex;

        // ══════════════════════════════════════════════════════
        //  Khởi tạo
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Gọi bởi <see cref="SingletonBehaviour{T}.Awake"/> sau khi singleton được xác nhận.
        /// </summary>
        protected override void OnInitialize()
        {
            if (_config == null)
                Debug.LogWarning("[LSDK.GameManager] GameManagerConfig chưa gán. Một số tính năng có thể không hoạt động.");

            BuildStateMachine();
        }

        private void BuildStateMachine()
        {
            _sm = new GameStateMachine();

            // Đăng ký tất cả state mặc định
            _sm.Register(new BootingState());
            _sm.Register(new MainMenuState());
            _sm.Register(new LoadingGameState());
            _sm.Register(new PlayingState());
            _sm.Register(new PausedState());
            _sm.Register(new LevelCompleteState());
            _sm.Register(new GameOverState());

            // Hook để game project thêm custom state mà không sửa SDK
            RegisterExtraStates(_sm);

            // Log khi state thay đổi (hữu ích khi debug)
            _sm.OnStateChanged += (from, to) =>
                Debug.Log($"[LSDK.GameManager] Transition: {from} → {to}");
        }

        /// <summary>
        /// Override method này trong subclass để đăng ký thêm custom state.
        /// <para>Không gọi <c>base.RegisterExtraStates()</c> nếu không cần.</para>
        /// <code>
        /// // Trong MyGameManager.cs (kế thừa GameManager):
        /// protected override void RegisterExtraStates(GameStateMachine sm)
        /// {
        ///     sm.Register(new ShopState());
        ///     sm.Register(new TutorialState());
        /// }
        /// </code>
        /// </summary>
        protected virtual void RegisterExtraStates(GameStateMachine sm) { }

        // ══════════════════════════════════════════════════════
        //  Unity lifecycle
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Vào Booting ở Start, sau khi mọi Awake đã chạy.
        /// LoadingManager chỉ gán Instance trong Awake — nếu Booting chạy trong
        /// OnInitialize thì Instance còn null dù component đã có trên scene.
        /// </summary>
        private void Start()
        {
            if (_sm == null || _sm.CurrentStateType != GameStateType.None) return;
            _sm.ChangeState(GameStateType.Booting);
        }

        private void Update()
        {
            _sm?.Tick(Time.deltaTime);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (_config == null || !_config.autoPauseOnFocusLost) return;
            if (!hasFocus && CurrentState == GameStateType.Playing)
                Pause();
        }

        // ══════════════════════════════════════════════════════
        //  Public API
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Chuyển sang <see cref="GameStateType.MainMenu"/>.
        /// Gọi từ BootingState sau khi load xong, hoặc từ bất kỳ state nào muốn về menu.
        /// </summary>
        public void GoToMainMenu()
        {
            _sm.ChangeState(GameStateType.MainMenu);
        }

        /// <summary>
        /// Bắt đầu load và chơi level tại <paramref name="levelIndex"/>.
        /// </summary>
        /// <param name="levelIndex">Index 0-based trong <see cref="GameManagerConfig.levelSceneNames"/>.</param>
        public void StartLevel(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
            _sm.ChangeState(GameStateType.LoadingGame,
                new GameStateContext { LevelIndex = levelIndex });
        }

        /// <summary>
        /// Gọi bởi <see cref="LoadingGameState"/> sau khi scene load xong.
        /// Chuyển sang <see cref="GameStateType.Playing"/>.
        /// </summary>
        public void BeginPlay()
        {
            _sm.ChangeState(GameStateType.Playing,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Tạm dừng gameplay. Chỉ hoạt động khi đang ở <see cref="GameStateType.Playing"/>.
        /// </summary>
        public void Pause()
        {
            if (CurrentState != GameStateType.Playing)
            {
                Debug.LogWarning("[LSDK.GameManager] Pause() bị bỏ qua: không đang ở Playing state.");
                return;
            }
            _sm.ChangeState(GameStateType.Paused,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Tiếp tục gameplay. Chỉ hoạt động khi đang ở <see cref="GameStateType.Paused"/>.
        /// </summary>
        public void Resume()
        {
            if (CurrentState != GameStateType.Paused)
            {
                Debug.LogWarning("[LSDK.GameManager] Resume() bị bỏ qua: không đang ở Paused state.");
                return;
            }
            _sm.ChangeState(GameStateType.Playing,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Player hoàn thành level. Chuyển sang <see cref="GameStateType.LevelComplete"/>.
        /// Chỉ hoạt động khi đang ở <see cref="GameStateType.Playing"/>.
        /// </summary>
        public void TriggerLevelComplete()
        {
            if (CurrentState != GameStateType.Playing)
            {
                Debug.LogWarning("[LSDK.GameManager] TriggerLevelComplete() bị bỏ qua: không đang ở Playing state.");
                return;
            }
            _sm.ChangeState(GameStateType.LevelComplete,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Player thua. Chuyển sang <see cref="GameStateType.GameOver"/>.
        /// Chỉ hoạt động khi đang ở <see cref="GameStateType.Playing"/>.
        /// </summary>
        public void TriggerGameOver()
        {
            if (CurrentState != GameStateType.Playing)
            {
                Debug.LogWarning("[LSDK.GameManager] TriggerGameOver() bị bỏ qua: không đang ở Playing state.");
                return;
            }
            _sm.ChangeState(GameStateType.GameOver,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Đăng ký thêm một custom state vào state machine.
        /// Có thể gọi sau <c>Awake</c> (trước khi <c>ChangeStateTo</c> state đó).
        /// <para>
        /// Dùng khi không muốn subclass GameManager:
        /// <code>
        /// void Start()
        /// {
        ///     GameManager.Instance.RegisterState(new ShopState());
        /// }
        /// </code>
        /// </para>
        /// </summary>
        public void RegisterState(IGameState state)
        {
            if (state == null)
            {
                Debug.LogError("[LSDK.GameManager] RegisterState: state is null.");
                return;
            }
            _sm.Register(state);
        }

        /// <summary>
        /// Chuyển đến bất kỳ state nào đã đăng ký — dùng cho custom state của từng game.
        /// <para>
        /// Khai báo custom state một lần duy nhất (ví dụ trong file <c>CustomGameStates.cs</c>):
        /// <code>
        /// public static class CustomGameStates
        /// {
        ///     public static readonly GameStateType Shop     = GameStateType.Register("Shop");
        ///     public static readonly GameStateType Tutorial = GameStateType.Register("Tutorial");
        /// }
        /// </code>
        /// Đăng ký state vào GameManager (override <see cref="RegisterExtraStates"/>):
        /// <code>
        /// protected override void RegisterExtraStates(GameStateMachine sm)
        /// {
        ///     sm.Register(new ShopState());
        ///     sm.Register(new TutorialState());
        /// }
        /// </code>
        /// Sau đó gọi:
        /// <code>
        /// GameManager.Instance.ChangeStateTo(CustomGameStates.Shop);
        /// </code>
        /// </para>
        /// </summary>
        public void ChangeStateTo(GameStateType stateType, GameStateContext ctx = null)
        {
            if (stateType == null)
            {
                UnityEngine.Debug.LogError("[LSDK.GameManager] ChangeStateTo: stateType is null.");
                return;
            }
            _sm.ChangeState(stateType, ctx);
        }

        /// <summary>
        /// Chơi lại level hiện tại (retry).
        /// </summary>
        public void Retry()
        {
            _sm.ChangeState(GameStateType.LoadingGame,
                new GameStateContext { LevelIndex = _currentLevelIndex, IsRetry = true });
        }

        /// <summary>
        /// Chuyển sang level tiếp theo.
        /// Nếu đã hết level → về <see cref="GameStateType.MainMenu"/>.
        /// </summary>
        public void NextLevel()
        {
            int next = _currentLevelIndex + 1;
            int total = _config != null ? _config.LevelCount : 0;

            if (next >= total)
            {
                Debug.Log("[LSDK.GameManager] Đã hết level, quay về Main Menu.");
                GoToMainMenu();
                return;
            }

            StartLevel(next);
        }
    }
}

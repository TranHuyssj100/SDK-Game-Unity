// Assets/SDK/SDKCore/GameManager/Runtime/Core/GameManager.cs
using LSDK.Core.Base;
using UnityEngine;

namespace LSDK.GameManager
{
    /// <summary>
    /// GameManager mở để kế thừa. Class con tự là singleton đúng kiểu, không cần ép.
    /// <code>
    /// public class GameController : GameManager&lt;GameController&gt;
    /// {
    ///     public override void StartLevel(int levelIndex)
    ///     {
    ///         // mở popup, rồi mới chạy flow gốc
    ///         base.StartLevel(levelIndex);
    ///     }
    /// }
    ///
    /// GameController.Instance.StartLevel(0);
    /// </code>
    /// GameObject gốc (không có parent) được <see cref="SingletonBehaviour{T}"/> đánh DontDestroyOnLoad.
    /// Override <c>Awake</c> thì phải gọi <c>base.Awake()</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager<T> : SingletonBehaviour<T>, IGameDirector
        where T : GameManager<T>
    {
        // ── Inspector ───────────────────────────────────────────

        [Header("Config")]
        [Tooltip("ScriptableObject chứa cấu hình (scene names, v.v.). Tạo qua LSDK/GameManager/Config.")]
        [SerializeField] private GameManagerConfig _config;

        [Header("Debug")]
        [Tooltip("State đang chạy. Cập nhật khi Play, chỉ để theo dõi.")]
        [ReadOnly, SerializeField] private string _currentState = "None";

        // ── State Machine ───────────────────────────────────────

        /// <summary>State machine của manager này. Subclass dùng khi override flow.</summary>
        protected GameStateMachine _sm;

        // ── Runtime data ────────────────────────────────────────

        /// <summary>Index level đang chơi. Subclass gán khi tự điều khiển flow.</summary>
        protected int _currentLevelIndex;

        // ── Properties ──────────────────────────────────────────

        /// <summary>Config hiện tại của GameManager.</summary>
        public virtual GameManagerConfig Config => _config;

        /// <summary>State đang active.</summary>
        public GameStateType CurrentState => _sm?.CurrentStateType ?? GameStateType.None;

        /// <summary>Index level đang chơi (0-based).</summary>
        public int CurrentLevelIndex => _currentLevelIndex;

        // ══════════════════════════════════════════════════════
        //  Khởi tạo
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Gọi bởi <see cref="SingletonBehaviour{T}.Awake"/> sau khi singleton được xác nhận.
        /// Override thì gọi <c>base.OnInitialize()</c> để giữ đăng ký <see cref="GameDirector"/> và state mặc định.
        /// </summary>
        protected override void OnInitialize()
        {
            if (GameDirector.Current != null && !ReferenceEquals(GameDirector.Current, this))
                DebugUtil.Warning("Đã có manager khác đang chạy. Chỉ giữ một GameManager trên scene.");

            GameDirector.Current = this;

            if (_config == null)
                DebugUtil.Warning("GameManagerConfig chưa gán. Một số tính năng có thể không hoạt động.");

            BuildStateMachine();
        }

        /// <summary>
        /// Tạo state machine và đăng ký state mặc định.
        /// Override khi muốn thay bộ state, nhớ gọi <c>base.BuildStateMachine()</c> nếu vẫn dùng state có sẵn.
        /// </summary>
        protected virtual void BuildStateMachine()
        {
            _sm = new GameStateMachine();

            _sm.Register(new BootingState());
            _sm.Register(new MainMenuState());
            _sm.Register(new LoadingGameState());
            _sm.Register(new PlayingState());
            _sm.Register(new PausedState());
            _sm.Register(new LevelCompleteState());
            _sm.Register(new GameOverState());

            RegisterExtraStates(_sm);

            _sm.OnStateChanged += OnStateChanged;
            SyncCurrentStateLabel();
        }

        private void OnStateChanged(GameStateType from, GameStateType to)
        {
            SyncCurrentStateLabel();
            DebugUtil.Info("Transition: " + from + " → " + to);
        }

        private void SyncCurrentStateLabel()
        {
            var state = CurrentState;
            _currentState = state != null ? state.Name : "None";
        }

        /// <summary>
        /// Override để đăng ký thêm custom state.
        /// <code>
        /// protected override void RegisterExtraStates(GameStateMachine sm)
        /// {
        ///     sm.Register(new ShopState());
        /// }
        /// </code>
        /// </summary>
        protected virtual void RegisterExtraStates(GameStateMachine sm) { }

        // ══════════════════════════════════════════════════════
        //  Unity lifecycle
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Giữ singleton và DontDestroyOnLoad từ <see cref="SingletonBehaviour{T}"/>.
        /// Override thì gọi <c>base.Awake()</c>.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Vào Booting sau khi mọi Awake đã chạy, để <c>LoadingManager.Instance</c> đã được gán.
        /// </summary>
        protected virtual void Start()
        {
            if (_sm == null || _sm.CurrentStateType != GameStateType.None) return;
            _sm.ChangeState(GameStateType.Booting);
        }

        /// <summary>Đẩy deltaTime xuống state đang active.</summary>
        protected virtual void Update()
        {
            _sm?.Tick(Time.deltaTime);
        }

        /// <summary>Tự pause khi mất focus nếu config bật <c>autoPauseOnFocusLost</c>.</summary>
        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (_config == null || !_config.autoPauseOnFocusLost) return;
            if (!hasFocus && CurrentState == GameStateType.Playing)
                Pause();
        }

        /// <summary>Gỡ khỏi <see cref="GameDirector"/> khi object bị hủy.</summary>
        protected override void OnDestroy()
        {
            if (GameDirector.Current == this)
                GameDirector.Current = null;
            base.OnDestroy();
        }

        // ══════════════════════════════════════════════════════
        //  Public API
        // ══════════════════════════════════════════════════════

        /// <summary>
        /// Chuyển sang <see cref="GameStateType.MainMenu"/>.
        /// </summary>
        public virtual void GoToMainMenu()
        {
            _sm.ChangeState(GameStateType.MainMenu);
        }

        /// <summary>
        /// Bắt đầu load và chơi level tại <paramref name="levelIndex"/>.
        /// </summary>
        public virtual void StartLevel(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
            _sm.ChangeState(GameStateType.LoadingGame,
                new GameStateContext { LevelIndex = levelIndex });
        }

        /// <summary>
        /// Scene đã load xong. Chuyển sang <see cref="GameStateType.Playing"/>.
        /// </summary>
        public virtual void BeginPlay()
        {
            _sm.ChangeState(GameStateType.Playing,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Tạm dừng gameplay. Chỉ chạy khi đang ở <see cref="GameStateType.Playing"/>.
        /// </summary>
        public virtual void Pause()
        {
            if (CurrentState != GameStateType.Playing)
            {
                DebugUtil.Warning("Pause() bị bỏ qua: không đang ở Playing state.");
                return;
            }
            _sm.ChangeState(GameStateType.Paused,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Tiếp tục gameplay. Chỉ chạy khi đang ở <see cref="GameStateType.Paused"/>.
        /// </summary>
        public virtual void Resume()
        {
            if (CurrentState != GameStateType.Paused)
            {
                DebugUtil.Warning("Resume() bị bỏ qua: không đang ở Paused state.");
                return;
            }
            _sm.ChangeState(GameStateType.Playing,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Player hoàn thành level. Chỉ chạy khi đang ở <see cref="GameStateType.Playing"/>.
        /// </summary>
        public virtual void TriggerLevelComplete()
        {
            if (CurrentState != GameStateType.Playing)
            {
                DebugUtil.Warning("TriggerLevelComplete() bị bỏ qua: không đang ở Playing state.");
                return;
            }
            _sm.ChangeState(GameStateType.LevelComplete,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>
        /// Player thua. Chỉ chạy khi đang ở <see cref="GameStateType.Playing"/>.
        /// </summary>
        public virtual void TriggerGameOver()
        {
            if (CurrentState != GameStateType.Playing)
            {
                DebugUtil.Warning("TriggerGameOver() bị bỏ qua: không đang ở Playing state.");
                return;
            }
            _sm.ChangeState(GameStateType.GameOver,
                new GameStateContext { LevelIndex = _currentLevelIndex });
        }

        /// <summary>Đăng ký thêm một state vào state machine.</summary>
        public virtual void RegisterState(IGameState state)
        {
            if (state == null)
            {
                DebugUtil.Error("RegisterState: state is null.");
                return;
            }
            _sm.Register(state);
        }

        /// <summary>Chuyển đến state đã đăng ký, kể cả state tự thêm của từng game.</summary>
        public virtual void ChangeStateTo(GameStateType stateType, GameStateContext ctx = null)
        {
            if (stateType == null)
            {
                DebugUtil.Error("ChangeStateTo: stateType is null.");
                return;
            }
            _sm.ChangeState(stateType, ctx);
        }

        /// <summary>Chơi lại level hiện tại.</summary>
        public virtual void Retry()
        {
            _sm.ChangeState(GameStateType.LoadingGame,
                new GameStateContext { LevelIndex = _currentLevelIndex, IsRetry = true });
        }

        /// <summary>
        /// Chuyển sang level tiếp theo.
        /// Hết level thì về Main Menu.
        /// </summary>
        public virtual void NextLevel()
        {
            int next = _currentLevelIndex + 1;
            int total = _config != null ? _config.LevelCount : 0;

            if (next >= total)
            {
                DebugUtil.Info("Đã hết level, quay về Main Menu.");
                GoToMainMenu();
                return;
            }

            StartLevel(next);
        }
    }

    /// <summary>
    /// GameManager mặc định, gắn thẳng lên scene khi game không cần subclass.
    /// </summary>
    [AddComponentMenu("LSDK/Game Manager")]
    public class GameManager : GameManager<GameManager>
    {
    }
}

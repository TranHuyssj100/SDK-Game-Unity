using System;
using System.Collections;
using System.Collections.Generic;
using LSDK.UISystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LSDK.Loading
{
    /// <summary>
    /// Một lượt load tại một thời điểm. Mở LoadingPanel trên UIRoot (DontDestroyOnLoad),
    /// chạy task và scene phía sau màn che, rồi mới đóng panel.
    /// Đặt component này dưới SDKCoreRoot. Nếu object không có cha thì tự DontDestroyOnLoad.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("LSDK/Loading Manager")]
    public class LoadingManager : MonoBehaviour
    {
        public static LoadingManager Instance { get; private set; }

        [SerializeField] LoadingConfig config;
        [SerializeField] bool blockInput = true;

        readonly LoadingProgress _progress = new LoadingProgress();
        Request _queued;

        LoadingSession _session;
        LoadingPanel _panel;
        LoadingConfig _activeConfig;
        SceneLoadTask _sceneTask;
        Action<string> _currentFailed;

        bool _busy;
        bool _blocked;
        bool _abort;
        bool _opened;
        bool _panelClosed;
        bool _dismissing;
        int _token;
        float _coveredAt;
        float _doneWeight;

        public LoadingPhase Phase { get; private set; }
        public float Progress { get; private set; }
        public bool IsBusy => _busy || Phase == LoadingPhase.Failed;

        public event Action<LoadingPhase> PhaseChanged;
        public event Action<float> ProgressChanged;
        public event Action Completed;
        public event Action<string> Failed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            ReleaseSuspendedScene();
            if (_blocked && UIManager.Instance != null) UIManager.Instance.Block(false);
            _blocked = false;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Phase != LoadingPhase.Working && Phase != LoadingPhase.Holding) return;
            float shown = _progress.Tick(SmoothSpeed, Time.unscaledDeltaTime);
            if (Mathf.Approximately(shown, Progress)) return;
            Publish(shown);
        }

        public void LoadScene(string sceneName, Action onCompleted = null, Action<string> onFailed = null)
            => LoadScene(sceneName, LoadSceneMode.Single, onCompleted, onFailed);

        public void LoadScene(string sceneName, LoadSceneMode mode, Action onCompleted = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Reject("Thiếu tên scene.", onFailed);
                return;
            }

            Run(new LoadingPlan
            {
                SceneName = sceneName.Trim(),
                Mode = mode,
                CloseOpenScreens = mode == LoadSceneMode.Single
            }, onCompleted, onFailed);
        }

        public void LoadScene(string sceneName, IEnumerable<ILoadTask> warmup, Action onCompleted = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Reject("Thiếu tên scene.", onFailed);
                return;
            }

            var plan = new LoadingPlan { SceneName = sceneName.Trim() };
            if (warmup != null) plan.AfterActivate.AddRange(warmup);
            Run(plan, onCompleted, onFailed);
        }

        public void Load(IEnumerable<ILoadTask> tasks, Action onCompleted = null, Action<string> onFailed = null)
        {
            if (tasks == null)
            {
                Reject("Danh sách task rỗng.", onFailed);
                return;
            }

            var plan = new LoadingPlan { CloseOpenScreens = false };
            plan.BeforeActivate.AddRange(tasks);
            Run(plan, onCompleted, onFailed);
        }

        public void Run(LoadingPlan plan, Action onCompleted = null, Action<string> onFailed = null)
        {
            if (plan == null)
            {
                Reject("Plan rỗng.", onFailed);
                return;
            }

            plan.SceneName = string.IsNullOrWhiteSpace(plan.SceneName) ? null : plan.SceneName.Trim();
            var request = new Request(plan, onCompleted, onFailed);

            if (IsBusy)
            {
                if (_queued != null)
                {
                    Debug.LogWarning("[Loading] Plan đang chờ bị thay bởi plan mới.");
                    _queued.Failed?.Invoke("Plan bị thay bởi lượt load mới.");
                }
                _queued = request;
                if (Phase == LoadingPhase.Failed) Dismiss();
                return;
            }

            StartCoroutine(RunRoutine(request));
        }

        public void Dismiss()
        {
            if (Phase != LoadingPhase.Failed || _dismissing) return;
            _dismissing = true;
            StartCoroutine(DismissRoutine());
        }

        IEnumerator RunRoutine(Request request)
        {
            int token = ++_token;
            _busy = true;
            _abort = false;
            _doneWeight = 0f;
            _sceneTask = null;
            _currentFailed = request.Failed;
            _activeConfig = request.Plan.Config != null ? request.Plan.Config : config;
            _progress.Reset();
            Publish(0f);

            string problem = Validate(request.Plan);
            if (problem != null)
            {
                NotifyFailed(problem);
                CleanupToIdle();
                PumpQueue();
                yield break;
            }

            yield return OpenCurtain(token);
            if (_abort)
            {
                yield return SettleAbort();
                yield break;
            }

            SetPhase(LoadingPhase.Working);
            yield return Execute(request.Plan);
            if (_abort)
            {
                yield return SettleAbort();
                yield break;
            }

            SetPhase(LoadingPhase.Holding);
            _progress.SetTarget(1f);
            yield return WaitCovered();
            yield return WaitBar();

            yield return Reveal();
            var done = request.Completed;
            CleanupToIdle();
            // Chạy plan đang chờ trước callback, để callback gọi Run thì xếp sau chứ không chồng.
            PumpQueue();
            done?.Invoke();
            Completed?.Invoke();
        }

        IEnumerator DismissRoutine()
        {
            ReleaseSuspendedScene();
            if (_panel != null) yield return Reveal();
            CleanupToIdle();
            PumpQueue();
        }

        IEnumerator SettleAbort()
        {
            bool keepError = _panel != null && _queued == null;
            if (keepError)
            {
                SetPhase(LoadingPhase.Failed);
                yield break;
            }

            if (_panel != null) yield return Reveal();
            CleanupToIdle();
            PumpQueue();
        }

        string Validate(LoadingPlan plan)
        {
            if (UIManager.Instance == null) return "Chưa có UIManager.";
            if (!UIManager.Instance.Contains<LoadingPanel>())
                return "LoadingPanel chưa có trong UIRegistry. Chạy Tools/LSDK/Loading/Setup Loading.";
            return null;
        }

        IEnumerator OpenCurtain(int token)
        {
            SetPhase(LoadingPhase.Covering);
            _session = new LoadingSession(PickTip(_activeConfig));
            EnsureBlock();

            _opened = false;
            _panel = null;
            UIManager.Instance.Open<LoadingPanel>(_session, panel =>
            {
                if (token != _token)
                {
                    if (panel != null && UIManager.Instance != null) UIManager.Instance.Close(panel);
                    return;
                }
                _panel = panel;
                _opened = true;
            });

            float wait = 0f;
            while (!_opened && wait < 10f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_opened || _panel == null)
            {
                Fail("Không mở được LoadingPanel. Kiểm tra UIRegistry.");
                yield break;
            }

            _coveredAt = Time.unscaledTime;
        }

        IEnumerator Execute(LoadingPlan plan)
        {
            if (!string.IsNullOrEmpty(plan.SceneName) && plan.CloseOpenScreens)
                yield return UIManager.Instance.CloseStackedUiRoutine();
            if (_abort) yield break;

            float total = TotalWeight(plan);
            yield return RunTasks(plan.BeforeActivate, total, 1f);
            if (_abort) yield break;

            if (!string.IsNullOrEmpty(plan.SceneName))
                yield return RunScene(plan, total);
            if (_abort) yield break;

            yield return RunTasks(plan.AfterActivate, total, 1f);
        }

        IEnumerator RunScene(LoadingPlan plan, float total)
        {
            float sceneWeight = Mathf.Max(0.0001f, plan.SceneWeight);
            _sceneTask = new SceneLoadTask(plan.SceneName, plan.Mode, sceneWeight, true);
            yield return RunOne(_sceneTask, total, 0.9f);
            if (_abort) yield break;
            if (_sceneTask.Operation == null)
            {
                Fail(string.IsNullOrEmpty(_sceneTask.Error) ? "Không load được scene." : _sceneTask.Error);
                yield break;
            }

            _doneWeight += sceneWeight * 0.9f;
            SetTarget(_doneWeight, total);

            // Scene active ngay khi load xong. Màn che vẫn phủ, nên Awake/Start
            // của scene mới chạy khuất, không hitch lúc mở màn.
            _sceneTask.AllowActivation();
            while (_sceneTask.Operation != null && !_sceneTask.Operation.isDone)
                yield return null;
            yield return null;

            _doneWeight += sceneWeight * 0.1f;
            SetTarget(_doneWeight, total);
            _sceneTask = null;
        }

        IEnumerator RunTasks(List<ILoadTask> tasks, float total, float scale)
        {
            if (tasks == null) yield break;
            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (task == null) continue;
                yield return RunOne(task, total, scale);
                if (_abort) yield break;
                _doneWeight += Mathf.Max(0f, task.Weight) * scale;
                SetTarget(_doneWeight, total);
            }
        }

        IEnumerator RunOne(ILoadTask task, float total, float scale)
        {
            string label = string.IsNullOrEmpty(task.Label) ? "Loading..." : task.Label;
            _session.SetStatus(label);

            float weight = Mathf.Max(0f, task.Weight);
            var box = new ReportBox(value =>
                SetTarget(_doneWeight + weight * Mathf.Clamp01(value) * scale, total));
            yield return task.Run(box);
            if (!string.IsNullOrEmpty(task.Error)) Fail(task.Error);
        }

        IEnumerator WaitCovered()
        {
            float minimum = MinimumCoveredTime;
            while (Time.unscaledTime - _coveredAt < minimum)
                yield return null;
        }

        IEnumerator WaitBar()
        {
            float deadline = Time.unscaledTime + 2f;
            while (_progress.Display + 0.001f < _progress.Target && Time.unscaledTime < deadline)
                yield return null;

            if (_progress.Display + 0.001f < _progress.Target)
            {
                _progress.Snap();
                Publish(_progress.Display);
            }
        }

        IEnumerator Reveal()
        {
            if (_panel == null) yield break;
            SetPhase(LoadingPhase.Revealing);

            var panel = _panel;
            _panelClosed = false;
            panel.Closed += HandlePanelClosed;
            if (!panel.IsOpen && !panel.IsTransitioning) _panelClosed = true;
            else UIManager.Instance.Close(panel);

            float guard = 0f;
            while (!_panelClosed && panel != null && guard < 8f)
            {
                guard += Time.unscaledDeltaTime;
                yield return null;
            }
            if (panel != null) panel.Closed -= HandlePanelClosed;
            _panel = null;
        }

        void HandlePanelClosed() => _panelClosed = true;

        void Fail(string message)
        {
            if (_abort) return;
            _abort = true;
            NotifyFailed(message);
            ReleaseSuspendedScene();
        }

        void NotifyFailed(string message)
        {
            Debug.LogError("[Loading] " + message);
            if (_session != null) _session.SetError(message);
            _currentFailed?.Invoke(message);
            Failed?.Invoke(message);
        }

        void Reject(string message, Action<string> onFailed)
        {
            Debug.LogError("[Loading] " + message);
            onFailed?.Invoke(message);
            Failed?.Invoke(message);
        }

        void CleanupToIdle()
        {
            _panel = null;
            _session = null;
            _sceneTask = null;
            _currentFailed = null;
            _activeConfig = null;
            _token++;
            EnsureUnblock();
            _dismissing = false;
            // Giữ _busy đến sau PhaseChanged để listener gọi Run thì xếp hàng, không chạy chồng.
            SetPhase(LoadingPhase.Idle);
            _busy = false;
        }

        void PumpQueue()
        {
            if (_queued == null) return;
            var next = _queued;
            _queued = null;
            StartCoroutine(RunRoutine(next));
        }

        void ReleaseSuspendedScene()
        {
            var operation = _sceneTask != null ? _sceneTask.Operation : null;
            if (operation != null && !operation.isDone) operation.allowSceneActivation = true;
        }

        void EnsureBlock()
        {
            if (!blockInput || _blocked || UIManager.Instance == null) return;
            UIManager.Instance.Block(true);
            _blocked = true;
        }

        void EnsureUnblock()
        {
            if (!_blocked) return;
            if (UIManager.Instance != null) UIManager.Instance.Block(false);
            _blocked = false;
        }

        void SetPhase(LoadingPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        void SetTarget(float done, float total)
        {
            _progress.SetTarget(total <= 0f ? 1f : done / total);
        }

        void Publish(float value)
        {
            Progress = value;
            if (_session != null) _session.SetProgress(value);
            ProgressChanged?.Invoke(value);
        }

        float MinimumCoveredTime => _activeConfig != null ? _activeConfig.minimumCoveredTime : 0.6f;
        float SmoothSpeed => _activeConfig != null ? _activeConfig.progressSmoothSpeed : 1.5f;

        static float TotalWeight(LoadingPlan plan)
        {
            float total = Sum(plan.BeforeActivate) + Sum(plan.AfterActivate);
            if (!string.IsNullOrEmpty(plan.SceneName))
                total += Mathf.Max(0.0001f, plan.SceneWeight);
            return total;
        }

        static float Sum(List<ILoadTask> tasks)
        {
            if (tasks == null) return 0f;
            float sum = 0f;
            for (int i = 0; i < tasks.Count; i++)
                if (tasks[i] != null) sum += Mathf.Max(0f, tasks[i].Weight);
            return sum;
        }

        static string PickTip(LoadingConfig source)
        {
            if (source == null || source.tips == null || source.tips.Length == 0) return "";
            if (!source.randomTip) return source.tips[0] ?? "";
            int index = UnityEngine.Random.Range(0, source.tips.Length);
            return source.tips[index] ?? "";
        }

        sealed class Request
        {
            public readonly LoadingPlan Plan;
            public readonly Action Completed;
            public readonly Action<string> Failed;

            public Request(LoadingPlan plan, Action completed, Action<string> failed)
            {
                Plan = plan;
                Completed = completed;
                Failed = failed;
            }
        }

        sealed class ReportBox : IProgress<float>
        {
            readonly Action<float> _report;
            public ReportBox(Action<float> report) { _report = report; }
            public void Report(float value) { _report?.Invoke(value); }
        }
    }
}

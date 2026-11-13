using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LSDK.Loading
{
    /// <summary>
    /// LoadSceneAsync. holdActivation = true thì dừng ở progress 0.9
    /// để LoadingManager tự gọi AllowActivation khi màn đã che kín.
    /// </summary>
    public sealed class SceneLoadTask : ILoadTask
    {
        readonly bool _holdActivation;

        public SceneLoadTask(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, float weight = 1f, bool holdActivation = false)
        {
            SceneName = sceneName ?? "";
            Mode = mode;
            Weight = weight < 0f ? 0f : weight;
            _holdActivation = holdActivation;
        }

        public string SceneName { get; }
        public LoadSceneMode Mode { get; }
        public AsyncOperation Operation { get; private set; }

        public string Label => SceneName;
        public float Weight { get; }
        public string Error { get; private set; }

        public IEnumerator Run(IProgress<float> progress)
        {
            if (string.IsNullOrWhiteSpace(SceneName))
            {
                Error = "Thiếu tên scene.";
                yield break;
            }

            try { Operation = SceneManager.LoadSceneAsync(SceneName, Mode); }
            catch (Exception ex)
            {
                Error = ex.Message;
                yield break;
            }

            if (Operation == null)
            {
                Error = $"Không load được scene '{SceneName}'. Kiểm tra Build Settings.";
                yield break;
            }

            Operation.allowSceneActivation = false;
            while (Operation.progress < 0.9f)
            {
                progress?.Report(Mathf.Clamp01(Operation.progress / 0.9f));
                yield return null;
            }

            if (!_holdActivation)
            {
                Operation.allowSceneActivation = true;
                while (!Operation.isDone) yield return null;
            }

            progress?.Report(1f);
        }

        public void AllowActivation()
        {
            if (Operation != null) Operation.allowSceneActivation = true;
        }
    }
}

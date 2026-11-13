using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace LSDK.Loading
{
    /// <summary>
    /// Một lượt load. SceneName rỗng thì chỉ chạy task trong scene hiện tại.
    /// BeforeActivate chạy trước khi scene mới active. AfterActivate chạy khi scene
    /// mới đã active và màn che vẫn đang phủ.
    /// </summary>
    public sealed class LoadingPlan
    {
        public string SceneName;
        public LoadSceneMode Mode = LoadSceneMode.Single;
        public float SceneWeight = 1f;
        public bool CloseOpenScreens = true;
        public LoadingConfig Config;
        public List<ILoadTask> BeforeActivate = new List<ILoadTask>();
        public List<ILoadTask> AfterActivate = new List<ILoadTask>();
    }
}

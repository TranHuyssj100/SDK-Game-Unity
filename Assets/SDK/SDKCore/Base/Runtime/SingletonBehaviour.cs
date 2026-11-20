using UnityEngine;

namespace LSDK.Core.Base
{
    public class SingletonBehaviour<T> : MonoBehaviour
       where T : MonoBehaviour
    {
        private static T _instance;

        /// <summary>true khi đã có instance còn sống. Không tạo mới.</summary>
        public static bool HasInstance => _instance != null;

        /// <summary>
        /// Instance hiện tại. Chưa có thì tìm trên scene rồi mới tạo.
        /// Trả null khi app đang thoát để không tạo object mới lúc dọn scene.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (SingletonRuntime.Quitting) return null;

#if UNITY_2023_1_OR_NEWER
                _instance = FindFirstObjectByType<T>();
#else
                _instance = FindObjectOfType<T>();
#endif

                if (_instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                }
                return _instance;
            }

        }
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this as T;
            // Con của SDKCore được root giữ qua scene. Tự DDOL chỉ khi object này là gốc.
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            OnInitialize();
        }
        protected virtual void OnInitialize()
        {
        }
        protected virtual void OnDestroy()
        {
            // Bản trùng bị hủy không được xóa tham chiếu của bản đang chạy.
            if (_instance == this) _instance = null;
        }
    }
}

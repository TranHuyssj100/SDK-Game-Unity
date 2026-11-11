using UnityEngine;

namespace LSDK.Core.Base
{
    public class SingletonBehaviour<T> : MonoBehaviour
       where T : MonoBehaviour
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                    }
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
            _instance = null;
        }
    }
}
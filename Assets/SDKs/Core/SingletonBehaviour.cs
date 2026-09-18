using UnityEngine;

namespace SDKs.Core
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
            DontDestroyOnLoad(this.gameObject);
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
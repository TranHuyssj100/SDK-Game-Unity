using LSDK.GameData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LSDK.Core
{
    /// <summary>
    /// Gốc của SDK Core. Gọi DontDestroyOnLoad một lần trên object này;
    /// Unity 6 bỏ qua DontDestroyOnLoad nếu gọi trên object con.
    /// Con cần sống qua scene: GameDataManager, IAPManager, PoolServer, UIRoot, EventSystem, LoadingManager.
    /// Observer là class static, không gắn lên GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("LSDK/SDK Core")]
    public class SDKCoreRoot : MonoBehaviour
    {
        public static SDKCoreRoot Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            AdoptGameData();
        }

        private void OnEnable()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            if (Instance != this) return;
            DestroyForeignEventSystems();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // GameData tự tạo trước khi scene load. Đưa instance đó vào dưới SDKCore
        // để object đặt sẵn trên scene còn copy saveDelay rồi tự xóa.
        private void AdoptGameData()
        {
            var data = GameDataManager.Instance;
            if (data == null || data.transform.IsChildOf(transform)) return;

            int index = 0;
            var placed = GetComponentsInChildren<GameDataManager>(true);
            for (int i = 0; i < placed.Length; i++)
            {
                if (placed[i] == data) continue;
                index = placed[i].transform.GetSiblingIndex();
                break;
            }

            data.transform.SetParent(transform, false);
            data.transform.SetSiblingIndex(index);
            data.gameObject.name = "GameDataManager";
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DestroyForeignEventSystems();
        }

        // Scene sau thường có EventSystem riêng. Giữ bản nằm dưới SDKCore.
        private void DestroyForeignEventSystems()
        {
#if UNITY_2023_1_OR_NEWER
            var systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var systems = FindObjectsOfType<EventSystem>();
#endif
            EventSystem owned = null;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i].transform.IsChildOf(transform))
                {
                    owned = systems[i];
                    break;
                }
            }
            if (owned == null) return;

            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null && systems[i] != owned)
                    Destroy(systems[i].gameObject);
            }
        }
    }
}

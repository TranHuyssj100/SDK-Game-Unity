using UnityEngine;

namespace LSDK.Firebase
{
    /// <summary>Tham số SDK. App Id nằm ở google-services / GoogleService-Info, không khai ở đây.</summary>
    [CreateAssetMenu(fileName = "FirebaseConfig", menuName = "LSDK/Firebase/Config")]
    public sealed class FirebaseConfig : ScriptableObject
    {
        /// <summary>Đường dẫn Resources, không có đuôi: Assets/Resources/LSDK/Firebase/SO/FirebaseConfig.asset</summary>
        public const string ResourcePath = "LSDK/Firebase/SO/FirebaseConfig";

        [Header("General")]
        [SerializeField] private bool _autoInitialize = true;

        [Header("Services")]
        [SerializeField] private bool _enableAnalytics = true;
        [SerializeField] private bool _enableCrashlytics = true;
        [SerializeField] private bool _enableAuth = true;
        [SerializeField] private bool _enableInstallations = true;
        [SerializeField] private bool _enableRemoteConfig = true;
        [SerializeField] private bool _enableMessaging = true;

        [Header("Messaging")]
        [Tooltip("Đăng ký FCM bằng Installation ID sau khi init.")]
        [SerializeField] private bool _messagingRegisterOnInit = true;
        [Tooltip("Hiện hộp thoại xin quyền thông báo trên iOS/tvOS lúc init.")]
        [SerializeField] private bool _requestNotificationPermissionOnInit;

        [Header("Remote Config")]
        [Tooltip("Fetch và activate Remote Config ngay sau khi init.")]
        [SerializeField] private bool _fetchOnInit = true;
        [Tooltip("Cache còn hạn thì không tải lại (giây). 0 = luôn fetch.")]
        [SerializeField] private int _cacheExpirationSeconds = 3600;

        public bool AutoInitialize => _autoInitialize;
        public bool EnableAnalytics => _enableAnalytics;
        public bool EnableCrashlytics => _enableCrashlytics;
        public bool EnableAuth => _enableAuth;
        public bool EnableInstallations => _enableInstallations;
        public bool EnableRemoteConfig => _enableRemoteConfig;
        public bool EnableMessaging => _enableMessaging;
        public bool MessagingRegisterOnInit => _messagingRegisterOnInit;
        public bool RequestNotificationPermissionOnInit => _requestNotificationPermissionOnInit;
        public bool FetchOnInit => _fetchOnInit;
        public int  CacheExpirationSeconds => _cacheExpirationSeconds;

        public static FirebaseConfig Load()
        {
            return Resources.Load<FirebaseConfig>(ResourcePath);
        }

        public void ResetToDefaults()
        {
            _autoInitialize = true;
            _enableAnalytics = true;
            _enableCrashlytics = true;
            _enableAuth = true;
            _enableInstallations = true;
            _enableRemoteConfig = true;
            _enableMessaging = true;
            _messagingRegisterOnInit = true;
            _requestNotificationPermissionOnInit = false;
            _fetchOnInit = true;
            _cacheExpirationSeconds = 3600;
        }

        private void Reset()
        {
            ResetToDefaults();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LSDK.Mediation
{
    [Serializable]
    public sealed class AdUnitSet
    {
        public string banner;
        public string interstitial;
        public string rewarded;
        public string appOpen;

        public string Get(AdFormat format)
        {
            string id;
            switch (format)
            {
                case AdFormat.Banner: id = banner; break;
                case AdFormat.Interstitial: id = interstitial; break;
                case AdFormat.Rewarded: id = rewarded; break;
                case AdFormat.AppOpen: id = appOpen; break;
                default: id = null; break;
            }
            return string.IsNullOrEmpty(id) ? string.Empty : id.Trim();
        }
    }

    [Serializable]
    public sealed class NetworkConfig
    {
        public AdNetworkType network = AdNetworkType.AdMob;
        public bool enabled = true;
        [Tooltip("Số nhỏ = ưu tiên cao hơn.")]
        public int priority = 0;
        public AdUnitSet android = new AdUnitSet();
        public AdUnitSet ios = new AdUnitSet();

        public AdUnitSet CurrentPlatformUnits
        {
            get
            {
#if UNITY_IOS
                return ios;
#else
                return android;
#endif
            }
        }

        public string GetAdUnitId(AdFormat format)
        {
            var set = CurrentPlatformUnits;
            return set == null ? string.Empty : set.Get(format);
        }
    }

    [CreateAssetMenu(fileName = "MediationConfig", menuName = "LSDK/Mediation/Config")]
    public sealed class MediationConfig : ScriptableObject
    {
        /// <summary>Đường dẫn trong Resources (không có đuôi): Assets/Resources/LSDK/Mediation/SO/MediationConfig.asset</summary>
        public const string ResourcePath = "LSDK/Mediation/SO/MediationConfig";

        [Header("General")]
        [Tooltip("true = dùng ad unit test của Google. NHỚ tắt khi build release.")]
        public bool useTestAds = true;
        [Tooltip("MediationManager tự Initialize ở Start().")]
        public bool autoInitialize = true;
        [Tooltip("Tự preload Interstitial / Rewarded / AppOpen và tự load lại sau khi show.")]
        public bool autoPreload = true;
        [Tooltip("Thời gian tối đa chờ các network init (giây).")]
        public float initTimeout = 15f;

        [Header("Frequency")]
        [Tooltip("Khoảng cách tối thiểu giữa 2 interstitial (giây).")]
        public float interstitialCooldown = 30f;
        [Tooltip("Xem rewarded xong thì reset cooldown interstitial.")]
        public bool rewardedResetsInterstitialCooldown = true;

        [Header("App Open")]
        [Tooltip("Tự show App Open khi quay lại game từ background.")]
        public bool autoShowAppOpen = true;
        [Tooltip("Phải ở background ít nhất bấy nhiêu giây mới show App Open.")]
        public float appOpenMinBackgroundSeconds = 5f;
        [Tooltip("Khoảng cách tối thiểu giữa 2 App Open (giây).")]
        public float appOpenCooldown = 60f;

        [Header("Retry (exponential backoff)")]
        public float retryBaseDelay = 2f;
        public float retryMaxDelay = 64f;
        [Tooltip("0 = thử mãi.")]
        public int maxRetryCount = 0;

        [Header("Privacy")]
        public ChildDirectedMode childDirected = ChildDirectedMode.Unspecified;
        [Tooltip("Test Device IDs cho GMA (lấy từ logcat). Xóa trước khi release.")]
        public List<string> testDeviceIds = new List<string>();

        [Header("Consent (UMP/GDPR)")]
        [Tooltip("true = bỏ qua xin consent (không dùng UMP). Dùng cho app trẻ em hoặc khu vực không yêu cầu consent.")]
        public bool skipConsent = false;
        [Tooltip("Debug: giả lập vùng địa lý để test consent form. Chỉ dùng khi test, xóa trước khi release.")]
        public ConsentDebugGeography consentDebugGeography = ConsentDebugGeography.Disabled;
        [Tooltip("Hashed Device ID để bật UMP debug mode (lấy từ logcat). Xóa trước khi release.")]
        public List<string> consentTestDeviceHashedIds = new List<string>();

        [Header("Networks")]
        public List<NetworkConfig> networks = new List<NetworkConfig>();

        public static MediationConfig Load()
        {
            return Resources.Load<MediationConfig>(ResourcePath);
        }

        public void ResetToDefaults()
        {
            networks = new List<NetworkConfig>
            {
                new NetworkConfig { network = AdNetworkType.AdMob, enabled = true, priority = 0 }
            };
        }

        private void Reset()
        {
            ResetToDefaults();
        }
    }
}

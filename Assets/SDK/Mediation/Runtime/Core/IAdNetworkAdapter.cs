using System;

namespace LSDK.Mediation
{
    /// <summary>
    /// Hợp đồng giữa MediationManager và từng bên cung cấp ad.
    /// Thêm network mới = viết 1 adapter kế thừa AdNetworkAdapterBase + đăng ký vào AdAdapterRegistry.
    /// Mọi event PHẢI được raise trên main thread.
    /// </summary>
    public interface IAdNetworkAdapter
    {
        AdNetworkType Network { get; }
        bool IsInitialized { get; }
        float BannerHeightPixels { get; }

        event Action<AdFormat> Loaded;
        event Action<AdFormat, MediationError> LoadFailed;
        event Action<AdFormat> Opened;
        event Action<AdFormat> Clicked;
        /// <summary>bool = user đã nhận thưởng (chỉ có nghĩa với Rewarded).</summary>
        event Action<AdFormat, bool> Closed;
        event Action<AdFormat, MediationError> ShowFailed;
        event Action<AdRevenueInfo> RevenuePaid;

        void Initialize(MediationConfig config, NetworkConfig networkConfig, Action<bool> onComplete);

        /// <summary>Network này có ad unit cho format này không.</summary>
        bool IsFormatSupported(AdFormat format);
        bool IsReady(AdFormat format);
        bool IsLoading(AdFormat format);

        /// <summary>Load Interstitial / Rewarded / AppOpen. Banner load qua ShowBanner.</summary>
        void Load(AdFormat format);
        void ShowFullscreen(AdFormat format, string placement);

        void ShowBanner(BannerPosition position);
        void HideBanner();
        void DestroyBanner();

        /// <summary>true nếu UMP/GDPR yêu cầu hiện nút mở Privacy Options (theo ConsentInformation.PrivacyOptionsRequirementStatus).</summary>
        bool IsPrivacyOptionsRequired { get; }

        /// <summary>Mở Privacy Options Form cho user chỉnh lại consent. onComplete(errorMessage) — null nếu thành công.</summary>
        void ShowPrivacyOptionsForm(Action<string> onComplete);
    }
}

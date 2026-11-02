using System;

namespace LSDK.Mediation
{
    public abstract class AdNetworkAdapterBase : IAdNetworkAdapter
    {
        public abstract AdNetworkType Network { get; }
        public bool IsInitialized { get; protected set; }
        public virtual float BannerHeightPixels => 0f;

        public event Action<AdFormat> Loaded;
        public event Action<AdFormat, MediationError> LoadFailed;
        public event Action<AdFormat> Opened;
        public event Action<AdFormat> Clicked;
        public event Action<AdFormat, bool> Closed;
        public event Action<AdFormat, MediationError> ShowFailed;
        public event Action<AdRevenueInfo> RevenuePaid;

        protected void RaiseLoaded(AdFormat f) => Loaded?.Invoke(f);
        protected void RaiseLoadFailed(AdFormat f, MediationError e) => LoadFailed?.Invoke(f, e);
        protected void RaiseOpened(AdFormat f) => Opened?.Invoke(f);
        protected void RaiseClicked(AdFormat f) => Clicked?.Invoke(f);
        protected void RaiseClosed(AdFormat f, bool rewardEarned) => Closed?.Invoke(f, rewardEarned);
        protected void RaiseShowFailed(AdFormat f, MediationError e) => ShowFailed?.Invoke(f, e);
        protected void RaiseRevenue(AdRevenueInfo info) => RevenuePaid?.Invoke(info);

        public abstract void Initialize(MediationConfig config, NetworkConfig networkConfig, Action<bool> onComplete);
        public abstract bool IsFormatSupported(AdFormat format);
        public abstract bool IsReady(AdFormat format);
        public abstract bool IsLoading(AdFormat format);
        public abstract void Load(AdFormat format);
        public abstract void ShowFullscreen(AdFormat format, string placement);
        public abstract void ShowBanner(BannerPosition position);
        public abstract void HideBanner();
        public abstract void DestroyBanner();
    }
}

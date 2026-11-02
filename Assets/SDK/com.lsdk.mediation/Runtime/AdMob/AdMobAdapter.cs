using System;
using System.Collections.Generic;
using UnityEngine;
using GMA = GoogleMobileAds.Api; // alias để tránh trùng tên (AdError, AdSize...) với LSDK.Mediation

namespace LSDK.Mediation.AdMob
{
    /// <summary>
    /// Adapter cho Google Mobile Ads Unity plugin (v8+).
    /// Tự đăng ký vào AdAdapterRegistry, chỉ compile khi có define LSDK_ADMOB.
    /// </summary>
    public sealed class AdMobAdapter : AdNetworkAdapterBase
    {
        private const double AppOpenExpiryHours = 4.0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            AdAdapterRegistry.Register(AdNetworkType.AdMob, () => new AdMobAdapter());
        }

        public override AdNetworkType Network => AdNetworkType.AdMob;
        public override float BannerHeightPixels => _banner != null ? _banner.GetHeightInPixels() : 0f;

        private MediationConfig _config;
        private NetworkConfig _networkConfig;
        private string _currentPlacement = string.Empty;

        private GMA.BannerView _banner;
        private bool _bannerLoaded;

        private GMA.InterstitialAd _interstitial;
        private bool _loadingInterstitial;

        private GMA.RewardedAd _rewarded;
        private bool _loadingRewarded;
        private bool _rewardEarned;

        private GMA.AppOpenAd _appOpen;
        private bool _loadingAppOpen;
        private DateTime _appOpenLoadedAtUtc;

        // ───────────────────────── Init ─────────────────────────
        public override void Initialize(MediationConfig config, NetworkConfig networkConfig, Action<bool> onComplete)
        {
            _config = config;
            _networkConfig = networkConfig;

            // Đưa mọi callback của GMA về main thread
            GMA.MobileAds.RaiseAdEventsOnUnityMainThread = true;

            var request = new GMA.RequestConfiguration();
            if (config.testDeviceIds != null && config.testDeviceIds.Count > 0)
            {
                request.TestDeviceIds = new List<string>(config.testDeviceIds);
            }

            switch (config.childDirected)
            {
                case ChildDirectedMode.True:
                    request.TagForChildDirectedTreatment = GMA.TagForChildDirectedTreatment.True;
                    break;
                case ChildDirectedMode.False:
                    request.TagForChildDirectedTreatment = GMA.TagForChildDirectedTreatment.False;
                    break;
            }
            GMA.MobileAds.SetRequestConfiguration(request);

            GMA.MobileAds.Initialize(status =>
            {
                IsInitialized = true;
                onComplete?.Invoke(true);
            });
        }

        private string GetAdUnitId(AdFormat format)
        {
            if (_config != null && _config.useTestAds) return AdMobTestIds.Get(format);
            return _networkConfig != null ? _networkConfig.GetAdUnitId(format) : string.Empty;
        }

        public override bool IsFormatSupported(AdFormat format)
        {
            return !string.IsNullOrEmpty(GetAdUnitId(format));
        }

        public override bool IsReady(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner:
                    return _banner != null && _bannerLoaded;
                case AdFormat.Interstitial:
                    return _interstitial != null && _interstitial.CanShowAd();
                case AdFormat.Rewarded:
                    return _rewarded != null && _rewarded.CanShowAd();
                case AdFormat.AppOpen:
                    return _appOpen != null && _appOpen.CanShowAd() &&
                           (DateTime.UtcNow - _appOpenLoadedAtUtc).TotalHours < AppOpenExpiryHours;
                default:
                    return false;
            }
        }

        public override bool IsLoading(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Banner: return _banner != null && !_bannerLoaded;
                case AdFormat.Interstitial: return _loadingInterstitial;
                case AdFormat.Rewarded: return _loadingRewarded;
                case AdFormat.AppOpen: return _loadingAppOpen;
                default: return false;
            }
        }

        public override void Load(AdFormat format)
        {
            switch (format)
            {
                case AdFormat.Interstitial: LoadInterstitial(); break;
                case AdFormat.Rewarded: LoadRewarded(); break;
                case AdFormat.AppOpen: LoadAppOpen(); break;
            }
        }

        public override void ShowFullscreen(AdFormat format, string placement)
        {
            _currentPlacement = placement ?? string.Empty;

            if (!IsReady(format))
            {
                RaiseShowFailed(format, new MediationError(-1, "Ad not ready"));
                return;
            }

            switch (format)
            {
                case AdFormat.Interstitial:
                    _interstitial.Show();
                    break;
                case AdFormat.Rewarded:
                    _rewardEarned = false;
                    _rewarded.Show(reward => _rewardEarned = true);
                    break;
                case AdFormat.AppOpen:
                    _appOpen.Show();
                    break;
                default:
                    RaiseShowFailed(format, new MediationError(-2, "Not a fullscreen format"));
                    break;
            }
        }

        // ───────────────────────── Banner ─────────────────────────
        public override void ShowBanner(BannerPosition position)
        {
            var pos = position == BannerPosition.Top ? GMA.AdPosition.Top : GMA.AdPosition.Bottom;

            if (_banner != null)
            {
                _banner.SetPosition(pos);
                _banner.Show();
                return;
            }

            string unitId = GetAdUnitId(AdFormat.Banner);
            if (string.IsNullOrEmpty(unitId))
            {
                RaiseLoadFailed(AdFormat.Banner, new MediationError(-3, "Banner ad unit id is empty"));
                return;
            }

            var size = GMA.AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(GMA.AdSize.FullWidth);
            var banner = new GMA.BannerView(unitId, size, pos);
            _banner = banner;
            _bannerLoaded = false;

            banner.OnBannerAdLoaded += () =>
            {
                if (_banner != banner) return;
                _bannerLoaded = true;
                RaiseLoaded(AdFormat.Banner);
            };
            banner.OnBannerAdLoadFailed += error =>
            {
                if (_banner != banner) return;
                RaiseLoadFailed(AdFormat.Banner, ToError(error));
            };
            banner.OnAdPaid += value =>
                PublishRevenue(AdFormat.Banner, "banner", unitId, value, banner.GetResponseInfo());
            banner.OnAdClicked += () => RaiseClicked(AdFormat.Banner);

            banner.LoadAd(new GMA.AdRequest());
        }

        public override void HideBanner()
        {
            if (_banner != null) _banner.Hide();
        }

        public override void DestroyBanner()
        {
            if (_banner != null)
            {
                _banner.Destroy();
                _banner = null;
            }
            _bannerLoaded = false;
        }

        // ───────────────────────── Interstitial ─────────────────────────
        private void LoadInterstitial()
        {
            string unitId = GetAdUnitId(AdFormat.Interstitial);
            if (string.IsNullOrEmpty(unitId) || _loadingInterstitial) return;

            DisposeInterstitial();
            _loadingInterstitial = true;

            GMA.InterstitialAd.Load(unitId, new GMA.AdRequest(), (ad, error) =>
            {
                _loadingInterstitial = false;

                if (error != null || ad == null)
                {
                    RaiseLoadFailed(AdFormat.Interstitial, ToError(error));
                    return;
                }

                _interstitial = ad;
                ad.OnAdPaid += value =>
                    PublishRevenue(AdFormat.Interstitial, _currentPlacement, unitId, value, ad.GetResponseInfo());
                ad.OnAdClicked += () => RaiseClicked(AdFormat.Interstitial);
                ad.OnAdFullScreenContentOpened += () => RaiseOpened(AdFormat.Interstitial);
                ad.OnAdFullScreenContentClosed += () =>
                {
                    DisposeInterstitial();
                    RaiseClosed(AdFormat.Interstitial, false);
                };
                ad.OnAdFullScreenContentFailed += err =>
                {
                    DisposeInterstitial();
                    RaiseShowFailed(AdFormat.Interstitial, ToError(err));
                };

                RaiseLoaded(AdFormat.Interstitial);
            });
        }

        private void DisposeInterstitial()
        {
            if (_interstitial != null)
            {
                _interstitial.Destroy();
                _interstitial = null;
            }
        }

        // ───────────────────────── Rewarded ─────────────────────────
        private void LoadRewarded()
        {
            string unitId = GetAdUnitId(AdFormat.Rewarded);
            if (string.IsNullOrEmpty(unitId) || _loadingRewarded) return;

            DisposeRewarded();
            _loadingRewarded = true;

            GMA.RewardedAd.Load(unitId, new GMA.AdRequest(), (ad, error) =>
            {
                _loadingRewarded = false;

                if (error != null || ad == null)
                {
                    RaiseLoadFailed(AdFormat.Rewarded, ToError(error));
                    return;
                }

                _rewarded = ad;
                ad.OnAdPaid += value =>
                    PublishRevenue(AdFormat.Rewarded, _currentPlacement, unitId, value, ad.GetResponseInfo());
                ad.OnAdClicked += () => RaiseClicked(AdFormat.Rewarded);
                ad.OnAdFullScreenContentOpened += () => RaiseOpened(AdFormat.Rewarded);
                ad.OnAdFullScreenContentClosed += () =>
                {
                    bool earned = _rewardEarned;
                    _rewardEarned = false;
                    DisposeRewarded();
                    RaiseClosed(AdFormat.Rewarded, earned);
                };
                ad.OnAdFullScreenContentFailed += err =>
                {
                    DisposeRewarded();
                    RaiseShowFailed(AdFormat.Rewarded, ToError(err));
                };

                RaiseLoaded(AdFormat.Rewarded);
            });
        }

        private void DisposeRewarded()
        {
            if (_rewarded != null)
            {
                _rewarded.Destroy();
                _rewarded = null;
            }
        }

        // ───────────────────────── App Open ─────────────────────────
        private void LoadAppOpen()
        {
            string unitId = GetAdUnitId(AdFormat.AppOpen);
            if (string.IsNullOrEmpty(unitId) || _loadingAppOpen) return;

            DisposeAppOpen();
            _loadingAppOpen = true;

            GMA.AppOpenAd.Load(unitId, new GMA.AdRequest(), (ad, error) =>
            {
                _loadingAppOpen = false;

                if (error != null || ad == null)
                {
                    RaiseLoadFailed(AdFormat.AppOpen, ToError(error));
                    return;
                }

                _appOpen = ad;
                _appOpenLoadedAtUtc = DateTime.UtcNow;
                ad.OnAdPaid += value =>
                    PublishRevenue(AdFormat.AppOpen, _currentPlacement, unitId, value, ad.GetResponseInfo());
                ad.OnAdClicked += () => RaiseClicked(AdFormat.AppOpen);
                ad.OnAdFullScreenContentOpened += () => RaiseOpened(AdFormat.AppOpen);
                ad.OnAdFullScreenContentClosed += () =>
                {
                    DisposeAppOpen();
                    RaiseClosed(AdFormat.AppOpen, false);
                };
                ad.OnAdFullScreenContentFailed += err =>
                {
                    DisposeAppOpen();
                    RaiseShowFailed(AdFormat.AppOpen, ToError(err));
                };

                RaiseLoaded(AdFormat.AppOpen);
            });
        }

        private void DisposeAppOpen()
        {
            if (_appOpen != null)
            {
                _appOpen.Destroy();
                _appOpen = null;
            }
        }

        // ───────────────────────── Helpers ─────────────────────────
        private void PublishRevenue(AdFormat format, string placement, string unitId, GMA.AdValue value, GMA.ResponseInfo info)
        {
            string source = string.Empty;
            try
            {
                var loaded = info != null ? info.GetLoadedAdapterResponseInfo() : null;
                if (loaded != null) source = loaded.AdSourceName;
            }
            catch (Exception)
            {
                // API response info thay đổi giữa các version plugin → bỏ qua, không làm hỏng tracking
            }

            RaiseRevenue(new AdRevenueInfo
            {
                Network = AdNetworkType.AdMob,
                Format = format,
                Placement = placement,
                AdUnitId = unitId,
                AdSourceName = source,
                Revenue = value.Value / 1000000.0,
                Currency = value.CurrencyCode,
                Precision = value.Precision.ToString()
            });
        }

        private static MediationError ToError(GMA.LoadAdError error)
        {
            return error == null
                ? new MediationError(-1, "Unknown load error")
                : new MediationError(error.GetCode(), error.GetMessage());
        }

        private static MediationError ToError(GMA.AdError error)
        {
            return error == null
                ? new MediationError(-1, "Unknown error")
                : new MediationError(error.GetCode(), error.GetMessage());
        }
    }
}

# LSDK Mediation (`LSDK.Mediation`)

Quản lý quảng cáo Banner / Interstitial / Rewarded / App Open, nhiều network qua adapter.
Version 1.0.0: tích hợp **Google AdMob**. Singleton + Coroutine, không phụ thuộc UniTask/VContainer.

## Cài đặt
1. Cài Google Mobile Ads Unity plugin (v8+).
   - Qua OpenUPM (`com.google.ads.mobile`): define `LSDK_ADMOB` tự bật.
   - Qua `.unitypackage`: tự thêm `LSDK_ADMOB` vào Scripting Define Symbols.
2. Add package này (Package Manager → Add from git URL, hoặc copy vào `Packages/`).
3. Điền **AdMob App ID** ở `Assets > Google Mobile Ads > Settings` (plugin yêu cầu, SDK không thay được).
4. Menu `LSDK > Mediation > Create Config` → tạo `Assets/Resources/LSDK/MediationConfig.asset`.
5. Điền ad unit id Android/iOS trong config. `useTestAds = true` thì dùng ad unit test của Google.

## Dùng
```csharp
using LSDK.Mediation;

// Tự Initialize theo config (autoInitialize). Gọi tay nếu muốn:
MediationManager.Instance.Initialize();

MediationManager.Instance.ShowBanner(BannerPosition.Bottom);
MediationManager.Instance.HideBanner();

MediationManager.Instance.ShowInterstitial("level_end", result => { /* load scene tiếp */ });

MediationManager.Instance.ShowRewarded("double_coin", result =>
{
    if (result == AdShowResult.Completed) GiveReward();
});

// Remove Ads (nối với IAPManager / GameData)
MediationManager.Instance.SetAdsRemoved(true);

// Tracking
MediationManager.OnAdRevenuePaid += info => { /* TrackingManager.LogAdRevenue(info) */ };
// Mute/pause game khi ad mở
MediationManager.OnFullscreenAdStateChanged += open => AudioListener.pause = open;
```

## Thêm network mới (IronSource, AppLovin...)
1. Tạo asmdef mới `LSDK.Mediation.<Network>` (reference `LSDK.Mediation` + SDK của network, `defineConstraints` theo define riêng).
2. Viết `class XxxAdapter : AdNetworkAdapterBase`, implement các hàm abstract.
3. Đăng ký bằng `[RuntimeInitializeOnLoadMethod]` → `AdAdapterRegistry.Register(AdNetworkType.Xxx, () => new XxxAdapter())`.
4. Thêm `NetworkConfig` tương ứng vào `MediationConfig` (priority thấp = ưu tiên cao).
Core không phải sửa gì.

## Hành vi cần biết
- Waterfall: show từ network ready có priority cao nhất. Banner lỗi thì tự nhảy sang network kế, hết thì backoff rồi thử lại.
- Load lỗi: retry exponential backoff (`retryBaseDelay` → `retryMaxDelay`).
- Interstitial có cooldown; Rewarded xong reset cooldown (tắt được trong config).
- Rewarded: callback gọi SAU khi ad đóng.
- App Open tự show khi quay lại từ background (đủ `appOpenMinBackgroundSeconds`, có cooldown); bỏ qua resume do chính fullscreen ad vừa đóng. Tắt tạm bằng `AppOpenEnabled = false`.
- Show lúc app launch/splash: gọi `ShowAppOpen("launch")` thủ công.
- `useTestAds` phải tắt khi build release; click ad thật của chính mình là vi phạm policy AdMob.

## Chưa có (để version sau)
UMP/GDPR consent, ATT iOS, rewarded interstitial, native ad, fallback show sang network khác khi show lỗi.

# LSDK Firebase (`LSDK.Firebase`)

Wrapper Firebase Unity SDK 14.1.0. Game gọi `FirebaseManager`, không gọi `Firebase.*` trực tiếp.
Singleton + Coroutine. Các tác vụ `Task` của Firebase được chờ trên main thread.

Package đã cài: Analytics, Auth, Crashlytics, Installations, Messaging, Remote Config.

## Cài đặt
1. Import các `.unitypackage` trong `ThirdParty` (Analytics → Installations → Auth → Crashlytics → Remote Config → Messaging).
2. Hộp thoại Dependency Manager: **Keep Unity's**.
3. Đặt `google-services.json` (Android) và `GoogleService-Info.plist` (iOS) theo hướng dẫn Firebase. SDK không chứa App Id.
4. Menu `Tools/LSDK/Firebase/Create Config` → `Assets/Resources/LSDK/Firebase/SO/FirebaseConfig.asset`. Bật Remote Config, `Fetch On Init`, `Cache Expiration Seconds` ở asset này.
5. Menu `Tools/LSDK/Firebase/Open Remote Config Window` để xem các `RemoteConfigModule` đã khai báo. Edit mode hiện giá trị mặc định trong code. Play mode hiện giá trị Firebase sau khi fetch.

Muốn chặn FCM tạo Installation ID trước khi user đồng ý, thêm vào AndroidManifest:

```xml
<meta-data android:name="firebase_messaging_auto_init_enabled" android:value="false" />
```

và tắt `Messaging Register On Init` trong config.

## Dùng

```csharp
using LSDK.Firebase;

FirebaseManager.Instance.Initialize();

FirebaseManager.OnInitialized += ok =>
{
    if (!ok) return;
    FirebaseManager.Instance.LogEvent("level_start", "level", 1);
};

FirebaseManager.Instance.SignInAnonymously((ok, user) =>
{
    if (ok) Debug.Log(user.UserId);
});

FirebaseManager.Instance.FetchAndActivate(ok =>
{
    int reward = (int)FirebaseManager.Instance.GetLong("coin_reward", 10);
});

// ── RemoteConfigModule (nhóm key theo domain) ──────────────────────────────
// 1. Khai báo module (không cần đăng ký thủ công)
using LSDK.Firebase;

public sealed class EconomyConfig : RemoteConfigModule
{
    public override string Prefix => "economy";          // key Firebase = "economy_coinReward", ...

    [SerializeField] private int coinReward = 10;
    [RCKey("max_level")]
    [SerializeField] private int maxLevel = 50;

    public int CoinReward => coinReward;
    public int MaxLevel   => maxLevel;
}

// 2. Đọc giá trị sau khi Firebase đã init
FirebaseManager.OnInitialized += ok =>
{
    if (!ok) return;
    // RemoteConfigManager.Instance tự PopulateAll sau FetchAndActivate
    int coin = RemoteConfigManager.Instance.Get<EconomyConfig>().CoinReward;
};

// 3. Nhận thông báo khi module được cập nhật
RemoteConfigEvents.ModuleUpdated += m =>
{
    if (m is EconomyConfig eco)
        Debug.Log("Economy updated: " + eco.CoinReward);
};

FirebaseManager.OnMessageReceived += message => Debug.Log(message.Title);
FirebaseManager.OnRegistrationId += id => { /* gửi id lên server game */ };

// Mediation đẩy doanh thu sang Analytics
FirebaseManager.Instance.LogAdRevenue("admob", "admob", "rewarded", adUnit, value, "USD");
```

`autoInitialize` bật thì `Start()` tự gọi `Initialize` khi có config trong Resources.

## Hành vi
- Init thất bại (thiếu file cấu hình Firebase, dependency không sẵn sàng) thì mọi API ghi log và trả fallback / callback `false`.
- Auth đổi trạng thái thì gắn user id sang Analytics và Crashlytics nếu hai service đó đang bật.
- Messaging dùng `RegisterAsync` và sự kiện `RegistrationReceived` (Installation ID). API token cũ của Firebase đã deprecated.
- Remote Config chưa có giá trị remote/default thì getter trả fallback.
- Analytics, Crashlytics, Messaging trên Editor/desktop là stub của Firebase: gọi được, không gửi dữ liệu thật.

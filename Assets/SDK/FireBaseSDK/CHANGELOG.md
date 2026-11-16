# Changelog

## 1.2.0
- Xoá `RemoteConfigSettings` ScriptableObject và `RemoteConfigEntry`: `Entries` không còn được dùng.
- Chuyển `FetchOnInit` và `CacheExpirationSeconds` vào `FirebaseConfig` (header "Remote Config").
- Thêm **Remote Config Window** (`Tools/LSDK/Firebase/Open Remote Config Window`): danh sách module bên trái, inspector data bên phải, giống GameData Window. Edit mode hiện giá trị mặc định trong code; Play mode hiện giá trị đã fetch.
- Xoá menu `Tools/LSDK/Firebase/Create Remote Config` (không còn asset RemoteConfigSettings).
- `FirebaseRemoteConfigService`: xoá overload `ApplyDefaults(IReadOnlyList<RemoteConfigEntry>)`.

## 1.1.0
- Thêm hệ thống **RemoteConfigModule** / **RemoteConfigManager**:
  - `RemoteConfigModule` – base class cho từng nhóm key; override `Prefix`, khai báo `[SerializeField]` field, dùng `[RCKey]` để đặt key tùy chỉnh.
  - `RemoteConfigManager` – singleton tự khám phá, `Get<T>()`, `CollectAllDefaults()`, `PopulateAll()`.
  - `RemoteConfigEvents` – static events `ModuleUpdated` / `AllUpdated`.
  - `RemoteConfigManagerEditor` – inspector live: foldout từng module, nút **Populate All**, Copy Key/Value.
- `FirebaseManager.InitializeRoutine` nay gộp default từ module + asset, gọi `PopulateAll()` sau Activate.
- Fix bug `SetField` gọi `field.SetValue(null, ...)` → `field.SetValue(instance, ...)`.
- Thêm `FirebaseRemoteConfigService.ApplyDefaultsRaw(Dictionary<string,object>)`.
- Sample `EconomyConfig` trong `Samples~/RemoteConfigModuleDemo/`.

## 1.0.0
- Thêm wrapper `LSDK.Firebase` trên Firebase Unity SDK 14.1.0: Analytics, Auth, Crashlytics, Installations, Messaging (Installation ID), Remote Config.
- Remote Config nằm ở `RemoteConfigSettings`. Inspector có Copy Key và Copy Value.

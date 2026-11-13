# LSDK Loading

Module load bất đồng bộ của `com.lsdk.core`. Assembly `LSDK.Loading`, namespace `LSDK.Loading`. Tham chiếu `LSDK.UISystem`.

Dùng cho hai việc, cùng một màn che:

- Chuyển scene (`LoadSceneAsync`, kích hoạt khi load xong, màn vẫn phủ).
- Load asset trong scene hiện tại qua `ILoadTask`.

`LoadingPanel` là `UIPanel` trên layer **Blocker**, `OpaqueBackground`, đăng ký trong `UIRegistry`. `LoadingManager` chỉ gọi `UIManager.Open<LoadingPanel>` / `Close`. UIRoot đã DontDestroyOnLoad nên màn che sống qua lúc scene cũ bị hủy.

## Setup

1. Scene đã có UIRoot (`Tools/LSDK/UI System/Create UI Root`) và `UIRegistry`.
2. `Tools/LSDK/Loading/Setup Loading`.
   - Tạo `LoadingConfig`, preset fade, prefab `LoadingPanel`.
   - Đăng ký prefab vào mọi `UIRegistry` (enum id `Loading` nếu auto generate đang bật).
   - Nếu scene đang mở có `SDKCoreRoot` mà chưa có `LoadingManager`, gắn component này làm con của root và gán config.
3. Scene cần load phải có trong Build Settings.
4. `LoadingManager` phải sống qua scene: để dưới `SDKCoreRoot`, hoặc để ở root scene (tự `DontDestroyOnLoad` khi không có cha).

Chạy lại menu không ghi đè prefab, config, preset đã có.

## Dùng

```csharp
using System.Collections;
using System.Collections.Generic;
using LSDK.Loading;
using UnityEngine;

// Chuyển scene. Screen và popup trên UIRoot được đóng trước khi scene cũ bị thay.
LoadingManager.Instance.LoadScene("Gameplay", OnReady, OnFailed);

// Scene mới active xong, màn vẫn che, rồi mới chạy warmup.
LoadingManager.Instance.LoadScene("Gameplay", new ILoadTask[]
{
    new DelegateLoadTask("Pool", 1f, WarmPool),
});

// Chỉ load asset trong scene hiện tại. Không đóng UI đang mở.
LoadingManager.Instance.Load(new ILoadTask[]
{
    new DelegateLoadTask("Map", 2f, LoadMap),
    new DelegateLoadTask("Audio", 1f, () => LoadAudio()),
});

IEnumerator LoadMap(System.IProgress<float> progress)
{
    progress.Report(0.4f);
    yield return null;
    progress.Report(1f);
}

IEnumerator LoadAudio() { yield return null; }
IEnumerator WarmPool(System.IProgress<float> progress) { progress.Report(1f); yield break; }

void OnReady() { }
void OnFailed(string reason) { }
```

Plan đủ pha, khi cần load cả trước và sau lúc scene active:

```csharp
var plan = new LoadingPlan
{
    SceneName = "Gameplay",
    BeforeActivate = { new DelegateLoadTask("Config", 1f, LoadConfig) },
    AfterActivate = { new DelegateLoadTask("Pool", 1f, WarmPool) },
};
LoadingManager.Instance.Run(plan, OnReady, OnFailed);
```

`BeforeActivate` chạy khi scene đích chưa active. `AfterActivate` chạy sau `Awake`/`Start` của scene mới, màn che vẫn phủ. Scene additive: tạo plan với `Mode = LoadSceneMode.Additive` và `CloseOpenScreens = false`.

## Tiến độ và thời gian

- Task báo progress 0–1. Thanh cộng theo `Weight`, rồi đuổi theo giá trị thật bằng `progressSmoothSpeed`.
- Scene chiếm `SceneWeight` (mặc định 1). 90% của phần đó là lúc Unity load tới 0.9, 10% còn lại lúc active.
- `minimumCoveredTime` tính từ lúc animation show của panel kết thúc. Load xong sớm vẫn chờ đủ khoảng này rồi mới mở màn.
- Một lượt tại một thời điểm. Lượt mới trong lúc đang chạy được xếp một chỗ; lượt xếp sau thay lượt đang chờ.
- Lỗi: panel ở lại, nút Đóng gọi `Dismiss()`. Gọi `Load` / `Run` lần nữa thì đóng lỗi và chạy lượt mới. `LoadingManager.Failed` và callback `onFailed` vẫn được gọi.

## Prefab

`LoadingPanel`: layer Blocker, `OpaqueBackground`, `CloseOnBack = false`, `UIAnimator` motion `None` với fade (preset `LoadingFade`, khoảng 0.35s). Motion `None` mặc định của UISystem có duration 0, nên preset này phải giữ fade và duration lớn hơn 0.

Nút Đóng chỉ hiện khi load lỗi.

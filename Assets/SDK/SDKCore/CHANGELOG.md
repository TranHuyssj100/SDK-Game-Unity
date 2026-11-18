# Changelog

## [1.1.5]

- GameManager gọi flow qua `IGameDirector` và `GameDirector` để class kế thừa vẫn nhận callback từ state.
- Log của GameManager đi qua `DebugUtil`.
- State đang chạy hiện read-only trên inspector.

## [1.0.1]

- Thêm module Loading: chuyển scene và load asset dưới `LoadingPanel` của UISystem.

## [1.0.0]

- Gộp Base, Observer, ObjectPools, GameData, UISystem và IAP vào package `com.lsdk.core`.
- Mỗi module giữ asmdef và namespace cũ.
- GameData `1.0.3`, IAP `1.0.4`, Observer `1.0.0`, UISystem `1.0.0` trở thành module của package này.

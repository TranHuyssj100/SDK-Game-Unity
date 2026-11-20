# LSDK Toast

Module toast của `com.lsdk.core`. Assembly `LSDK.ToastSystem`, namespace `LSDK.ToastSystem`.

`ToastPanel` là `UIPanel` trên layer **Overlay**. Item được lấy từ `ObjectPool` của SDK (cùng class mà `PoolServer` dùng). Pool này nằm trên panel, prefab gắn trực tiếp, không khai báo trong `PoolDatabase` — vì dòng toast là UI con của canvas, không phải object gameplay.

## Setup

1. Scene đã có UIRoot và `UIRegistry`.
2. `Tools/LSDK/Toast/Create Toast Sample`.
   - Tạo `Assets/Resources/LSDK/Toast/Prefabs/ToastItem.prefab` và `ToastPanel.prefab`.
   - `ToastPanel` layer Overlay, `UIAnimator` motion None, đã gán item và container.
   - Đăng ký prefab vào mọi `UIRegistry` (tên `Toast`) nếu auto generate đang bật.
   - Prefab đã có thì giữ nguyên, không ghi đè.
3. Màu chữ sửa trên prefab `ToastItem`: Normal trắng, Success xanh, Warning vàng, Error đỏ.

## Dùng

```csharp
using LSDK.ToastSystem;

ToastPanel.Show("Đã sao chép", ToastType.Normal);   // trắng
ToastPanel.Show("Đã lưu", ToastType.Success);       // xanh
ToastPanel.Show("Pin yếu", ToastType.Warning);      // vàng
ToastPanel.Show("Mất mạng", ToastType.Error);       // đỏ
```

Panel mở lần đầu qua `UIManager.Open<ToastPanel>`, những lần sau chỉ mượn thêm item trong pool. Hết dòng thì panel tự đóng. Toast không chặn click xuống screen phía dưới. Animation chạy cả khi `timeScale = 0`.

Nhiều toast cùng lúc xếp cách nhau theo `Stack Spacing`. Hết giờ, item trả về pool.

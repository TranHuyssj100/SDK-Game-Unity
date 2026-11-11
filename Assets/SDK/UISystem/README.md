# LSDK UI System  (`LSDK.UISystem`)

## Cài đặt
1. Copy folder `LSDK.UISystem` vào `<project>/Packages/`
   (hoặc Package Manager > Add package from disk > chọn `package.json`,
   hoặc git URL: `<repo>.git?path=/LSDK.UISystem`).
2. **DOTween phải có asmdef**: `Tools > Demigiant > DOTween Utility Panel > Create ASMDEF`.
   (Package không thể reference `Assembly-CSharp-firstpass`, thiếu bước này sẽ lỗi assembly.)

## Cấu trúc Canvas
```
UIRoot              Canvas (ScreenSpaceOverlay) + CanvasScaler + UIManager
├── Screen          Canvas (sortOrder 0)  + GraphicRaycaster   ← HomePanel, GamePanel…
├── Popup           Canvas (sortOrder 10) + GraphicRaycaster   ← popup thông thường
├── Overlay         Canvas (sortOrder 20) + GraphicRaycaster   ← toast, tooltip
└── Blocker         Canvas (sortOrder 30) + GraphicRaycaster   ← loading fullscreen
    └── InputBlocker  Image trong suốt chặn click khi đang transition
```
- `UIRoot` **không** có `GraphicRaycaster`. Mỗi layer canvas có raycaster riêng.
- Canvas con dùng `overrideSorting = true` để đảm bảo thứ tự.

## Setup nhanh
1. `LSDK > UI System > Create UI Root` → tạo UIRoot với 4 layer canvas và input blocker.
2. `Assets > Create > LSDK > UI System > UI Registry`, kéo vào field `Registry` của UIManager.
3. Mở Registry, kéo thả prefab UI (hoặc cả folder) vào khung drop → enum `UIId` tự generate
   (mặc định `Assets/LSDK/Generated/UIId.cs`).
4. Prefab UI: script kế thừa `UIPanel`, thêm `UIAnimator`, chỉnh `Layer`.

## Dùng
```csharp
using LSDK.UISystem;

public class UIShop : UIPanel
{
    protected override void OnOpen(object data) { }
    protected override void OnShown() { }
    protected override void OnClosed() { }
}

UIManager.Instance.Open(UIId.Shop, new ShopArgs());   // qua enum
UIManager.Instance.Open<UIShop>();                    // qua type
UIManager.Instance.Close(UIId.Shop);
UIManager.Instance.Back();                            // gọi từ Input System mới nếu cần
```

## Animation
- `UIAnimator`: chọn `Motion` (Scale / SlideTop / SlideBottom / SlideLeft / SlideRight).
- `UIAnimationPreset` (SO): bảng thông số ease/duration/delay cho từng motion, dùng chung cho nhiều popup.
  - Nút **Update**: thêm type mới vào UIMotion, giữ giá trị cũ.
  - Nút **Refresh**: reset tất cả về mặc định.
- Motion `SlideTop/Bottom/Left/Right` = cạnh màn hình mà panel vào/ra.
- Muốn animation riêng: tự viết trong `OnOpen/OnShown`.

## Tối ưu Spine
- Tạo child object cho `SkeletonGraphic`, thêm component `SpineHolder`.
  SpineHolder tự thêm Canvas riêng, cô lập mesh rebuild của Spine khỏi panel.
- Tắt `raycastTarget` trên `SkeletonGraphic`.
- Nếu cần fade Spine cùng panel (`CanvasGroup`): bật **CanvasGroup Compatible** trên material Spine.
- Popup / Blocker che kín màn hình: bật `OpaqueBackground` trên `UIPanel`.
  UIManager sẽ tắt Canvas Screen (`Canvas.enabled = false`) khi popup đó mở,
  giúp Spine ở Screen không cập nhật mesh khi bị che hoàn toàn.

## Enum id ổn định
Mỗi entry có `id` int cố định, enum được generate với giá trị tường minh (`Shop = 3`),
nên xoá/thêm entry không làm lệch id đã serialize ở nơi khác.
Package runtime chỉ nhận `System.Enum` nên không phụ thuộc enum generate (không bị circular assembly).

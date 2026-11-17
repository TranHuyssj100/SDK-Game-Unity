# LSDK Utils

Package `com.lsdk.utils`. Tiện ích Editor. Assembly `LSDK.Utils.Editor`, namespace `LSDK.Utils.EditorTools`. Chỉ chạy trên Unity 6.

## Cài đặt

Package Manager → dấu `+` → **Add package from git URL**:

```
https://github.com/TranHuyssj100/SDK-Game-Unity.git?path=/Assets/SDK/Utils#Developer
```

Hoặc thêm vào `Packages/manifest.json`:

```json
"com.lsdk.utils": "https://github.com/TranHuyssj100/SDK-Game-Unity.git?path=/Assets/SDK/Utils#Developer"
```

`#Developer` là bắt buộc: `package.json` nằm trên nhánh `Developer`, còn nhánh mặc định của repo là `main`.

Project đang mở repo này thì không add package này lần nữa. Code đã nằm ở `Assets/SDK/Utils`.

## Dùng

Sau khi Unity import xong, toolbar chính (bên trái) có dropdown Scene Switcher. Menu liệt kê scene trong `Assets/` và `Packages/`. Chọn một scene để mở scene đó trong Editor. Không cần gọi code.

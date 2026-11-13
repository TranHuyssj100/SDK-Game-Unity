# LSDK Core

Package `com.lsdk.core`. Một SDK gồm các module nhỏ, mỗi module một assembly. Namespace và tên asmdef giữ nguyên.

Cài bằng Git URL. Package Manager → dấu `+` → **Add package from git URL**:

```
https://github.com/TranHuyssj100/SDK-Game-Unity.git?path=/Assets/SDK/SDKCore#Developer
```

Hoặc thêm vào `Packages/manifest.json`:

```json
"com.lsdk.core": "https://github.com/TranHuyssj100/SDK-Game-Unity.git?path=/Assets/SDK/SDKCore#Developer"
```

`#Developer` là bắt buộc: `package.json` nằm trên nhánh `Developer`, còn nhánh mặc định của repo là `main`.

`Mediation` không nằm trong package này. Folder đó vẫn ở `Assets/SDK/Mediation`.

## Module

| Folder | Assembly | Namespace |
|---|---|---|
| `Base` | `LSDK.Core.Base` | `LSDK.Core.Base` |
| `Observer` | `LSDK.Observer.Runtime` | `LSDK.Core.Observer` |
| `ObjectPools` | `LSDK.ObjectPools` | `LSDK.Core.ObjectPools` |
| `GameData` | `LSDK.Gamedata.Runtime` | `LSDK.GameData` |
| `UISystem` | `LSDK.UISystem` | `LSDK.UISystem` |
| `Loading` | `LSDK.Loading` | `LSDK.Loading` |
| `IAP` | `LSDK.IAP.Runtime` | `LSDK.IAP` |

`ObjectPools` tham chiếu `Base`. `IAP` tham chiếu `GameData`. `Loading` tham chiếu `UISystem`. Các module còn lại không tham chiếu lẫn nhau.

## Phụ thuộc ngoài

- Unity Purchasing `5.4.3` và Services Core `1.12.5` (IAP).
- Unity UI (`com.unity.ugui`) cho UISystem.
- DOTween cho UISystem. Package này reference `DOTween.dll`, nên project phải có asmdef của DOTween: `Tools > Demigiant > DOTween Utility Panel > Create ASMDEF`. Gói cài nằm ở `UISystem/ThirdParty/dotween.unitypackage`.

## Sample

Trong Package Manager, import:

- GameData Default Modules
- IAP GameData Integration (cần Default Modules)
- IAP Button

Hướng dẫn chi tiết: `IAP/README.md`, `UISystem/README.md`, `Loading/README.md`.

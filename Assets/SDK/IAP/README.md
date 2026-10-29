# LSDK IAP

Package `com.lsdk.iap`, namespace `LSDK.IAP`. Dat o `Assets/SDK/IAP/` (cung layout voi GameData: `Editor/`, `Runtime/`, `package.json`).

Yeu cau: Unity IAP **5.4.3** (`com.unity.purchasing`), `com.unity.services.core`, LSDK GameData. Import sample **Default Modules** cua GameData (CurrencyData nam trong sample do).
Assembly: `LSDK.IAP.Runtime` tham chieu `LSDK.Gamedata.Runtime`, `LSDK.GameData.Modules`, `UnityEngine.Purchasing`, `Unity.Services.Core`.

## Setup

1. `Create > SDK > IAP Catalog`, them san pham (id, googleId, appleId, type, rewards).
   `rewards` la list: moi dong co `rewardKey` + `rewardAmount`. Mot goi co the co nhieu phan, vd Coin 500 va Gem 10.
   Vd: `coin_pack_1` / Consumable / Coin 500; `remove_ads` / NonConsumable / bat `removeAds`; `vip_monthly` / Subscription.
2. Tao GameObject, gan `IAPManager`, keo catalog vao.
3. Boot game (sau khi GameDataManager da len):

```csharp
IAPManager.Instance.Init();   // mac dinh: coin/gem cap vao CurrencyData
```

Can cap qua rieng (item, starter pack...):

```csharp
IAPManager.Instance.Init(new GameDataRewardGranter((p, reward) =>
{
    if (reward.rewardKey == "starter_item") { /* reward.rewardAmount */ }
}));
```

4. Mua hang:

```csharp
IAPManager.Instance.Purchase("coin_pack_1", r =>
{
    if (r.success) Debug.Log("Mua xong " + r.productId);
    else Debug.Log("Loi: " + r.reason);
});

string price = IAPManager.Instance.GetPrice("remove_ads");
bool noAds   = IAPManager.Instance.IsAdsRemoved();   // true neu bat ky goi removeAds nao dang so huu / con han
bool vip     = IAPManager.Instance.IsSubscribed("vip_monthly");
DateTime? exp = IAPManager.Instance.GetSubscriptionExpireUtc("vip_monthly");
IAPManager.Instance.Restore(ok => { });                      // nut Restore tren iOS
```

`OnSubscriptionChanged(productId, active)` ban khi goi duoc kich hoat hoac store bao het han. `IsSubscribed` tu tra false khi qua `expireUtc`, ke ca giua session.

Gia han (transaction moi tu store) cap reward them mot lan roi keo dai han. iOS ban transaction moi moi ky; Google thuong khong, nen dung `IsSubscribed` cho quyen loi thay vi dua vao reward moi ky. Bam mua khi goi con han bi `AlreadyOwned`.

`reason == Deferred`: thanh toan dang cho duyet (Ask to Buy, Google pending). Callback ket thuc ngay; khi duoc duyet, qua duoc cap va ban `OnPurchaseCompleted` (khong goi lai callback).

Callback cua `Purchase` chi nhan ket qua cua dung san pham do. Giao dich store tu ban (pending luc khoi dong, don duoc duyet muon) chi di qua `OnPurchaseCompleted`.

## Verify tren server

Flow: store bao thanh toan -> `IAPManager` POST `IAPReceipt` len `validationUrl` -> server hoi Google / Apple ->
chi khi server tra `valid: true` VA `productId` (SKU store xac nhan) trung `googleId` / `appleId` trong catalog moi `Grant` -> `SaveAll` -> `ConfirmPurchase`.

Cau hinh: dien `Validation Url` tren component `IAPManager` (vd `https://api.mygame.com/iap/verify`), hoac tu truyen validator:

```csharp
var validator = new ServerReceiptValidator("https://api.mygame.com/iap/verify");
validator.Headers["x-api-key"] = "...";
IAPManager.Instance.UserId = playerId;          // server gan don voi tai khoan
IAPManager.Instance.Init(granter, null, validator);
```

Request (JSON, field cua `IAPReceipt`): `productId` (ID noi bo), `storeProductId`, `productType`, `store` (`GooglePlay` / `AppleAppStore`),
`transactionId`, `purchaseToken` + `orderId` (Google), `signedTransaction` + `originalTransactionId` (Apple JWS), `packageName`, `userId`, `unityReceipt`, `restored`.

Response:

```json
{ "valid": true, "productId": "com.lsdk.sdkgames.coin_pack_1", "transactionId": "...", "expiresAtMs": 0, "message": "" }
```

| Ket qua | Client lam gi |
|---|---|
| `valid: true`, productId trung catalog | Cap qua, luu, confirm |
| `valid: false` hoac productId khac catalog | `ValidationFailed`. Khong cap, khong confirm (Google tu hoan tien don chua acknowledge sau 3 ngay) |
| HTTP loi / timeout / body hong | `ValidationError`. Khong cap, khong confirm. Store gui lai don lan mo app sau |

Server KHONG duoc tin `storeProductId` client gui: phai tu tra catalog theo `productId` noi bo roi so voi SKU Google / Apple tra ve,
neu khong nguoi choi mua goi re roi khai la goi dat.

Server mau (Node.js 18+): `Server~/` (Unity bo qua thu muc `~`).

- `products.json`: copy tu `products.example.json`, map `productId` noi bo -> `type`, SKU `google`, SKU `apple` (trung catalog Unity + Play Console + App Store Connect).
- Google: `GOOGLE_PACKAGE_NAME`, `GOOGLE_APPLICATION_CREDENTIALS` (service account da moi vao Play Console). Goi `purchases.products.get` voi SKU tu catalog server, hoac `purchases.subscriptionsv2.get` roi kiem tra `lineItems[].productId`.
- Apple: `APPLE_BUNDLE_ID`, `APPLE_APP_ID` (production), `APPLE_ROOT_CERTS_DIR` chua `AppleRootCA-G3.cer`. Verify chu ky JWS StoreKit 2, kiem tra `bundleId`, `productId`, `revocationDate`.
- `IAP_API_KEY` (tuy chon) so voi header `x-api-key`.
- Chong dung lai token dang luu trong RAM: production can DB voi unique index theo `store + transactionId`.

Bo qua verify: `validationUrl` trong va khong truyen validator (log canh bao tren device). Don cua `MockStoreAdapter` bo qua verify khi `skipValidationForMock` bat.

## Noi voi GameData

- `IAPData : GameDataModule` duoc GameDataManager tu dang ky luc khoi tao. `IAPManager.Init` goi `Register<IAPData>()` de lay instance. Key luu: `SDK_GameData_IAP`.
- Hien trong GameData Window neu window cua may liet ke module dang dang ky.
- Flow ghi: Grant -> `GameDataManager.SaveAll()` (ep ghi ngay, bo qua debounce 1.5s) -> Confirm. Nhu vay crash giua chung thi store gui lai, `processedTransactionIds` chan cap trung.

## Test trong Editor

`MockStoreAdapter` tu dung khi chay Editor (`useMockInEditor`). Doi ket qua:
`MockStoreAdapter.NextResult = MockStoreAdapter.Behaviour.UserCancel;`

Subscription gia han 30 ngay (`MockStoreAdapter.SubscriptionDuration`). Het han thu:
`MockStoreAdapter.SetSubscriptionExpire("vip_monthly", DateTime.UtcNow.AddSeconds(-1));`

## Luu y

- `UnityIAPAdapter` viet theo API IAP v5 (StoreController). Compile loi ten API/assembly thi sua file do va `LSDK.IAP.Runtime.asmdef`.
- Subscription: moi goi luu rieng trong `IAPData.subscriptions`. Luc store san sang va sau Restore, `CheckEntitlement` (FullyEntitled / EntitledButNotFinished = con han). Ngay het han lay tu receipt neu store tra ve; `0` nghia la chua biet han, tin co `active` den lan hoi store ke. Goi `IAPManager.Instance.RefreshEntitlements()` khi app quay lai foreground neu can cap nhat som.
- Ky subscription cu da het han (iOS phat lai lich su khi restore / cai lai) duoc confirm nhung khong cap reward.
- Non-consumable da confirm tu truoc (cai lai game, may moi) duoc khoi phuc tu `OnPurchasesFetched`: verify server roi cap qua 1 lan + `AddOwned`. Hoan tien khong thu hoi.
- Trang thai subscription tu `CheckEntitlement` (khong co receipt) khong di qua server; chi ky mua / gia han moi duoc verify.

# LSDK Currency

Package `com.lsdk.currency`, phụ thuộc `com.lsdk.core`. Assembly `LSDK.Currency`, namespace `LSDK.Currency`.

Mỗi dòng trong `CurrencyCatalog` là một loại tiền. Bật **Use Regen** thì loại đó hồi theo thời gian: có trần, một mốc cho cả hàng chờ, và có thể bật unlimited để chặn tiêu. Tắt hồi thì chỉ là số dư, sàn 0. Heart là dòng mẫu, không phải lối đi riêng.

## Catalog

Asset mặc định: `Assets/Resources/LSDK/Currency/CurrencyCatalog.asset`.

| Id | Tên | Hồi | Trần | Chu kỳ | Unlimited | Số đầu |
|---|---|---|---|---|---|---|
| 0 | Coin | không |  |  | không | 0 |
| 1 | Heart | có | 5 | 1800 giây | có | đầy 5 |
| 2 | Diamond | không |  |  | không | 0 |

Thêm loại mới: sửa catalog, giữ id cũ, rồi `Tools/LSDK/Currency/Generate CurrencyType`. Id đã có save thì không đổi.

`Tools/LSDK/Currency/Create Catalog` tạo asset nếu chưa có và sinh lại enum.

`Tools/LSDK/Currency/Create Currency UI` sinh `CurrencyType`, tạo `CurrencyItem` và `CurrencyPanel` trong `Runtime/Generate` nếu chưa có, thêm ô còn thiếu theo catalog, rồi đăng ký `CurrencyPanel` vào mọi `UIRegistry`. Thanh tiền nằm layer Overlay.

```csharp
CurrencyPanel.Open();
```

## Luật ví hồi

- `nextRegenUnix == 0` nghĩa là không có đơn vị nào đang chờ.
- Rời trạng thái đầy thì mở mốc `now + chu kỳ`.
- Đang hồi dở thì tiêu thêm hoặc cộng thêm vẫn giữ mốc.
- Amount chạm trần thì xóa mốc.
- Đang đầy và mốc đã là 0 thì lần tăng bị bỏ qua. Một lần cộng từ dưới trần vẫn được vượt trần, không bị kẹp.
- Unlimited còn hạn thì mọi lần giảm bị bỏ qua.
- `AddUnlimitedSeconds` cộng dồn từ `max(now, until)`. Amount dưới trần thì đầy lên trần và xóa lịch hồi.
- Hết unlimited thì amount vẫn đầy. Mốc chỉ mở lại sau lần tiêu xuống dưới trần.
- Hồi là lazy. `Refresh` cộng một đơn vị cho mỗi chu kỳ đã trôi, tính từ mốc cũ.
- Amount dưới trần mà chưa có mốc thì `Refresh` chỉ đặt lịch, chưa cộng.

Ví dụ: mốc `T`, amount `2`, trần `5`, chu kỳ `1800`, `now = T + 4500`. Ba đơn vị tới hạn, amount lên `5`, mốc về `0`. Nếu trần chưa chạm sau `n` lần cộng thì mốc còn lại là `T + n * chu kỳ`.

## Dùng

```csharp
using LSDK.Currency;

var wallet = CurrencyManager.Instance;

wallet.Refresh(CurrencyType.Heart);
if (wallet.GetAmount(CurrencyType.Heart) > 0)
    wallet.TrySpend(CurrencyType.Heart, 1);

wallet.Add(CurrencyType.Coin, 100);
wallet.Add(CurrencyType.Heart, 1);
wallet.AddUnlimitedSeconds(CurrencyType.Heart, 3600);

wallet.AmountChanged += change =>
{
    Debug.Log(change.Type + " " + change.Previous + " -> " + change.Amount);
};

int seconds = wallet.GetRegenRemainingSeconds(CurrencyType.Heart);

if (wallet.TryGetState(CurrencyType.Heart, out var state))
    Debug.Log(state.Amount + "/" + state.Max + " " + state.RegenRemainingSeconds);
```

`AmountChanged` chỉ bắn khi số dư đổi. `TryGetState` đọc toàn bộ trạng thái một loại bằng một lần tra cứu, nên UI dùng hàm này thay cho gọi nhiều hàm lẻ.

## Vòng hồi

Manager tự cộng đơn vị khi tới hạn. Vòng ngủ tới mốc `NextRegenUnix` sớm nhất, không quá `Max Sleep Seconds` (mặc định 30 giây, để theo kịp khi người chơi đổi giờ máy). Không có loại nào đang hồi thì vòng dừng hẳn, và tự chạy lại khi chi tiêu mở lịch hồi mới. Quay lại app thì bắt kịp ngay. Tắt `Auto Refresh` thì phải tự gọi `Refresh`.

Vẫn nên gọi `Refresh` ngay trước cổng chơi. `CurrencyPanel` chỉ vẽ, không refresh.

Luật màn chơi nằm ngoài ví. Ví cho amount về 0. Ngưỡng được vào màn, ví dụ chỉ trừ khi amount lớn hơn 1, là việc của gameplay.

Save nằm ở `CurrencyData`, key `SDK_GameData_Currency`, qua GameData.

## Nhận tiền (icon bay)

`CurrencyManager.Add` có `Transform` thì vừa cộng số dư vừa chơi animation. Không gọi `Add` thêm lần nữa.

```csharp
CurrencyManager.Instance.Add(CurrencyType.Coin, 100, button.transform);
CurrencyManager.Instance.Add(CurrencyType.Heart, 1, null); // null: bay từ giữa màn hình
```

Icon lấy từ pool, nổ quanh điểm gọi rồi bay tới đúng ô trên `CurrencyPanel`. Chữ trên `CurrencyItem` cùng loại giữ số cũ cho tới khi icon đầu tiên chạm ô, rồi mới đếm tới số mới. Số icon theo số nhận, kẹp bởi min/max trong config.

Sprite gán một chỗ: ô **Icon** của từng dòng trong `CurrencyCatalog`. Ô tiền và icon bay đều dùng ảnh đó. Catalog để trống thì icon bay dùng sprite đang gán trên Image của `CurrencyItem`.

Tạo asset và prefab pool: `Tools → LSDK → Currency → Create Receive Setup`. Scene cần `PoolServer` đã gán `PoolDatabase`, và `CurrencyPanel` đang mở. Thông số bay, ease, số icon nằm ở `Resources/LSDK/Currency/CurrencyReceiveConfig`.

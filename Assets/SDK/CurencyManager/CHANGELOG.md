# Changelog

## [Unreleased]

- Nhận tiền nằm ở `CurrencyManager.Add(type, amount, origin)`: cộng số dư, icon pool bay từ transform gọi (null thì giữa màn hình) tới `CurrencyItem` cùng loại. Chữ bắt đầu đếm khi icon đầu tiên chạm ô. Thông số ở `CurrencyReceiveConfig`.
- `CurrencyDefinition.Icon` là sprite dùng chung cho ô tiền và icon bay. Catalog trống thì icon bay lấy sprite trên Image của ô.
- `CurrencyRecord` thêm `IsUnlimited` (cờ lưu sẵn, hạ về false khi hết hạn) và `Type`. `_type` được gán lại từ `_typeId` mỗi lần load để inspector hiện đúng tên loại tiền.
- `RegenWallet.SyncUnlimited` đồng bộ cờ trong `Refresh` và `AddUnlimited`. Vòng hồi thức dậy đúng lúc unlimited hết hạn.
- Đổi tên field save `_records` thành `_currencyRecords` trong `CurrencyData`. Save cũ cần xóa hoặc migrate.

## [1.2.0]

- Module thành package `com.lsdk.currency`, phụ thuộc `com.lsdk.core` `1.3.2`.
- Vòng hồi ngủ tới mốc đơn vị kế tiếp thay vì poll mỗi giây. Không có gì đang hồi thì vòng dừng hẳn.
- `RefreshAll` duyệt thẳng catalog, không qua `Enum.IsDefined`. Loại có id mới nhưng chưa generate enum vẫn được hồi.
- Thêm `CurrencyManager.TryGetState` và struct `CurrencyState` để UI đọc trạng thái bằng một lần tra cứu.
- `CurrencyItem` chỉ dựng lại chữ khi số dư, trần hoặc đồng hồ đổi. `CurrencyPanel` chỉ vẽ, chỉ tick khi còn ô đang chờ hồi.
- Bỏ các lần bắt kịp thừa lúc khởi động và khi quay lại app. Seed và thao tác gộp thành một lần `Save`.
- `CurrencyCatalog.Load` thiếu asset thì chỉ thử một lần.
- Code Editor suy ra đường dẫn từ vị trí module (`CurrencyPaths`), dời folder không còn sinh file ở chỗ cũ.
- `_autoRefreshInterval` đổi thành `_maxSleepSeconds` (mặc định 30).

## [1.1.0]

- Menu `Tools/LSDK/Currency/Create Currency UI` tạo prefab trong `Runtime/Generate` và đăng ký `CurrencyPanel` vào UIRegistry.
- `CurrencyItem` hiện số dư. Loại có hồi hiện `số/trần` và đồng hồ khi đang hồi.

## [1.0.0]

- Ví tiền dùng chung cho mọi dòng trong `CurrencyCatalog`.
- Dòng bật hồi có trần, một mốc hồi, bắt kịp offline và unlimited chặn tiêu.
- Catalog mẫu: Coin, Heart (trần 5, 1800 giây, unlimited), Diamond.

# M4-04 — Ghi nhận cọc và hoàn thành đơn

Phụ trách: Phạm Thành Nghĩa — MSSV 23103100308. Codex hỗ trợ triển khai và kiểm thử.
Cập nhật: 27/10/2026. Đầu việc: #24, phối hợp tính tiền M5.

## 1. Chức năng

Nhân viên/Admin mở **Vận hành → Đơn đặt sân → Chi tiết**:

- Đơn đã xác nhận có form **Ghi nhận cọc** khi cọc còn thấp hơn tổng tiền hiện tại.
- Chi tiết hiển thị tiền sân dự kiến, tiền dịch vụ theo giá đã lưu, tổng tiền dự kiến và phần còn phải thu sau cọc.
- Đến giờ kết thúc, đơn đã xác nhận có form **Hoàn thành đơn**; người xử lý phải tích xác nhận đã thu đủ phần còn lại.
- Thành công chuyển về chi tiết và hiện thông báo. Lỗi hiện tại trang, giữ số cọc đã nhập, đọc lại tiền và phiên bản mới; không tự tích lại xác nhận thu đủ.
- Liên kết **Dịch vụ của đơn** luôn có trên chi tiết, kể cả đơn đã nhận cọc/hoàn thành. Quyền sửa dịch vụ vẫn do M5 kiểm tra.

## 2. Quy tắc ghi cọc

`GhiCocAsync(GhiCocYeuCau(MaDatSan, TongCoc, PhienBan), cancellationToken)`.

- Chỉ nhận đơn `DangXuLy` (đã xác nhận).
- `TongCoc` là **tổng cọc đã thực nhận**, gồm cọc trước đó, không phải số tiền cộng thêm.
- Số đồng nguyên dương, tối đa `9999999999999999`, phải lớn hơn cọc đã lưu và không vượt tổng tiền hiện tại.
- Có thể ghi tăng cọc nhiều lần; mỗi lần phải dùng phiên bản vừa đọc.
- Không hỗ trợ giảm/hoàn cọc. Gửi lại cùng phiên bản bị từ chối, không cộng tiền hai lần.
- Cọc bằng tổng tiền được phép; vẫn phải xác nhận thu đủ khi hoàn thành.
- Việc ghi cọc không chốt `TongTien` và không đổi trạng thái. Quy tắc M4-03 tiếp tục chặn hủy đơn có cọc.
- Không áp đặt tỷ lệ cọc cố định; cho phép ghi cọc khi đơn vẫn đã xác nhận, kể cả đã đến giờ kết thúc.

Đây là thao tác ghi nhận tiền đã thu ngoài hệ thống, không kết nối cổng thanh toán.

## 3. Hoàn thành và hợp đồng M5

`HoanThanhDonAsync(HoanThanhDonYeuCau(MaDatSan, DaThuDu, PhienBan), cancellationToken)`.

Yêu cầu hoàn thành không còn chứa `TienSan`/`TienDichVu`. Các bên gọi nội bộ cần chuyển từ constructor cũ năm tham số sang ba tham số nêu trên.

Sau khóa sân, service kiểm tra trạng thái, phiên bản và giờ kết thúc, rồi:

1. Dùng `ITinhTienService.TinhTienSan` với đơn giá/thời gian đã lưu trên đơn.
2. Đọc `ChiTietDatSan` từ database, dùng `TinhTienDichVu` với số lượng và giá đã chụp trên từng dòng.
3. Dùng `TinhTongTien`, kiểm tra giới hạn lưu trữ và tổng không thấp hơn cọc.
4. Lưu `TienSan`, `TienDichVu`, `TongTien`, `NgayHoanThanh` và `HoanThanh` trong cùng transaction.

Không sử dụng giá hiện tại của danh mục để thay giá lịch sử. Không nhận tiền chốt từ HTTP.
`TienCoc` vẫn là cọc lịch sử; sau khi hoàn thành, giao diện hiển thị còn phải thu bằng 0 vì người xử lý đã xác nhận thu đủ.

Tiền dự kiến trên chi tiết quản lý và tiền chốt đều dùng M5. Truy vấn chi tiết cá nhân cũng chuyển hàm tiền sân tạm sang M5.
Constructor `DatSanService` thêm `ITinhTienService`; đăng ký DI M5 có sẵn, các fixture tạo service trực tiếp đã cập nhật.

## 4. Đồng thời, quyền và dữ liệu

- POST `GhiCoc`/`HoanThanh` chỉ cho Admin/NhanVien, có antiforgery và kiểm tra Base64 `RowVersion` đúng 8 byte.
- Khóa sân và transaction dùng cơ chế chung M4; `RowVersion` kiểm soát mọi cập nhật đơn.
- M5 cập nhật `RowVersion` khi sửa dịch vụ. Nếu dịch vụ đổi sau khi mở trang hoặc ngay trước UPDATE của M4, thao tác tiền bị từ chối, không ghi đè dịch vụ.
- Khi xung đột, rollback và bỏ entity đơn khỏi tracking để lần `SaveChanges` sau không vô tình lưu thao tác đã thất bại.
- Khi M5 giảm dịch vụ khiến tổng thấp hơn cọc, trang báo cần kiểm tra lại và chặn hoàn thành.
- URL quay lại chỉ nhận URL nội bộ; dữ liệu nhập được Razor mã hóa.
- Không thay schema/migration, không thay connection string ứng dụng.

## 5. Kiểm thử ngày 20/10/2026

- Build toàn solution `--no-restore`: **0 warning, 0 error**.
- M4 trên SQL Server LocalDB riêng `M4Issue24_20261020`: **183 passed, 0 failed, 0 skipped**; tăng 42 ca so với 141 ca trước M4-04.
- JavaScript: **5 passed, 0 failed**, gồm xác nhận ghi cọc/hoàn thành, khóa mọi nút và khôi phục khi quay lại.
- M5: **5 passed, 0 failed**.
- Hồi quy M103: **47 passed**; M104: **24 passed**, không có ca thất bại hoặc bỏ qua.
- Hồi quy M201: **108 passed, 0 failed, 5 skipped**. Năm ca SQL Server tùy chọn của M2 không bật `QLDS_M2_TEST_SQLSERVER`; không coi các ca này là đã kiểm thử.
- `git diff --check`: đạt.

Các ca M4 bổ sung bao phủ:

- Ghi cọc nhiều lần, cọc bằng tổng, không giảm cọc, số tiền sai/lẻ/vượt giới hạn, vượt tổng.
- Phân quyền HTTP, antiforgery, phiên bản sai/cũ, trạng thái và giờ không cho phép.
- Chốt tiền bằng giá trên đơn/dòng dịch vụ; bỏ qua giá giả từ form và giá danh mục đã thay đổi.
- Không hoàn thành nếu chưa xác nhận thu đủ; sau hoàn thành không sửa dịch vụ.
- Hai POST đồng thời chỉ một lần thành công; gửi lại không thay đổi dữ liệu.
- Sửa dịch vụ sau khi mở form và sửa bằng service M5 ở context khác ngay trước SQL UPDATE của M4.
- Lỗi sau SQL write rollback cọc/tiền/trạng thái; tổng vượt `decimal(18,2)` trả lỗi nghiệp vụ.

Lần chạy trong sandbox không truy cập được registry LocalDB; chạy lại ngoài sandbox trên instance riêng đạt số liệu trên.
Fixture tạo/xóa database tên ngẫu nhiên riêng, không dùng database ứng dụng.
TRX tại `%TEMP%\M4Issue24-results\m404.trx`. Máy thiếu Node trong PATH, đã dùng Node portable 22.20.0 trong thư mục tạm.
Kiểm thử giao diện hiện ở mức HTTP render và JS với DOM mô phỏng; chưa nghiệm thu trực quan trình duyệt desktop/mobile trong lượt này.

```powershell
$env:M4_TEST_SQL_SERVER = '(localdb)\M4Issue24_20261020'
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --no-restore
dotnet test tests/QuanLyDatSan.M4.Tests/QuanLyDatSan.M4.Tests.csproj --no-build --no-restore
node --test tests/QuanLyDatSan.M4.Tests/quan-ly-don-dat.test.cjs
git diff --check
```

### Chạy lại ngày 27/10/2026

- M4 Release trên LocalDB riêng `M4Issue24_20261020`: **183 passed, 0 failed, 0 skipped**.
- M5 Release: **5 passed, 0 failed, 0 skipped**.
- `git diff --check`: đạt.
- Báo cáo M4: `%TEMP%\M4Issue24-20261027\m404-retest.trx`.
- JavaScript chưa chạy lại trong lượt này; Node không có trong PATH và bản portable cũ không còn trong thư mục tạm. Kết quả 5 ca JavaScript ở trên thuộc lần chạy 20/10.
- Chưa hoàn tất nghiệm thu trực quan giao diện ghi cọc/hoàn thành trên trình duyệt desktop/mobile.

## 6. Kiểm tra thủ công khi review

1. Đăng nhập Nhân viên/Admin, xác nhận một đơn và ghi tổng cọc 50.000; tăng lên 80.000, kiểm tra cọc lưu là 80.000.
2. Thử ghi bằng/thấp hơn cọc cũ, lớn hơn tổng, số tiền lẻ hoặc trống: hiện lỗi, dữ liệu không đổi.
3. Thêm dịch vụ, xem tổng tiền và phần còn lại; thay giá danh mục không được đổi giá dịch vụ đã ghi trên đơn.
4. Với đơn đến giờ kết thúc, tích thu đủ và hoàn thành: lưu tiền chốt/thời điểm, cọc giữ nguyên, phần còn lại bằng 0.
5. Mở hai tab; sửa dịch vụ hoặc ghi cọc ở tab thứ nhất, gửi tab cũ: hiện lỗi, nạp phiên bản mới, không tự tích thu đủ.
6. Kiểm tra bố cục desktop/mobile, bàn phím và hộp xác nhận; quay lại bằng lịch sử không bị kẹt nút.

## 7. Bàn giao

Nội dung PR: [PR-M4-04.md](PR-M4-04.md).
Code và tài liệu ở workspace; chưa commit/push/mở PR hoặc đóng issue #24 trong lượt này.
M4-01 đã merge qua PR #54 ngày 23/10/2026 và issue #21 đã đóng. Giao diện khách hủy của M3 và phần tích hợp tìm sân trống của M2 là các phần bàn giao riêng.

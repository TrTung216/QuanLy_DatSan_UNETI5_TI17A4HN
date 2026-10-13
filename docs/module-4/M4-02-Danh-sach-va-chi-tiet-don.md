# M4-02 — Danh sách và chi tiết đơn cho nhân viên/Admin

Ngày thực hiện: 13/10/2026. Codex hỗ trợ theo yêu cầu người dùng.
Issue: [#22](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/22).

## Chức năng

- Đăng nhập bằng Admin hoặc Nhân viên, chọn **Vận hành → Đơn đặt sân**.
- Danh sách: `/QuanLyDonDat/DanhSach`.
- Chi tiết: `/QuanLyDonDat/ChiTiet/{id}`; đơn không tồn tại trả 404.
- Khách hàng bị từ chối quyền trên cả hai endpoint; người chưa đăng nhập được chuyển tới đăng nhập.
- Từ khóa tìm trong tên khách hàng **hoặc** tên sân. Các bộ lọc loại sân, trạng thái,
  ngày sử dụng từ/đến kết hợp bằng AND. Khoảng ngày dựa trên `GioBatDau`, bao gồm cả hai ngày biên.
- Sắp xếp ngày sử dụng, tên khách hoặc tên sân theo hai chiều; thêm mã đơn giảm dần
  để thứ tự ổn định khi trùng giá trị. Mặc định ngày sử dụng giảm dần.
- Mỗi trang 10 đơn; trang ngoài phạm vi được đưa về trang hợp lệ. Liên kết phân trang
  và nút quay lại từ chi tiết giữ từ khóa, bộ lọc, cách sắp xếp và trang hiện tại.
- Bộ lọc sai hiện validation, danh sách rỗng có thông báo. Loại sân ngừng hoạt động
  vẫn có trong bộ lọc để xem đơn lịch sử.
- Chi tiết hiển thị khách, sân, thời gian, giá lưu trên đơn, cọc, các mốc xử lý và lý do
  hủy/từ chối. Tiền đã chốt lấy từ đơn; đơn chưa chốt hiển thị tiền sân dự kiến.

## Cấu trúc

| Thành phần | Trách nhiệm |
| --- | --- |
| `Controllers/QuanLyDonDatController.cs` | Authorize, validation, gọi service, ánh xạ ViewModel |
| `Services/DatSan/HopDongDocDonQuanLy.cs` | Bộ lọc, cách sắp xếp, DTO cho truy vấn quản lý |
| `Services/DatSan/IDatSanService.cs` | Bổ sung ba phương thức đọc quản lý vào interface dùng chung |
| `Services/DatSan/DatSanService.DocQuanLy.cs` | Truy vấn SQL, lọc/sắp xếp/phân trang, chi tiết và dropdown |
| `ViewModels/QuanLyDonDatViewModel.cs` | Validation bộ lọc, dữ liệu hiển thị và tham số liên kết |
| `Views/QuanLyDonDat/` | Danh sách và chi tiết dùng layout quản trị hiện tại |
| `Services/MenuQuanTri.cs` | Menu bên và lối vào trên trang điều hành cho đúng vai trò |
| `tests/QuanLyDatSan.M4.Tests/QuanLyDonDatTests.cs` | 20 ca SQL Server/HTTP cho M4-02 |

Controller không truy vấn DbContext. Service trả DTO đã tải, dùng `AsNoTracking` và
không đưa entity/tài khoản/mật khẩu sang view. Endpoint quản lý không được cache.
`quayLai` chỉ chấp nhận URL nội bộ. Nội dung khách nhập được Razor mã hóa khi hiển thị.

## Kiểm thử

Kết quả ngày 13/10/2026: build thành công, **231 test đạt, 0 lỗi, 0 bỏ qua**.

| Project test | Số ca đạt |
| --- | ---: |
| M4 (gồm 20 ca mới của M4-02) | 82 |
| M103 | 45 |
| M104 | 24 |
| M201 | 80 |

Chạy từ thư mục chứa solution:

```powershell
dotnet test tests/QuanLyDatSan.M4.Tests/QuanLyDatSan.M4.Tests.csproj --filter FullyQualifiedName~QuanLyDonDatTests
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --logger "trx;LogFileName=m402-regression.trx"
```

Các ca mới kiểm tra quyền khách/nhân viên/Admin và menu; lọc kết hợp và biên ngày;
tìm theo khách/sân; sáu cách sắp xếp với dữ liệu trùng giá trị; phân trang; truy vấn sai;
giữ tham số khi sang trang/chi tiết/quay lại; chặn URL quay lại bên ngoài; chi tiết không tồn tại;
tiền đã chốt/dự kiến và mã hóa lý do có thẻ HTML.

Fixture M4 dùng SQL Server LocalDB thật và database riêng `QuanLyDatSan_M4Test_<guid>`,
tự dọn sau test. Chạy trên bản sao tạm của source mới từ main. Fixture tạo schema từ model
bằng `EnsureCreated`, không kiểm chứng chuỗi migration. Không thay đổi `QuanLyDatSan_Local`.

## Phần tiếp theo

- #23: nối action/form xác nhận, từ chối, hủy vào nghiệp vụ service đã có,
  kèm antiforgery, RowVersion và kiểm thử HTTP. Hủy từ giao diện khách phối hợp M3 (#20).
- #24: ghi nhận cọc và hoàn thành đơn, phối hợp hợp đồng tính tiền M5 (#27).
- M4-02 chưa bổ sung các nút chuyển trạng thái và chưa kiểm tra giao diện trực quan bằng trình duyệt.
- Đây là kết quả triển khai local, chưa xác nhận nhóm review/merge hoặc đóng issue trên GitHub.

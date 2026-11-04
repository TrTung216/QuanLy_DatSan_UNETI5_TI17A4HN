# M2 — Ảnh đại diện sân

Phan Giang Tâm — 23103100196. Ngày 04/11/2026.

## Phần đã bổ sung

- Dùng nền ảnh M1 từ PR #68, main `1676620`; không tạo migration hoặc kho ảnh mới.
- Admin/Nhân viên mở chi tiết sân để tải, thay hoặc gỡ một ảnh. Trang sửa có liên kết đến phần ảnh; form thông tin và form ảnh độc lập.
- Danh sách có ảnh thu nhỏ và chi tiết dùng partial `_AnhDaiDien` của M1. Không lấy ảnh loại sân làm ảnh sân. File mất hiện fallback qua JS ảnh dùng chung.
- Controller nhận `CapNhatAnhViewModel`, POST + antiforgery và giới hạn request 6 MiB. Stream chỉ được giữ đến khi service hoàn tất.
- `SanTheThaoService.Anh.cs` kiểm quyền và phiên bản, giữ khóa kho file, lưu ảnh mới hoàn chỉnh rồi mới lưu khóa DB qua UoW. Chỉ dọn ảnh cũ sau commit bằng `IDonDepAnh`.
- Xung đột token ảnh trả `XungDotAnh`, không ghi đè. Commit không xác định giữ file mới cho công cụ bảo trì. Lỗi dọn sau commit trả thành công kèm cảnh báo.
- Xóa sân bị FK chặn giữ file. Xóa thành công mới gọi dọn ảnh; công cụ bảo trì M1 xử lý file còn sót. Sửa/xóa sân gặp concurrency trả `XungDotDuLieu` với thông báo tải lại, tránh kết luận bản ghi đã mất chỉ vì ảnh thay đổi.

## Chạy local

Dừng ứng dụng cũ. Từ gốc repository:

```powershell
dotnet restore
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release
dotnet ef database update --project QuanLyDatSan_UNETI5_DHTI17A4HN --configuration Release --no-build -- --environment Development
```

Áp dụng migration nền M1 `20261104074333_ThemAnhDaiDienSanVaLoaiSan` bằng connection string local đúng máy. Không tạo lại migration ảnh hoặc reset DB.

Cấu hình kho riêng ngoài repo/publish, thay đường dẫn ví dụ theo máy:

```powershell
dotnet user-secrets set "LuuTruAnh:ThuMucGoc" "D:\DuLieuDatSan\Anh" --project QuanLyDatSan_UNETI5_DHTI17A4HN
dotnet run --project QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http
```

Ảnh không đi theo Git. Không đưa file upload, đường dẫn cá nhân hay User Secrets vào commit. Chưa cấu hình kho thì chức năng thông tin sân vẫn dùng được; thay ảnh báo kho chưa sẵn sàng.

## Thử chức năng

1. Đăng nhập Admin/Nhân viên → Danh sách sân → Xem → Ảnh đại diện sân.
2. Chọn JPEG/PNG/WebP tĩnh tối đa 5 MiB → Lưu ảnh sân. Tải lại, xem danh sách và chi tiết.
3. Thay ảnh khác, sửa giá/tên sân, kiểm tra ảnh còn nguyên. Gỡ ảnh khi không chọn file.
4. Mở hai tab cùng ảnh cũ, thay ảnh ở tab đầu rồi gửi tab sau: tab sau báo xung đột, tải lại trước khi tiếp tục.
5. Khách chỉ đọc ảnh sân và loại sân đang hoạt động; không có form ghi. Bản ghi bị ẩn/file mất trả 404 ở endpoint ảnh.
6. Sân có đơn đặt không xóa được; ảnh vẫn còn. Sân chưa có dữ liệu tham chiếu xóa thành công thì dọn file ảnh.

Không dùng ảnh riêng tư hoặc ảnh không được phép sử dụng để demo.

## Kiểm thử

```powershell
$env:QLDS_M2_TEST_SQLSERVER = '1'
dotnet test tests/QuanLyDatSan.M201.Tests/QuanLyDatSan.M201.Tests.csproj -c Release
```

`AnhSanTests.cs`: HTTP upload/thay/gỡ, giữ thông tin, quyền, CSRF, file lỗi, request lớn, kho thiếu, ảnh bị ẩn và fallback.

`AnhSanSqlTests.cs`: quyền service, hai yêu cầu thay ảnh, phiên bản cũ, ảnh cạnh tranh sửa/xóa thông tin, FK giữ lịch sử, kết quả commit chưa rõ và cleanup thất bại. Fixture dùng DB ngẫu nhiên/thư mục tạm, không dùng DB hoặc kho ảnh cá nhân.

Test nền M1 kiểm decoder/pixel/metadata/path traversal; không nhân bản bộ giải mã trong M2. Khởi tạo lại scope/kho đọc được ảnh là kiểm tra tự động, không thay nghiệm thu restart/deploy thật hoặc trình duyệt desktop/mobile.

## Kết quả ngày 04/11/2026

- Chạy toàn solution cấu hình Release, bật SQL Server M1/M2: M103 107, M104 52, M201 165, M4 237 — tổng 561 ca đạt, 0 lỗi test, 0 bỏ qua.
- Build trong lượt test thành công; còn 2 cảnh báo CS0219 sẵn có ở `Views/ThongKe/Index.cshtml`.
- M4 chạy trên instance LocalDB test sạch tự tạo/dọn vì database mẫu `model` của instance mặc định trên máy có lịch sử migration cũ. Không sửa database mẫu hoặc database ứng dụng.
- Diff Model/DbContext/migration rỗng; M2 sử dụng nguyên nền ảnh M1. `git diff --check` đạt.
- Chưa nghiệm thu trực quan browser desktop/mobile hoặc khởi động lại ứng dụng với kho ảnh thật. Chưa áp dụng migration/cấu hình kho ảnh thay người dùng.

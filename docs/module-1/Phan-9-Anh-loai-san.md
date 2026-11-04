# Bàn giao nền ảnh M1 và hướng dẫn tích hợp M2

Trần Trọng Tùng — 23103100202 — M1. Ngày 04/11/2026.

Nhánh `feature/m1-kho-anh-dung-chung` dựa trên main `6fa6c3b`, sau nền đặt cọc PR #67. Đây là hợp đồng để review/merge; M2 lấy main sau khi PR nền được merge, không tự khôi phục migration ảnh cũ từ stash.

## 1. Phần đã triển khai và phần M2 tiếp tục

M1 cung cấp kho file, chuẩn hóa ảnh, hợp đồng DTO/lỗi, DI, endpoint đọc ảnh sân/loại sân, migration hai cột ảnh, công cụ dọn file mồ côi và partial hiển thị. Admin có thể thay/gỡ ảnh loại sân tại trang chi tiết; trang chủ đọc ảnh sân/loại sân từ DB và có trạng thái chưa có ảnh.

M2 tiếp tục làm nghiệp vụ ảnh sân trong service/repository của M2, form thay/gỡ, hiển thị danh sách/chi tiết sân và test quyền Admin/Nhân viên. PR nền không tạo endpoint ghi ảnh sân hoặc làm thay phần này. Sau merge, M2 không cần thêm package xử lý ảnh, không tạo lại cột ảnh/migration và không tự mở static directory.

## 2. File dùng chung

| File/thư mục trong project ứng dụng | Chức năng |
| --- | --- |
| `Services/Anh/HopDongAnh.cs` | Interface, DTO, enum và exception độc lập MVC/EF |
| `Infrastructure/Anh/KhoAnhDiaPhuong.cs` | Lưu, đọc, xóa khóa ảnh trong thư mục ngoài source/publish |
| `Infrastructure/Anh/ChuanHoaAnh.cs`, `policy.xml` | Kiểm tra JPEG/PNG/WebP, 5 MiB, 4096 px/chiều, 16 triệu pixel; chuẩn hóa JPEG tối đa 1600 px, bỏ metadata |
| `Infrastructure/Anh/GioiHanTaiAnhAttribute.cs`, `GioiHanAnhMiddleware.cs` | Giới hạn request 6 MiB; phản hồi 413 trước model binding cho endpoint được đánh dấu |
| `Repositories/Anh/` | Đọc khóa/quyền công khai và kiểm tra tham chiếu cả hai bảng |
| `Services/Anh/AnhService.cs` | Quyền đọc và điều phối thay/gỡ ảnh loại sân |
| `Services/Anh/DonDepAnh.cs` | Kiểm tra tham chiếu bằng scope DB mới rồi dọn file |
| `Infrastructure/Anh/DonAnhMoCoi.cs` | CLI bảo trì với khóa độc quyền, chỉ file không tham chiếu cũ hơn 24 giờ |
| `Controllers/AnhController.cs` | Endpoint đọc theo ID, không nhận khóa/đường dẫn từ client |
| `Extensions/DependencyInjection.cs` | `AddModuleAnh()` được gọi từ `AddUngDungServices()` |
| `Views/Shared/_AnhDaiDien.cshtml`, `ViewModels/AnhViewModel.cs` | Thẻ ảnh/fallback và form DTO dùng lại |
| `wwwroot/css/anh.css`, `wwwroot/js/anh.js` | Tỷ lệ ảnh, fallback khi file mất; đã nạp trong các layout chung |

Kho local hỗ trợ nhiều tiến trình cùng truy cập một thư mục trên cùng máy bằng khóa file. Không coi đây là giải pháp kho phân tán/cloud; nếu thay implementation phải thiết kế lại khóa và bảo trì cùng M1.

## 3. Hợp đồng chính xác

Namespace: `QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh`.

```csharp
public enum NhomAnh { LoaiSan, San }
public enum ThaoTacAnh { ThayAnh, GoAnh }
public sealed record AnhTaiLen(Stream NoiDung, string TenTep, long DoDai);
public sealed record YeuCauAnh(ThaoTacAnh ThaoTac, string? AnhCu, AnhTaiLen? Tep);
public sealed record KetQuaAnh(MaLoiAnh MaLoi, bool CanDonTep = false);
public sealed record TepAnhCu(string Khoa, DateTime SuaLanCuoiUtc);

public interface IKhoAnh
{
    Task<IDisposable> GiuKhoaAsync(bool baoTri, CancellationToken ct);
    Task<string> LuuAsync(NhomAnh nhom, AnhTaiLen tep, CancellationToken ct);
    Task<Stream?> MoDocAsync(string khoa, CancellationToken ct);
    Task XoaAsync(string khoa, CancellationToken ct);
    IEnumerable<TepAnhCu> LietKeTepCu(DateTime truocUtc);
}
public interface IDonDepAnh
{
    Task<bool> XoaNeuKhongThamChieuAsync(string? khoa);
}
```

- Caller sở hữu stream upload: mở trước khi gọi service, giữ qua `await`, dispose sau đó. `IKhoAnh` không dispose stream upload. Stream trả bởi `MoDocAsync` thuộc caller; `FileStreamResult` của MVC sẽ dispose sau response.
- `TenTep` chỉ phục vụ kiểm tra định dạng; `DoDai` chỉ là dữ liệu khai báo. Kho kiểm tra số byte thật và sinh khóa mới `san/<guid>.jpg` hoặc `loai-san/<guid>.jpg`.
- `AnhCu` chỉ là giá trị so sánh chống ghi đè. Không truyền nó trực tiếp vào `XoaAsync` hoặc ghép đường dẫn; lấy khóa cũ từ bản ghi DB.
- Request thường giữ `using var khoa = await kho.GiuKhoaAsync(false, ct)` từ trước khi lưu file đến sau lưu DB/dọn bù. `true` dành cho công cụ bảo trì. Đây là khóa phối hợp file, không thay thế concurrency token DB.
- `LuuAsync` trả khóa sau khi đóng file hoàn chỉnh. `MoDocAsync` trả null khi file không còn. Các lỗi ảnh dự kiến ném `LoiAnhException` chứa `MaLoi`; service bắt và trả `KetQuaAnh`. Cancellation và lỗi ngoài dự kiến không bị che thành thành công.
- Service M2 không gọi `LietKeTepCu`; không xóa cả thư mục. Sau commit dùng `IDonDepAnh` thay vì xóa trực tiếp để bảo vệ file còn tham chiếu.

| `MaLoiAnh` | Caller xử lý |
| --- | --- |
| `ThanhCong` | Redirect + Success; thêm Warning nếu `CanDonTep` |
| `DuLieuAnhKhongHopLe` | Báo thiếu file, dữ liệu/hành động không hợp lệ |
| `DinhDangAnhKhongHoTro` | Chỉ JPEG/PNG/WebP tĩnh đúng nội dung |
| `AnhQuaDungLuong` | Tối đa 5 MiB/file |
| `AnhQuaKichThuoc` | Tối đa 4096 px/chiều và 16 triệu pixel |
| `AnhKhongDocDuoc` | File hỏng/không giải mã được |
| `XungDotAnh` | Yêu cầu tải lại trang; không retry ghi đè |
| `LuuTruAnhChuaSanSang` | Báo kho chưa sẵn sàng, không lộ đường dẫn |
| `KhongTonTai` | 404 cho thao tác ghi |
| `KhongDuQuyen` | Forbid cho thao tác ghi |

## 4. M2 triển khai theo luồng nào

1. Controller POST giữ quyền Admin/Nhân viên và antiforgery. Dùng `CapNhatAnhViewModel` hoặc ViewModel tương đương, chỉ một file; gỡ ảnh không gửi file kèm. Kiểm tra ModelState, số file và ID. Không bind Entity sân.
2. Gắn các attribute sau (namespace `Infrastructure.Anh`); middleware chung tự nhận diện, không cần hardcode tên action M2:

   ```csharp
   [HttpPost, ValidateAntiForgeryToken]
   [GioiHanTaiAnh, RequestSizeLimit(ChuanHoaAnh.GioiHanRequest)]
   [RequestFormLimits(MultipartBodyLengthLimit = ChuanHoaAnh.GioiHanRequest)]
   ```

3. Controller mở `await using var stream = model.Tep?.OpenReadStream()`, tạo `AnhTaiLen`/`YeuCauAnh`, gọi service M2 và ánh xạ kết quả. Service không nhận `IFormFile` hoặc `HttpContext`.
4. Service kiểm tra tài khoản/quyền từ `ITaiKhoanHienTai`, nạp sân tracking qua repository, so sánh khóa DB với `AnhCu` bằng ordinal. Kiểm tra lại enum/hành động/file để chặn caller trực tiếp.
5. Giữ khóa file thường; thay ảnh gọi `LuuAsync(NhomAnh.San, ...)`, gỡ ảnh đặt giá trị mới null. Chỉ thay `DuongDanAnh` trên entity tracking, không `Update()` toàn Entity do client gửi.
6. Gọi `IDonViCongViec.LuuAsync`. EF so sánh cột ảnh cũ trong WHERE do mapping concurrency token. Bắt `LoiLuuTruException` với `XungDotPhienBan`, dọn ảnh mới nếu không tham chiếu rồi trả `XungDotAnh`.
7. Lưu thành công mới dọn khóa cũ đọc từ DB. Dọn thất bại vẫn trả thành công kèm `CanDonTep = true`. Nếu commit không rõ kết quả, giữ file mới để kiểm tra sau, không xóa mù hoặc báo thành công.
8. Khi xóa sân, chỉ dọn khóa ảnh sau khi xóa DB thành công; FK/lịch sử chặn xóa thì giữ ảnh. Khi sửa tên/giá/trạng thái không ghi đè cột ảnh. Tham khảo `LoaiSanService.XoaAsync` và `AnhService.CapNhatLoaiSanAsync`.

Phần cập nhật ảnh sân và test thuộc M2. Không thêm nó vào `IAnhService.CapNhatLoaiSanAsync`; có thể thêm phương thức rõ tên vào service sân hiện hành, dùng lại `KetQuaAnh`.

## 5. Đọc và hiển thị

- `GET /Anh/San/{id}`: Guest/Khách hàng chỉ thấy sân hoạt động thuộc loại sân hoạt động. Admin/Nhân viên đọc sân bị ẩn theo quyền quản trị hiện có.
- `GET /Anh/LoaiSan/{id}`: loại sân hoạt động công khai; loại ngừng hoạt động chỉ Admin xem.
- Không tồn tại/không có ảnh/không được xem/file mất đều 404. Trả JPEG với `nosniff` và `Cache-Control: private, no-store`; không expose thư mục upload qua static files.
- DTO đọc của M2 thêm thông tin có ảnh/khóa theo nhu cầu, không trả đường dẫn vật lý. Ví dụ gọi partial:

  ```cshtml
  @await Html.PartialAsync("_AnhDaiDien", new AnhDaiDienViewModel(
      NhomAnh.San, Model.MaSan, Model.TenSan, Model.DuongDanAnh is not null,
      ChiTiet: true))
  ```

  Dùng namespace `ViewModels` và `Services.Anh`. Kiểm tra chữ ký `AnhDaiDienViewModel` trong code khi tích hợp. Không gán ảnh loại sân làm ảnh sân, không tải lại CSS/JS đã có trong layout.

## 6. Migration và chạy local

Migration: `20261104074333_ThemAnhDaiDienSanVaLoaiSan`, sau `20261103132533_ThemGiaoDichVaBienNhanCoc`. Có file Designer và snapshot; cả `LoaiSan`/`SanTheThao` thêm nullable `nvarchar(255)` và concurrency token. Không thêm cột RowVersion, không reset DB. Down chỉ bỏ hai cột ảnh nên mất khóa ảnh nếu rollback; backup DB và kho file trước vận hành.

Các lệnh dưới đây dành cho thành viên chủ động chạy trên máy của mình, từ gốc repo:

```powershell
dotnet restore
dotnet build -c Release
dotnet ef database update --project QuanLyDatSan_UNETI5_DHTI17A4HN -- --environment Development
# Thay đường dẫn ví dụ bằng thư mục riêng ngoài repo và ngoài thư mục publish:
dotnet user-secrets set "LuuTruAnh:ThuMucGoc" "D:\DuLieuDatSan\Anh" --project QuanLyDatSan_UNETI5_DHTI17A4HN
dotnet run --project QuanLyDatSan_UNETI5_DHTI17A4HN -- --environment Development
```

Giữ connection string bằng User Secrets/biến môi trường hiện có, không gửi hoặc commit thông tin cá nhân. Chưa cấu hình kho: phần khác vẫn chạy, ghi ảnh báo kho chưa sẵn sàng. Không cần sửa appsettings để khai báo đường dẫn cá nhân. `git pull` không đem theo ảnh/upload từ máy khác.

Chạy thử Admin → Loại sân → Chi tiết → chọn một JPEG/PNG/WebP → Lưu ảnh → tải lại → thay ảnh → gỡ ảnh. Kiểm tra ở cửa sổ khách; đổi loại sang ngừng hoạt động phải trả 404 cho khách. M2 dùng ảnh thực tế được phép sử dụng; ảnh tổng hợp của test không đưa lên giao diện thật.

## 7. Bảo trì, kiểm thử và giới hạn

```powershell
# Chỉ báo cáo, không xóa:
dotnet run --project QuanLyDatSan_UNETI5_DHTI17A4HN -- --environment Development --kiem-tra-anh-mo-coi
# Người vận hành chủ động chạy sau khi xem báo cáo:
dotnet run --project QuanLyDatSan_UNETI5_DHTI17A4HN -- --environment Development --don-anh-mo-coi
```

Chỉ xóa file đúng mẫu, cũ hơn 24 giờ, không còn tham chiếu ở cả hai bảng. Bảo trì giữ khóa độc quyền và dừng nếu có thao tác ảnh đang chạy. Backup bao gồm cả DB và thư mục ảnh; không tự xóa ảnh khi deploy.

Test ảnh nằm ở `tests/QuanLyDatSan.M104.Tests/{KhoAnhTests,AnhHttpTests,AnhSqlTests}.cs`. Test SQL dùng DB ngẫu nhiên có guard và thư mục tạm, không sửa DB/kho ảnh ứng dụng. Bật `QLDS_M1_TEST_SQLSERVER=1` để chạy migration/concurrency ảnh và `QLDS_M2_TEST_SQLSERVER=1` cho hồi quy M2. M4 có test LocalDB theo fixture hiện hành.

M2 cần bổ sung test upload/thay/gỡ sân, quyền ghi Admin/Nhân viên/khách, chống CSRF, form cũ, hai yêu cầu tranh chấp, giữ ảnh khi sửa thông tin/xóa bị FK chặn, đọc ảnh sân bị ẩn và lỗi dọn file. Nền đã có test concurrency token sân, nhưng chưa thay thế test service ghi ảnh sân của M2.

## 8. Kết quả kiểm tra ngày 04/11/2026

- Build Release solution đạt. Còn hai warning CS0219 đã có ở `Views/ThongKe/Index.cshtml` (`tenCheDo`, `nhanCheDo`), không thuộc thay đổi ảnh.
- Toàn solution: **534/534 đạt, không skip** — M103: 107; M104: 52; M201: 138; M4: 237. Đã bật hai cờ SQL Server nói trên và dùng database test riêng.
- Bao phủ chuẩn hóa/metadata/giới hạn ảnh, path traversal, cấu hình sai, khóa bảo trì, HTTP upload/thay/gỡ loại sân, chống CSRF, quyền đọc/ghi, migration nâng cấp, cập nhật đồng thời, dọn file khi lỗi và token ảnh sân.
- Test migration cọc do M1 viết được chỉnh fixture để INSERT sân bằng các cột tồn tại ở schema cũ, trước khi chạy migration mới. Không đổi nghiệp vụ hoặc service M4.
- `dotnet ef migrations has-pending-model-changes`: không có thay đổi model chưa được migration. Đã xem SQL nâng cấp: chỉ hai cột ảnh và đặt null cột mới ở các dòng seed loại sân. Không áp dụng migration vào database ứng dụng.
- NuGet audit gồm dependency gián tiếp không báo package có lỗ hổng tại lần kiểm tra này. Package ảnh khai báo giấy phép Apache-2.0 trong metadata NuGet.
- Edge headless/DevTools: trang chủ 1440 px và 390 px, chi tiết loại sân 390 px, ảnh/fallback hiển thị và không tràn ngang. Preview dùng TestServer, SQLite RAM và ảnh tổng hợp trong thư mục tạm; không coi đây là test thao tác trình duyệt trên DB ứng dụng. Luồng POST thực tế được kiểm qua HTTP tests.
- Log/TRX/ảnh chụp cục bộ nằm dưới `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/m1-anh-*`, không commit. Chưa kiểm chứng restart/deploy trên máy khác; chưa nghiệm thu form/service ghi ảnh sân do M2 chưa triển khai.

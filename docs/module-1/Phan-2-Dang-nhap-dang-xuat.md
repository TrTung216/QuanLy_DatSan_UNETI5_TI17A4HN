# M1 — Phần 2: Đăng nhập, đăng xuất và phiên làm việc

Phụ trách: Trần Trọng Tùng — MSSV: **23103100202**. Codex hỗ trợ soạn mã và kiểm thử.

## Chức năng đã có

- Trang `/TaiKhoan/DangNhap` có kiểm tra dữ liệu ở server và thông báo tiếng Việt.
- Mật khẩu được băm/kiểm tra bằng `PasswordHasher<TaiKhoan>` của ASP.NET Core Identity; nâng cấp giá trị băm khi thư viện yêu cầu. Không Trim mật khẩu.
- Tên đăng nhập được Trim và so sánh theo collation không phân biệt hoa/thường đã tạo ở phần 1.
- Cookie xác thực đi cùng Session lưu `MaTaiKhoan`, `HoTen`, `VaiTro` và `MaPhien`. Mỗi lần đăng nhập có mã phiên mới.
- Mỗi yêu cầu có cookie hợp lệ đều kiểm tra tài khoản trong database. Tài khoản bị xóa/khóa, vai trò thay đổi hoặc Session không khớp sẽ phải đăng nhập lại. Họ tên được cập nhật trong Session và principal.
- Trang `/TaiKhoan/ThongTin` chỉ hiển thị thông tin của người đang đăng nhập. Không nhận mã tài khoản từ URL.
- Đăng nhập/đăng xuất dùng POST và antiforgery. Đăng xuất xóa Session và cookie xác thực.
- Chỉ cho phép ReturnUrl nội bộ. Form không hiển thị lại mật khẩu khi có lỗi.
- Giới hạn 10 POST đăng nhập/phút/IP; quá giới hạn trả HTTP 429. Trang từ chối quyền trả HTTP 403.
- Cookie HttpOnly, SameSite=Lax; ngoài Development yêu cầu HTTPS. Session hết hạn sau 30 phút không hoạt động; cookie có thời hạn 30 phút và tự gia hạn khi sử dụng.

## File chính

| File | Trách nhiệm |
| --- | --- |
| `Controllers/TaiKhoanController.cs` | Nhận form, kiểm tra mật khẩu, đăng nhập/đăng xuất và kiểm tra ReturnUrl. |
| `ViewModels/DangNhapViewModel.cs` | Các trường được nhận từ form và quy tắc kiểm tra. |
| `Services/PhienDangNhap.cs` | Tạo principal, đồng bộ Session, kiểm tra lại tài khoản mỗi yêu cầu. |
| `Services/TaiKhoanHienTai.cs` | Hợp đồng lấy mã tài khoản, họ tên, vai trò đã xác thực. |
| `Services/KhoiTaoAdmin.cs` | Lệnh tạo Admin đầu tiên; từ chối nếu đã có Admin hoặc trùng tên. |
| `Views/TaiKhoan/` | Đăng nhập, thông tin tài khoản và từ chối truy cập. |

File chung thay đổi: `Program.cs` đăng ký dịch vụ/middleware, `Views/Shared/_Layout.cshtml` thêm menu tài khoản. Không đổi entity, package hay migration.

## Tạo Admin đầu tiên và chạy thử

Cần cấu hình kết nối và chạy migration theo phần 1. Tại thư mục gốc repository, dùng PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet build
dotnet ef database update --project .\QuanLyDatSan_UNETI5_DHTI17A4HN

# Dữ liệu ví dụ; mật khẩu nhập kín và không ghi vào repo hay câu lệnh.
$env:AdminKhoiTao__TenDangNhap = 'admin'
$env:AdminKhoiTao__HoTen = 'Trần Trọng Tùng'
$env:AdminKhoiTao__Email = 'admin@example.test'
$matKhau = Read-Host 'Mật khẩu Admin (12–128 ký tự)' -AsSecureString
try {
    $env:AdminKhoiTao__MatKhau = [System.Net.NetworkCredential]::new('', $matKhau).Password
    dotnet run --no-build --no-launch-profile --project .\QuanLyDatSan_UNETI5_DHTI17A4HN -- --tao-admin
} finally {
    Remove-Item Env:AdminKhoiTao__MatKhau -ErrorAction SilentlyContinue
    $matKhau.Dispose()
}
Remove-Item Env:AdminKhoiTao__TenDangNhap,Env:AdminKhoiTao__HoTen,Env:AdminKhoiTao__Email
dotnet run --no-build --project .\QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http
```

Mở `http://localhost:5024/TaiKhoan/DangNhap`. Lệnh `--tao-admin` kết thúc ngay sau khi thực hiện; không khởi động web và không tự chạy migration. Lệnh trả exit code 1 nếu cấu hình sai, đã có Admin, trùng tên hoặc không ghi được database. Nó không đặt lại mật khẩu tài khoản có sẵn.

## Hợp đồng dùng chung cho M2–M5

Dùng cơ chế phân quyền ở server của ASP.NET Core:

```csharp
using Microsoft.AspNetCore.Authorization;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

// Bất kỳ tài khoản đã đăng nhập:
[Authorize]

// Chỉ Admin:
[Authorize(Roles = nameof(VaiTro.Admin))]

// Admin hoặc nhân viên:
[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
```

Các ví dụ trên là các lựa chọn riêng cho Controller/action, không gắn cả ba attribute cùng lúc.

Inject `ITaiKhoanHienTai` vào constructor để lấy `MaTaiKhoan`, `HoTen`, `VaiTro`. Mã tài khoản là null khi chưa đăng nhập. Khi xử lý hồ sơ/đơn của khách, lấy mã khách qua quan hệ với mã tài khoản hiện tại và giới hạn quyền sở hữu ngay trong truy vấn. Chỉ ẩn menu không đủ để phân quyền.

Middleware được sắp theo thứ tự `UseSession → UseAuthentication → UseAuthorization`. Không tự đặt giá trị Session từ form và không dùng Session làm nguồn quyền độc lập.

## Kiểm thử

```powershell
dotnet build --no-restore
powershell -NoProfile -File .\docs\module-1\KiemTraDangNhap.ps1
```

Script cần SQL Server LocalDB và dotnet-ef 10. Script tạo database có tên ngẫu nhiên `QuanLyDatSan_M1_LoginTest_<guid>`, dùng mật khẩu ngẫu nhiên, chạy web trên cổng loopback tạm, rồi dừng tiến trình và xóa đúng database đó. Log nằm trong `obj/` đã được Git bỏ qua. Không dùng database học tập hay database tích hợp cho script này.

Các kiểm tra bao gồm: tạo Admin và từ chối tạo lần hai; lưu mật khẩu băm; không cho khách xem thông tin; dữ liệu trống; sai mật khẩu; tài khoản không tồn tại; Trim/hoa thường; HttpOnly; chống giả mạo POST; GET không đăng xuất; cập nhật họ tên; đổi vai trò/khóa tài khoản; hash sai định dạng; ReturnUrl; mất Session; đăng xuất và phát lại cookie cũ; giới hạn đăng nhập; trang 403.

## Phạm vi phần tiếp theo

M1-03 đã thêm quản lý tài khoản/vai trò chỉ dành cho Admin, xem [Phần 3](Phan-3-Quan-ly-tai-khoan.md). Tài khoản khách tạo tại M1-03 chưa tự tạo hồ sơ KhachHang thuộc M3; chưa có đổi/khôi phục mật khẩu. Phiên bản quyền được thêm vào cookie để thu hồi phiên khi đổi vai trò/trạng thái qua M1-03, kể cả đã khôi phục giá trị cũ trước request tiếp theo.

Session và bộ đếm giới hạn đăng nhập đang lưu trong bộ nhớ của một tiến trình. Khởi động lại ứng dụng làm mất phiên. Khi triển khai nhiều máy hoặc qua reverse proxy, cần cấu hình kho Session dùng chung và địa chỉ proxy tin cậy; chưa có cấu hình đó trong phần này.

Khi bổ sung thao tác đổi/đặt lại mật khẩu ở phần sau, cần bổ sung cơ chế vô hiệu hóa các phiên khác của tài khoản. Hiện phần này kiểm tra trạng thái/vai trò và mã phiên, chưa theo dõi thay đổi mật khẩu của một phiên đã đăng nhập.

Tham khảo: [cookie authentication và ValidatePrincipal](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0), [rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).

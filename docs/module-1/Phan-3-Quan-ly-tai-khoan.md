# M1-03 — Quản lý tài khoản và phân quyền

Phụ trách: **Trần Trọng Tùng — MSSV 23103100202**. Codex hỗ trợ triển khai và kiểm thử. Đối chiếu checklist issue #9 được Tùng cung cấp: quản trị tài khoản/vai trò/trạng thái chỉ dành cho Admin, kiểm tra quyền phía server, thu hồi phiên khi quyền/trạng thái thay đổi.

## Chức năng

- Admin xem danh sách tài khoản, tìm theo tên đăng nhập/họ tên/email, lọc vai trò và trạng thái; phân trang 10 dòng, giữ bộ lọc khi chuyển trang.
- Admin tạo tài khoản với họ tên, email, một trong ba vai trò và mật khẩu ban đầu 12–128 ký tự có xác nhận. Server đặt trạng thái mới là hoạt động; không nhận mã tài khoản/trạng thái từ form tạo.
- Admin sửa họ tên/email, đổi vai trò, khóa hoặc mở khóa qua trang Cập nhật. Tên đăng nhập và mật khẩu không được thay đổi bởi form này. Không xóa cứng tài khoản.
- Không cho tự khóa hoặc hạ quyền tài khoản Admin đang đăng nhập; không cho mất Admin hoạt động cuối cùng. Có thể sửa họ tên/email của chính mình.
- Sau khi đổi vai trò hoặc trạng thái, mọi phiên cũ của tài khoản bị từ chối ở request tiếp theo, kể cả đã khôi phục vai trò hoặc mở khóa trước request đó. Sửa riêng họ tên/email không kết thúc phiên; tên hiển thị được đồng bộ bởi cơ chế M1-02.
- Tài khoản Khách hàng ở đây chỉ là tài khoản xác thực. Hồ sơ `KhachHang`, liên kết và nghiệp vụ đặt sân thuộc M3, chưa tự tạo hồ sơ giả. Không thêm đăng ký công khai hoặc đổi/khôi phục mật khẩu vào M1-03.

## Ma trận endpoint

| Controller/action | Phương thức | Quyền |
| --- | --- | --- |
| `QuanLyTaiKhoan/DanhSach` | GET | Admin |
| `QuanLyTaiKhoan/Tao` | GET, POST | Admin |
| `QuanLyTaiKhoan/Sua/{id}` | GET, POST | Admin |

Controller mới đặt `[Authorize(Roles = nameof(VaiTro.Admin))]` ở cấp lớp; không có action anonymous. Khách chưa đăng nhập được chuyển tới đăng nhập. Nhân viên/Khách hàng đã đăng nhập bị chuyển tới trang từ chối trả HTTP 403 theo cookie handler đang dùng. POST vẫn bị kiểm tra quyền ngay cả khi có anti-forgery hợp lệ.

Menu “Quản lý tài khoản” chỉ hiển thị cho Admin và active trên cả DanhSach/Tao/Sua. Các View dùng `_AdminLayout`, không sao chép header/footer; layout không cấp quyền server. `TaiKhoanController` vẫn giữ các chức năng M1-02 cho người dùng hiện tại.

## File thay đổi

Đường dẫn ứng dụng dưới `QuanLyDatSan_UNETI5_DHTI17A4HN/`:

| File | Trách nhiệm |
| --- | --- |
| `Controllers/QuanLyTaiKhoanController.cs` | Danh sách/projection, validation, tạo/sửa, transaction và quy tắc bảo vệ Admin. |
| `ViewModels/QuanLyTaiKhoanViewModel.cs` | Model riêng cho tạo/sửa/danh sách; không bind entity từ request. |
| `Views/QuanLyTaiKhoan/` | DanhSach, Tao, Sua, partial trường thông tin và `_ViewStart`. |
| `Views/QuanLyTaiKhoan/DanhSach.cshtml.css`, `wwwroot/js/quan-ly-tai-khoan.js` | CSS isolation giữ bảng dễ đọc khi cuộn ngang; chống gửi lặp form tạo/sửa và khôi phục nút qua lịch sử trình duyệt. |
| `Services/PhienBanQuyenTaiKhoan.cs` | Giữ phiên bản quyền theo tài khoản trong tiến trình, đổi phiên bản sau commit. |
| `Services/PhienDangNhap.cs` | Kiểm tra claim `PhienBanQuyen` bên cạnh Session, database và vai trò. |
| `Controllers/TaiKhoanController.cs` | Ghi claim phiên bản quyền lúc đăng nhập, giữ luồng đăng nhập/đăng xuất có sẵn. |
| `Program.cs` | Đăng ký singleton phiên bản quyền. |
| `Views/Shared/_Header.cshtml` | Thêm menu Admin và active cho các action quản lý tài khoản. |
| `wwwroot/css/layout.css`, `wwwroot/js/layout.js` | Đồng bộ breakpoint menu thành 1200px để đủ chỗ cho các mục Admin. |
| `Views/Shared/_ThongBao.cshtml` | Cập nhật chú thích key TempData đang dùng. |
| `tests/QuanLyDatSan.M103.Tests/` (tại gốc repo) | Kiểm thử HTTP tích hợp bằng cookie, Session, authorization và antiforgery thật với SQLite trong RAM. |
| File solution `.slnx` | Đưa project kiểm thử vào solution. |

Không thay Entity, DbContext, enum, schema hoặc migration. Không thay package của project ứng dụng. Các thay đổi `.csproj`, snapshot và migration `CapNhatModel` có sẵn trước đầu việc được giữ nguyên, không thuộc M1-03. Package kiểm thử chỉ nằm trong project test.

## Dữ liệu và xử lý cạnh tranh

- Mật khẩu dùng `IPasswordHasher<TaiKhoan>` đang có; không Trim mật khẩu, không hiển thị lại password trong form lỗi, không đưa hash ra View/danh sách.
- Tên đăng nhập Trim và so sánh trực tiếp cột `TenDangNhap` có collation `Vietnamese_100_CI_AS`, trùng không phân biệt hoa/thường nhưng phân biệt dấu. Kiểm tra trước khi lưu và xử lý lỗi unique SQL Server 2601/2627 để trả thông báo vào form.
- Required/độ dài/email/enum được kiểm tra ở server. Vai trò/trạng thái là nullable + Required + EnumDataType để từ chối cả trường bị bỏ trống và giá trị enum giả mạo. Form không bind mật khẩu/tên đăng nhập khi sửa, không bind mã/trạng thái khi tạo.
- Mỗi transaction ghi dùng `Serializable`: đọc tập Admin hoạt động, xác nhận lại người thực hiện còn là Admin hoạt động, kiểm tra quy tắc rồi mới lưu. Điều này giữ tập Admin ổn định khi có các thao tác hạ quyền/khóa đồng thời. SQL Server có thể chọn một transaction làm deadlock victim; mã 1205 được trả thành lỗi yêu cầu tải lại và thử lại, không báo thành công.
- POST thiếu anti-forgery bị từ chối; GET chỉ đọc. Mã không tồn tại trả 404; bộ lọc enum sai hoặc từ khóa dài quá 100 ký tự trả 400. Các trang quản trị có `ResponseCache(NoStore = true)`.
- Chỉ gán `TempData["Success"]` sau commit; lỗi validation hiển thị ở form, không giả báo thành công. Thông báo được Razor encode.

## Phiên đăng nhập

`PhienBanQuyenTaiKhoan` lưu một mã ngẫu nhiên cho từng tài khoản. Login đưa mã hiện tại vào claim. Sau commit thay đổi vai trò/trạng thái, mã được thay mới; `ValidatePrincipal` từ chối cookie mang mã cũ và xóa Session/cookie. Việc đổi lại về giá trị cũ không khôi phục phiên đã thu hồi. Kiểm tra tài khoản trong database ở M1-02 vẫn giữ nguyên để phát hiện khóa/xóa/đổi vai trò ngoài giao diện.

Phạm vi vận hành hiện tại vẫn là **một tiến trình**, phù hợp Session trong bộ nhớ đang dùng. Khởi động lại ứng dụng khiến người dùng đăng nhập lại. Cookie trước khi cập nhật M1-03 chưa có claim mới cũng phải đăng nhập lại. Không triển khai nhiều instance với cơ chế này; khi mở rộng cần kho Session và phiên bản quyền dùng chung hoặc security stamp lưu bền vững. Đổi rồi khôi phục bằng SQL trực tiếp trước khi có request không đi qua cơ chế tăng phiên bản: thao tác quản trị thường xuyên cần thực hiện qua Controller này.

## Chạy và kiểm tra

Tại thư mục gốc repo:

```powershell
dotnet build
dotnet test tests/QuanLyDatSan.M103.Tests --no-restore
dotnet run --no-build --project .\QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http
```

Ứng dụng thật mở tại `http://localhost:5024`. Dùng cấu hình kết nối và Admin đã có; vào menu **Quản lý tài khoản**. M1-03 không yêu cầu migration mới và không tự tạo/reset database. Không đưa connection string hoặc mật khẩu cá nhân vào repository.

Project test dùng `WebApplicationFactory` với endpoint/middleware/Razor thật, thay duy nhất provider database bằng SQLite trong RAM và Data Protection bằng provider tạm trong RAM. Tài khoản kiểm thử và mật khẩu ngẫu nhiên chỉ tồn tại trong test. Không thay authentication handler, không giả principal để bỏ qua đăng nhập và không dùng tài khoản/database thật. Schema test chỉ có bảng tài khoản phục vụ M1-03, không chạy migration các module khác.

Các ca kiểm thử bao gồm:

- Khách chưa đăng nhập, Nhân viên, Khách hàng không được GET/POST các URL quản trị trực tiếp; không thấy menu Admin.
- Admin tạo, sửa, đổi quyền, khóa/mở khóa; lọc/phân trang, trạng thái rỗng, link form với id từ query hoặc route.
- Tên trùng sau Trim/khác hoa thường; trường trống, sai email, mật khẩu ngắn/xác nhận không khớp, enum thiếu/sai; không ghi dữ liệu khi lỗi.
- Không để trường ngoài ViewModel thay mã/trạng thái tạo hoặc tên đăng nhập/mật khẩu sửa; kiểm tra hash mật khẩu và không lộ hash/password trong HTML.
- POST thiếu token, id không tồn tại, bộ lọc không hợp lệ.
- Bảo vệ Admin cuối cùng và không tự hạ quyền ngay cả khi có Admin thứ hai.
- Hai phiên của cùng tài khoản bị thu hồi sau đổi quyền/khóa rồi khôi phục; đăng nhập lại nhận quyền mới. Admin bị hạ quyền mất truy cập quản trị; đăng xuất kết thúc quyền truy cập.
- Sửa họ tên không đăng xuất; tài khoản bị khóa không thể đăng nhập.

Giới hạn: SQLite không thay thế kiểm thử collation/locking/deadlock/unique race trên SQL Server. Cần kiểm tra cạnh tranh trên database SQL Server kiểm thử được phép trước khi triển khai thực tế; chưa coi các nhánh lỗi SQL Server 1205/2601/2627 là đã được kiểm thử tích hợp bằng SQLite.

## Kết quả thực tế ngày 07/10/2026

- `dotnet build`: **thành công, 0 lỗi, 0 cảnh báo**. Restore ban đầu bị mạng sandbox chặn; build thành công khi có quyền truy cập NuGet.
- `dotnet test tests/QuanLyDatSan.M103.Tests --no-restore`: **22/22 đạt**, 0 bỏ qua. Kết quả TRX cục bộ ở `tests/QuanLyDatSan.M103.Tests/obj/results/m103.trx` (Git bỏ qua).
- Đã chạy bản ứng dụng kiểm thử qua Kestrel loopback với SQLite trong RAM, dùng Chrome headless ở **1440, 1024, 390, 320px** cho DanhSach/Tao/Sua: không tràn ngang toàn trang, bảng cuộn riêng, active đúng menu, menu thu gọn/Escape hoạt động.
- Đã thao tác thật trong trình duyệt: đăng nhập Admin, tạo tài khoản, cập nhật sang trạng thái khóa, thấy thông báo sau lưu, đăng xuất và bị yêu cầu đăng nhập lại; Nhân viên/Khách hàng không thấy menu quản trị và URL trực tiếp trả trang 403. Lượt cuối không có lỗi JavaScript; đã xem ảnh desktop và mobile. Ảnh cục bộ ở `tests/QuanLyDatSan.M103.Tests/obj/ui-preview/screenshots/`.
- Bộ test không sử dụng tài khoản hay database SQL Server thật. Các tình huống đặc thù SQL Server nêu ở phần giới hạn vẫn cần kiểm tra trên môi trường kiểm thử được cho phép; không chạy migration hoặc tạo/reset database thật trong đầu việc này.
- `git diff --check` đạt. Checksum xác nhận cả bốn file có thay đổi từ trước (project `.csproj`, snapshot và hai file migration `CapNhatModel`) giữ nguyên từng byte. Tiếp tục trên nhánh đang có `feature/m1-layout-menu` vì working tree chưa sạch; không tự đổi nhánh, commit/push/PR/merge.

Phần chức năng và kiểm thử tự động M1-03 đã triển khai. Việc review/approve của thành viên khác, tạo PR và merge vẫn thuộc bước bàn giao; không tự đóng issue #9 hoặc báo đã merge.

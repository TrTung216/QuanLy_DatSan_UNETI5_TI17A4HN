# M1-06 — Tích hợp menu theo vai trò

Trần Trọng Tùng — MSSV 23103100202. Codex hỗ trợ triển khai và kiểm thử ngày 18/10/2026.

Issue: [M1-06 #12](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/12).
Liên quan: [M1-05 #11](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/11).
Nền kiểm tra: main `ce5de11` sau merge PR #51, có các sửa hồ sơ M1 từ trước trong working tree.

## Thay đổi trong đợt này

- Tách cấu hình menu khách khỏi Razor sang `Services/MenuKhachHang.cs`, dùng lại `MucMenu` hiện có. Thêm Danh sách sân cho khách chưa đăng nhập và Khách hàng, trỏ tới GET `SanTheThao/Index` đã tồn tại.
- Header khách dùng cấu hình này; bấm tên người đăng nhập để mở `TaiKhoan/ThongTin`. Giữ Đăng ký bên phải Đăng nhập và đăng xuất POST có anti-forgery.
- Đổi breakpoint navbar khách từ lg sang xl để khớp CSS/JS chung dưới 1200px, tránh menu dài chen chúc trên tablet.
- `MenuQuanTri` chỉ trả menu cho principal đã xác thực là Admin/NhanVien. Đơn đặt sân giữ active cả khi POST XacNhan/TuChoi/Huy trả lại View chi tiết do lỗi; không tạo liên kết GET tới action POST.
- Cập nhật hướng dẫn layout theo hai layout độc lập hiện tại, bảng route/vai trò, TempData và phần chưa triển khai. Giữ ghi nhận đóng góp M3/M4; không nhận lại phần việc đã có.

Không sửa Controller, nghiệp vụ, phân quyền server, Entity, DbContext, migration hay package trong đợt menu này. Các thay đổi hồ sơ/tài khoản và package vốn có ở local được giữ nguyên, không phải thay đổi của M1-06. Không tạo/reset database người dùng, không commit/push/tạo PR trong đợt này.

## Kiểm tra đã thực hiện

| Kiểm tra | Kết quả và phạm vi |
| --- | --- |
| `dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release --no-restore` | Thành công, 0 lỗi, 0 cảnh báo trong lần build này. Không phải kết quả audit bảo mật NuGet. |
| `dotnet test tests/QuanLyDatSan.M103.Tests/QuanLyDatSan.M103.Tests.csproj -c Release --no-restore` | 75/75 đạt, không bỏ qua; gồm 15 ca mới trong `MenuVaiTroTests` và 60 ca tài khoản/service/hồ sơ đang có tại local. |
| Hợp đồng route và quyền | Duyệt MVC action descriptors cho từng menu của Guest/KhachHang/NhanVien/Admin: đích GET tồn tại, không cần ID bắt buộc, principal phù hợp được authorization policy cho phép. |
| HTTP tích hợp | Dùng WebApplicationFactory với cookie/Session thật, tài khoản test trong SQLite RAM. Kiểm tra HTML menu 4 trạng thái; đăng nhập, POST đăng xuất thiếu token trả 400, token hợp lệ đăng xuất và trang tài khoản yêu cầu đăng nhập lại cho cả 3 vai trò. |
| Active và role giả chưa xác thực | Nhận đúng một mục Đơn đặt sân tại ChiTiet/XacNhan/TuChoi/Huy; không nhầm Controller khác. Role claim không có xác thực không làm hiện menu riêng. |
| Razor render | 8 trường hợp: 4 trạng thái × View có/không có Styles/Scripts. Đúng layout, một header/main/footer, section render một lần, TempData được mã hóa, đăng xuất có token. |
| Chrome headless/Playwright | 20 trường hợp: 4 trạng thái × 1440/1200/1024/390/320px. Không tràn ngang kể cả tên tài khoản dài; menu công khai/thu gọn, Escape khi focus trong menu và trả focus, nút đăng nhập/đăng ký hoặc đăng xuất. JS đăng xuất chặn gửi lặp và khôi phục qua pageshow. Không lỗi JavaScript hoặc thiếu tài nguyên được tải. |
| Xem ảnh và diff | Đã xem ảnh khách hàng desktop/mobile; đã rà diff và `git diff --check`. |

## Giới hạn và nghiệm thu

- Trình duyệt dùng HTML Razor render với principal mô phỏng và CSS/JS thật; không phải đăng nhập bằng tài khoản SQL Server thực. HTTP đăng nhập/đăng xuất được kiểm thử riêng bằng middleware thật với SQLite RAM.
- Không bấm qua toàn bộ luồng nghiệp vụ M2–M5 trên database thật, không kiểm thử lại transaction/concurrency SQL Server trong đợt menu. Bộ test route/policy không chứng minh dữ liệu của từng màn hình luôn hợp lệ.
- Kiểm thử giao diện và ảnh tạm nằm trong `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/layout-check/`; công cụ render/browser ở `obj/` được Git bỏ qua. Không đưa dữ liệu mô phỏng hoặc dependency Playwright vào ứng dụng.
- Phần code/tài liệu M1-06 đã được kiểm tra tại local. Issue còn cần PR, thành viên khác review/approve và Tùng merge bằng **Create a merge commit** theo quy ước; không tự đóng Issue bằng kết quả local.

## Chạy thử để review

1. Build Release rồi chạy `dotnet run -c Release --no-build --project .\QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http` từ gốc repo. Mở `http://localhost:5024` với cấu hình hiện có.
2. Khi chưa đăng nhập: Trang chủ, Danh sách sân; Đăng nhập và Đăng ký ở khu tài khoản. Vào chi tiết sân, mục Danh sách sân phải active.
3. Đăng nhập Khách hàng: có Trang chủ, Danh sách sân, Đặt sân, Lịch của tôi, Hồ sơ. Bấm tên trên header để xem thông tin tài khoản.
4. Đăng nhập Nhân viên/Admin: có sân, thêm sân, đơn đặt sân và thông tin tài khoản; chỉ Admin có loại sân, dịch vụ, quản lý tài khoản. Mở đơn hợp lệ và kiểm tra active cả khi form xử lý đơn có lỗi validation.
5. Thu nhỏ trình duyệt; Tab vào menu rồi Escape, kiểm tra focus và không tràn ngang. Đăng xuất và thử vào lại trang tài khoản phải được yêu cầu đăng nhập. Thử URL quản trị bằng Khách hàng phải bị chặn.

Hướng dẫn tích hợp cho các thành viên: [Hướng dẫn sử dụng layout](../Huong_dan_su_dung_layout.md).

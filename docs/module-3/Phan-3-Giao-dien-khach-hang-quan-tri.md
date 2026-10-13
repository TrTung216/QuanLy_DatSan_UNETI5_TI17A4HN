# M3 — Phần 3: Giao diện khách hàng và khu vực quản trị

Phụ trách: Nguyễn Văn Quý — MSSV: **23103100181**.

Đề xuất này có chạm vào file của M1 (Tùng): `_ViewStart.cshtml`, `_AdminLayout.cshtml`, `TaiKhoanController.cs`, `Home/Index.cshtml`. Cần Tùng review trước khi merge theo hướng dẫn layout chung.

## 1. Ba giao diện theo người dùng

| Người dùng | Layout | Trang đầu sau đăng nhập |
| --- | --- | --- |
| Khách chưa đăng nhập | `_LayoutKhachHang` | Trang chủ giới thiệu, có Đăng ký và Đăng nhập |
| Khách hàng | `_LayoutKhachHang` | `/KhachHang/TrangChu` |
| Admin, Nhân viên | `_AdminLayout` (thanh bên) | `/QuanTri/Index` |

`_ViewStart` chọn layout theo vai trò đã xác thực. `_Layout` và `_Header` của M1 được giữ nguyên, hiện không còn layout nào dùng làm cha; Tùng quyết định giữ hay bỏ.

Nếu đăng nhập có `ReturnUrl` cục bộ thì vẫn quay lại URL đó; chỉ khi không có mới chuyển theo vai trò (`VeTrangSauDangNhap` trong `TaiKhoanController`).

## 2. Giao diện khách hàng
- Menu khách chưa đăng nhập: Trang chủ, Đăng ký, nút Đăng nhập.
- Menu khách hàng: Trang chủ, Đặt sân, Lịch của tôi, Hồ sơ, tên và vai trò, Đăng xuất (form POST có anti-forgery, id `dang-xuat` để `layout.js` chống gửi lặp).
- `/KhachHang/TrangChu`: số đơn chờ xác nhận, số đơn đã xác nhận sắp tới, điểm tích lũy, thao tác nhanh, tối đa 3 đơn sắp tới. Số liệu lấy từ database, chỉ của khách đang đăng nhập.
- Khách có hồ sơ bị khóa thấy thông báo ngay trên trang chủ.

## 3. Giao diện quản trị
- Thanh bên chia nhóm: Tổng quan, Vận hành, Danh mục, Tài khoản; nhóm không có mục nào thì ẩn. Dưới 992px thanh bên thành menu trượt (offcanvas).
- `/QuanTri/Index`: số đơn chờ xác nhận, khách hàng hoạt động, sân đang hoạt động, và lối vào các chức năng theo nhóm. Không đếm bảng `DichVu` vì bảng này chưa có migration trên `main`.
- **Một nguồn menu duy nhất**: `Services/MenuQuanTri.cs` dùng cho cả thanh bên lẫn trang điều hành. Thêm chức năng mới bằng một dòng `MucMenu` vào đúng nhóm, điều kiện hiển thị khớp `[Authorize]` của action. Nhóm "Vận hành" hiện trống để M2/M3/M4 bổ sung khi có màn hình quản lý.
- Nhóm "Tài khoản" có "Quản lý tài khoản" (chỉ Admin, của M1-03) và "Thông tin tài khoản"; điều kiện hiển thị khớp `[Authorize(Roles = Admin)]` của `QuanLyTaiKhoanController`.
- Không có link giả: chỉ các action đã tồn tại.

## 4. Thông báo
`_ThongBao` của M1 đọc các key `Success`, `Error`, `Warning`, `Info`. Các controller M3 đã đổi từ `ThanhCong`/`ThatBai` sang `Success`/`Error`; nếu không đổi thì thông báo của M3 không hiển thị.

## 5. Tài nguyên mới
- `wwwroot/css/thanh-phan.css`: thẻ số liệu, ô thao tác nhanh, banner, các bước, khung form.
- `wwwroot/css/quan-tri.css`: khung quản trị.
- Nhãn trạng thái đơn dùng lớp `ql-status` của M1 (`HienThiDatSan.LopMau`).

## 6. Kiểm thử thủ công
1. Chưa đăng nhập: Trang chủ, Đăng ký, Đăng nhập; vào thẳng `/DonDat/Tao` bị chuyển tới đăng nhập, đăng nhập xong quay lại `/DonDat/Tao`.
2. Đăng ký rồi đăng nhập bằng tài khoản khách: vào `/KhachHang/TrangChu`; kiểm tra menu; thông báo "Đăng ký thành công" hiện ở trang đăng nhập.
3. Khách hàng truy cập `/QuanTri/Index` và `/DichVu/DanhSach` → trang 403.
4. Đăng nhập Admin → `/QuanTri/Index`, thanh bên có "Dịch vụ"; Nhân viên không thấy mục này và gọi `/DichVu/DanhSach` bị 403.
5. Admin hoặc Nhân viên mở `/DonDat/Tao` → 403.
6. Đăng xuất ở cả hai giao diện; nhấn Back không xem lại được trang cần đăng nhập.
7. Kiểm tra 1440, 1024, 390, 320px: menu khách thu gọn, thanh bên quản trị mở bằng nút Menu và đóng được, không tràn ngang.
8. Tạo đơn, sửa hồ sơ: thông báo thành công hiện đúng ở layout khách.

## 7. Điều chỉnh sau review 07/10/2026
- Đã tích hợp `origin/main` (có M1-03); giữ cơ chế thu hồi phiên: `TaoPrincipal(taiKhoan, maPhien, phienBanQuyen.Lay(...))` kết hợp điều hướng theo vai trò.
- Migration `CapNhatModel` (tạo trùng `SanTheThao`) được thay bằng `ThemKhachHangVaDatSan`, chỉ tạo `KhachHang` và `DatSan`.
- CSS quản trị nằm đúng `wwwroot/css`; đã xóa thư mục `Views/wwwroot`.
- Ngày sinh dùng `input type="date"` (giá trị `yyyy-MM-dd`, `min`/`max`) ở cả đăng ký và sửa hồ sơ; kiểm tra phía server giữ nguyên.
- Không đưa thay đổi `.csproj` (nâng gói EF) vào PR M3.

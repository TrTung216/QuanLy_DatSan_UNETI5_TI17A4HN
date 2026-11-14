# Tổ chức ViewModel

ViewModel được nhóm theo chức năng giao diện. Namespace theo đường dẫn thư mục, ví dụ `QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.TaiKhoan`.

| Thư mục | Phạm vi | Phụ trách/phối hợp |
|---|---|---|
| `Chung/Anh` | Form cập nhật ảnh và dữ liệu partial ảnh sân/loại sân | M1 chủ trì, M2 sử dụng |
| `Chung` | Dữ liệu trang lỗi dùng chung (`ErrorViewModel`) | M1 |
| `TaiKhoan` | Đăng nhập, quản lý tài khoản | M1 |
| `LoaiSan` | Form, danh sách và chi tiết loại sân | M1 |
| `TrangChu` | Trang chủ công khai và sân theo loại | M1 |
| `SanTheThao` | Thêm/sửa, danh sách, chi tiết, tra cứu sân | M2 |
| `KhachHang` | Đăng ký, hồ sơ và trang chủ khách hàng | M3 |
| `DonDat/KhachHang` | Tạo đơn, lịch đặt, chi tiết, hủy cá nhân và thông tin cọc | M3, có đóng góp M4 |
| `DonDat/QuanLy` | Danh sách, chi tiết, xác nhận, từ chối, hủy, ghi cọc và hoàn thành đơn | M4, có đóng góp M3 |
| `DonDat/Chung` | Ánh xạ lỗi đặt sân dùng cho cả giao diện khách và quản lý | M4 chủ trì, phối hợp M1/M3 |
| `DichVu` | Quản lý dịch vụ và dịch vụ trong đơn | M5 |
| `ThongKe` | Dữ liệu trang thống kê | M5 |
| `DieuHanh` | Dữ liệu trang điều hành Admin/nhân viên | M3 hiện triển khai; phối hợp khi sửa |

## Khi bổ sung hoặc sửa

- Đặt file vào nhóm chức năng tương ứng. Chỉ đưa vào `Chung` khi có nhu cầu sử dụng chung thực tế; dùng ở nhiều màn hình của một module chưa có nghĩa là dùng chung toàn ứng dụng.
- Cập nhật `using`, `@model`, `@using` và test liên quan khi chuyển namespace. Không thêm tất cả namespace vào `_ViewImports.cshtml` chỉ để tránh sửa caller.
- Giữ thông tin tác giả và phần đóng góp trên file. Thư mục thể hiện phạm vi chức năng, không thay thế lịch sử đóng góp.
- Các DTO/hợp đồng service tiếp tục nằm ở `Services`; Entity EF nằm ở `Models`. ViewModel phục vụ form, validation định dạng và dữ liệu hiển thị, theo [quy ước Controller/Service/Repository](../../docs/Quy_uoc_Controller_Service_Repository.md).
- Thay đổi phần dùng chung cần trao đổi với người chủ trì và kiểm tra các module sử dụng.

## Lịch sử sắp xếp và refactor

PR #73 chỉ chuyển file, namespace và các tham chiếu; không đổi tên lớp, thuộc tính, validation, kế thừa, công thức tiền hoặc nghiệp vụ.

Đợt kế tiếp ngày 12/11/2026:

- `ChiTietDonQuanLyViewModel` độc lập với `ChiTietDonViewModel`: mỗi màn hình ánh xạ DTO vào dữ liệu riêng, không kế thừa các trường hủy/cọc của khách hàng.
- `XuLyDonViewModel` chứa `MaDon`, `PhienBan`, `QuayLai` và validation chung cho xác nhận, ghi cọc, hoàn thành, hủy/từ chối của quản lý. `TinhTrangCoc` chỉ thuộc `XacNhanDonViewModel`.
- `ConPhaiThu`, `TienCocToiThieu` chỉ nhận kết quả service; không viết công thức trong ViewModel. Hai khoản này có thể null khi dữ liệu tiền sai, đi cùng `ThongBaoLoiTien`; giao diện không đổi null thành số 0.
- `ThanhToanDonViewModel.cs` vẫn chứa `GhiCocViewModel`/`HoanThanhDonViewModel`. `HienThiCoc` vẫn nằm trong `CocDonViewModel.cs`; `ThongBaoLoiDatSanViewModel` giữ tên hiện có. Tách helper là đầu việc riêng.

Cả hai đợt không thay schema và không cần migration. Chi tiết hợp đồng tiền/DTO xem [hợp đồng M3/M4/M5](../../docs/Hop_dong_M3_M4_M5.md).

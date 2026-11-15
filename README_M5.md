# M5 — Thay đổi dịch vụ trong đơn và kiểm tra tiền

## Nội dung đã thay đổi

| File | Trước | Sau |
|---|---|---|
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyDichVuTrongDon/DichVuTrongDonService.cs` | Cho thêm/sửa/hủy dịch vụ ở cả `ChoXuLy` và `DangXuLy`; hủy xóa dòng không lưu dấu vết; kiểm tra RowVersion chỉ dựa trên bản ghi được tải lúc POST. | Chỉ cho phép thay đổi khi `DangXuLy`; kiểm tra người gọi có quyền Admin/Nhân viên đang hoạt động; so sánh phiên bản gửi từ form; khóa sân và cập nhật đơn trong cùng transaction theo thứ tự M4. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Repositories/QuanLyDichVuTrongDon/DichVuTrongDonRepository.cs` | Xóa cứng dòng hủy; không có kiểm tra quyền, khóa sân, đọc phiên bản form hay truy vấn lịch sử. | Lưu bản chụp dòng hủy vào `LichSuHuyDichVu` trước khi xóa dòng hoạt động; đọc lịch sử; kiểm tra quyền từ tài khoản đang hoạt động; dùng khóa sân `UPDLOCK, ROWLOCK` và RowVersion. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Repositories/QuanLyDichVuTrongDon/IDichVuTrongDonRepository.cs` | Hợp đồng repository chỉ hỗ trợ đọc, xóa dòng và tính tiền. | Bổ sung hợp đồng quyền, khóa sân, phiên bản và đọc/ghi lịch sử hủy. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyDichVuTrongDon/IDichVuTrongDonService.cs` | Các lệnh ghi không nhận phiên bản form; DTO không mang phiên bản hoặc thông tin lịch sử. | Các lệnh ghi nhận phiên bản; DTO mang phiên bản để gửi lại và thông tin dòng đã hủy. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/ViewModels/DichVuTrongDonViewModels.cs` | Form thêm/sửa không mang RowVersion. | Thêm trường `PhienBan` bắt buộc cho POST thêm/sửa. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Controllers/DichVuTrongDonController.cs` | POST chỉ dựa vào quyền Controller. | Giữ chặn quyền Controller, truyền RowVersion vào Service và trả `Forbid` nếu Service từ chối quyền. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Views/DichVuTrongDon/Index.cshtml` | Không gửi RowVersion; không hiển thị dấu vết hủy. | Gửi RowVersion trong mọi form thay đổi; hiển thị dòng lịch sử hủy, thời điểm UTC và mã tài khoản thực hiện; ẩn thao tác trên lịch sử. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyTinhTien/TinhTienService.cs` | `TinhTongTien` cộng thẳng hai giá trị, không từ chối tiền âm hoặc tổng ngoài miền lưu trữ. | Từ chối khoản âm, khoản riêng vượt `decimal(18,2)`, tổng vượt miền lưu trữ và khoản có quá hai chữ số thập phân. |
| `QuanLyDatSan_UNETI5_DHTI17A4HN/Migrations/20261113210000_M5LuuLichSuHuyDichVu.cs` | Không có nơi lưu lịch sử hủy. | Tạo bảng audit lưu đơn, mã dòng gốc, bản chụp dịch vụ/số lượng/đơn giá, thời điểm UTC và tài khoản hủy. |
| `tests/QuanLyDatSan.M5.Tests/DichVuTrongDonServiceTests.cs` | Chưa có test cho hợp đồng chỉnh sửa dịch vụ trong đơn. | Bổ sung kiểm tra từ chối `ChoXuLy`, lịch sử hủy, thêm lại dịch vụ, RowVersion cũ và phân quyền Service. |
| `tests/QuanLyDatSan.M5.Tests/TinhTongTienValidationTests.cs` | Chưa kiểm tra tiền âm và giới hạn `decimal(18,2)` của tổng tiền. | Bổ sung kiểm tra khoản âm, giới hạn tổng và giá trị đúng tại biên. |

## Lưu ý triển khai

- Hủy vẫn xóa dòng đang hoạt động để giữ nguyên unique index hiện có cho cặp đơn/dịch vụ. Bản chụp xóa được giữ riêng trong `LichSuHuyDichVu`, vì vậy cùng dịch vụ có thể được thêm lại mà không mất lịch sử cũ.
- Migration mới cần được áp dụng trước khi sử dụng tính năng hủy sau khi cập nhật ứng dụng.
- Không sửa `ApplicationDbContext` hoặc các luồng M3/M4; bảng lịch sử được thao tác trực tiếp trong transaction M5.

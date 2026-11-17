# Khách hàng thêm dịch vụ vào đơn

Ngày tích hợp: 16/11/2026. Trần Trọng Tùng (23103100202, M1) tích hợp theo yêu cầu,
tiếp nối hai phương thức repository do M5 bổ sung. M3 phụ trách giao diện khách hàng;
M5 phụ trách nghiệp vụ và repository dịch vụ. Giữ ghi nhận tác giả các phần đã có.

## Luồng sử dụng

1. Khách đăng nhập, vào **Lịch của tôi → Chi tiết đơn**.
2. Với đơn **Đã xác nhận**, chọn **Thêm dịch vụ**.
3. Chọn dịch vụ đang hoạt động, số lượng 1–100, gửi form.
4. Thành công: chuyển về danh sách dịch vụ của đơn, hiện thông báo và tổng tiền dịch vụ mới.
5. Khách chỉ thêm/xem; liên hệ nhân viên để sửa số lượng hoặc hủy. Đơn hoàn thành,
   đã hủy, từ chối hoặc đang chờ không được thêm dịch vụ.

Thêm cùng dịch vụ sẽ cộng số lượng (tổng không quá 100), giữ đơn giá snapshot của dòng cũ.
Dòng mới lấy giá danh mục ở máy chủ. Không thay tiền cọc/chính sách cọc, không chốt
`TongTien` trước khi hoàn thành. Tiền dịch vụ dùng `ITinhTienService` hiện có; tiền sân,
dịch vụ và phần còn phải thu khi hoàn thành vẫn do luồng chung M4/M5 xử lý.

## Hợp đồng và file

- `IDichVuTrongDonRepository`: giữ nguyên hai chữ ký M5 thêm, triển khai trong
  `DichVuTrongDonRepository`. Lọc mã đơn, tài khoản chủ đơn, vai trò khách hàng,
  trạng thái hoạt động của tài khoản và hồ sơ.
- `IDichVuTrongDonService.LayChoKhachHangAsync(maDatSan, cancellationToken)`:
  trả DTO đọc hoặc null nếu không có quyền/không tồn tại.
- `KhachHangThemAsync(maDatSan, maDichVu, soLuong, phienBan, cancellationToken)`:
  lấy danh tính qua `ITaiKhoanHienTai`; trả `KetQuaNghiepVu` hiện có.
  `KhongTimThay` được ánh xạ HTTP 404; lỗi validation/nghiệp vụ hiện lại form;
  thành công dùng TempData `Success` và redirect. Không thêm bộ mã lỗi song song.
- `DonDatController.DichVu.cs`: GET `/DonDat/DichVu/{id}` và POST
  `/DonDat/ThemDichVu`, kế thừa phân quyền khách hàng của controller partial.
- Form POST chỉ dùng `Form.MaDatSan`, `Form.MaDichVu`, `Form.SoLuong`,
  `Form.PhienBan` và antiforgery token. Không nhận giá hoặc chủ đơn làm căn cứ tin cậy.
- `DichVuDonKhachHangViewModel`, `Views/DonDat/DichVu.cshtml`: form riêng và
  bảng dịch vụ, không truy vấn DB hoặc tự tính tổng tiền.
- `Views/DonDat/ChiTiet.cshtml`: liên kết vào form. `MenuKhachHang`: giữ mục
  **Lịch của tôi** đang chọn trên GET và POST lỗi.

Thao tác ghi dùng chung transaction M5: khóa sân → nạp lại đơn giới hạn chủ sở hữu
và quyền hiện tại → kiểm tra trạng thái/RowVersion → cập nhật dịch vụ → tính tiền
→ đánh dấu cập nhật phiên bản đơn → Unit of Work lưu. Hai lần gửi cùng phiên bản
không được âm thầm cộng thêm. Controller quản lý của nhân viên/Admin giữ nguyên quyền.

Không đổi Entity/schema, không cần migration. Không cần đăng ký DI mới.

## Kiểm thử

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release --no-restore
dotnet test tests/QuanLyDatSan.M103.Tests/QuanLyDatSan.M103.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~KhachHangThemDichVuTests|FullyQualifiedName~DonDatCocControllerTests|FullyQualifiedName~MenuVaiTroTests'
```

Kết quả tại lần tích hợp: build thành công; **60/60** test đạt, không bỏ qua.
Kiểm thử mới gồm service, giá snapshot/cọc, gửi lặp phiên bản, trạng thái/số lượng,
chủ đơn thay đổi sau khóa, truy vấn repository thực tế trên SQLite RAM, HTTP với
đăng nhập/antiforgery/Razor thật, giá client giả, form lỗi và menu active.
Kiểm thử HTTP dùng repository/Unit of Work giả cho thao tác dịch vụ; không tương đương
kiểm thử khóa, rollback hay tranh chấp trên SQL Server.

Chưa kiểm thử đồng thời trên SQL Server hoặc trực quan trình duyệt desktop/mobile
cho trang mới. Không tạo/reset database ứng dụng để chạy các test trên.
Trước nghiệm thu nhóm cần kiểm tra hai request thêm cùng phiên bản, thêm đồng thời
với hoàn thành đơn, đối chiếu tiền ở trang nhân viên và khách, kiểm tra bố cục trên điện thoại.

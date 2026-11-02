# M1 — Tạo và đồng bộ hồ sơ từ quản lý tài khoản

Người phụ trách: Trần Trọng Tùng — MSSV 23103100202.

## Vấn đề và hành vi sau sửa

Trước đây Admin tạo tài khoản vai trò KhachHang chỉ lưu TaiKhoan, khiến khách đăng nhập nhưng không có hồ sơ để sử dụng M3. Khi Admin sửa họ tên/email, hồ sơ cũ không được cập nhật tương ứng.

- Admin tạo khách phải cung cấp số điện thoại hợp lệ; tài khoản và hồ sơ được lưu cùng transaction. Họ tên/email dùng chung; NgayDangKy lấy từ IDongHo; điểm ban đầu 0, hồ sơ hoạt động. Không tạo số điện thoại giả.
- Khi sửa tài khoản khách cũ chưa có hồ sơ, hoặc chuyển nhân viên thành khách, màn hình yêu cầu số điện thoại để bổ sung hồ sơ trong cùng transaction.
- Khi hồ sơ đã có, M1 chỉ đồng bộ HoTen/Email. Giữ số điện thoại, ngày sinh, điểm, trạng thái hồ sơ, địa chỉ, ghi chú và lịch sử. Đổi vai trò khỏi khách không xóa hồ sơ; khóa/mở khóa tài khoản không tự khóa/mở khóa hồ sơ M3.
- Quyền Admin, bảo vệ Admin cuối/tự khóa, isolation Serializable và thu hồi phiên sau commit vẫn được kiểm tra như trước.
- Program.cs chỉ đăng ký IDongHo một lần; IDatSanService dùng đăng ký đã có trong AddUngDungServices, không đăng ký lặp trong Program.

## Khắc phục tài khoản cũ, ví dụ mã 1002

1. Dừng rồi chạy lại ứng dụng để nạp code mới (ứng dụng đang chạy vẫn dùng code cũ).
2. Đăng nhập bằng Admin; mở Quản lý tài khoản → Sửa tài khoản cần bổ sung, hoặc `/QuanLyTaiKhoan/Sua/1002` nếu đúng tài khoản trong database của bạn.
3. Giữ vai trò Khách hàng, kiểm tra họ tên/email và nhập số điện thoại thật vào trường tạo hồ sơ.
4. Lưu, sau đó dùng tài khoản khách kiểm tra lại Hồ sơ và Đặt sân.

Không tự sửa dữ liệu tài khoản 1002 trong database của người dùng ở lần triển khai này vì chưa có số điện thoại thật. Không cần reset database hoặc tạo migration. Không chạy SQL bổ sung hàng loạt với dữ liệu giả.

## Phạm vi file và hợp đồng

- `Services/QuanLyTaiKhoan/IQuanLyTaiKhoanService.cs`: thêm SoDienThoai cho yêu cầu tạo/sửa, CoHoSoKhachHang cho dữ liệu đọc và mã lỗi SoDienThoaiHoSoKhongHopLe.
- `Services/QuanLyTaiKhoan/QuanLyTaiKhoanService.cs`: tạo/bổ sung hồ sơ và đồng bộ hai bản ghi trong transaction; dùng IDongHo đã có.
- `Controllers/QuanLyTaiKhoanController.cs`: truyền đầu vào, nạp cờ tồn tại hồ sơ từ server và ánh xạ lỗi vào trường số điện thoại.
- `ViewModels/QuanLyTaiKhoanViewModel.cs`, `Views/QuanLyTaiKhoan/_ThongTinForm.cshtml`, `wwwroot/js/quan-ly-tai-khoan.js`: trường nhập số điện thoại chỉ để tạo hồ sơ còn thiếu. JavaScript hỗ trợ ẩn/hiện; service vẫn bắt buộc kiểm tra khi không có JavaScript hoặc gửi POST trực tiếp.
- `Program.cs`: loại đăng ký DI trùng, giữ đăng ký trong extension dùng chung.
- `tests/QuanLyDatSan.M103.Tests`: bổ sung các ca tích hợp và schema hồ sơ trong database RAM của test M1.

Không sửa Entity, DbContext, migration, IKhachHangService, DatSanService hay Controller/View nghiệp vụ M2–M5. Đọc/ghi Entity KhachHang hiện có từ nghiệp vụ quản lý tài khoản để bảo đảm quan hệ đã thống nhất; không triển khai chức năng quản lý hồ sơ thay cho M3.

Số điện thoại được kiểm tra theo định dạng hiện có của hồ sơ: bắt đầu 0 hoặc +84, theo sau 9–10 chữ số, tối đa 20 ký tự; chuẩn hóa khoảng trắng đầu/cuối. Khi đã có hồ sơ, số điện thoại gửi thêm không được dùng để ghi đè hồ sơ.

## Kiểm thử

`HoSoTaiKhoanTests` kiểm tra tạo khách kèm hồ sơ, chặn thiếu/sai số điện thoại, không nhận trường hệ thống, sửa khách cũ/đổi vai trò, không tạo hồ sơ trùng khi gửi lại, đồng bộ họ tên/email và giữ dữ liệu hồ sơ cũ, rollback khi lỗi sau SQL, và đồng hồ chỉ có một singleton.

`ServiceM1Tests` bổ sung kiểm tra IDatSanService chỉ có một đăng ký scoped và rollback cả hồ sơ khi đổi vai trò thất bại. Các ca phân quyền/antiforgery/thu hồi phiên và Admin cuối của bộ cũ vẫn được giữ.

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release
```

Release là cấu hình kiểm tra riêng khi Debug đang bị tiến trình ứng dụng khóa; không thay cấu hình project để né lỗi khóa file. Khi chạy lại bằng Visual Studio, dừng Debug trước rồi build/run bình thường.

Kết quả ngày 14/10/2026:

- Build Release: thành công, 0 cảnh báo, 0 lỗi (lệnh kiểm tra dùng `-p:NuGetAudit=false`, không phải kiểm toán lỗ hổng package).
- Toàn solution: 244/244 test đạt — M1-03: 58; M1-04: 24; M2-01: 80; M4: 82.
- Kiểm tra bổ sung trên SQL Server LocalDB riêng: tạo khách kèm hồ sơ, Admin sửa đồng bộ, bổ sung hồ sơ cũ rồi tạo đơn đều thành công; DI chỉ có một IDatSanService; model vẫn khớp snapshot. 5/5 ca đạt.
- Chưa kiểm tra trực quan browser/mobile; các form và phân quyền đã chạy qua HTTP test. Database của ứng dụng/người dùng không được thay đổi trong lần kiểm thử này.

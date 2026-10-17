# M4-03 — Xác nhận, từ chối và hủy đơn

Phụ trách: Phạm Thành Nghĩa — MSSV 23103100308. Codex hỗ trợ triển khai và kiểm thử.
Nội dung: đối chiếu issue #23, hợp đồng M3/M4, kết quả kiểm thử và hướng dẫn review.
Cập nhật: 16/10/2026. Issue: [#23](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/23).
Đối chiếu quy ước nhóm v1.4 và hướng dẫn layout chung.

Nghiệm thu kỹ thuật mới nhất: **đạt**, M4 114/114, JS 4/4 và 9 nhóm kiểm tra Chrome.
Xem [biên bản 16/10/2026](Nghiem-thu-M4-03-2026-10-16.md) để biết phạm vi, ảnh và giới hạn.

## Kết quả triển khai

- POST XacNhan/TuChoi/Huy tại QuanLyDonDatController: chỉ Admin/NhanVien, có antiforgery.
- Form gửi mã đơn và RowVersion Base64 (đúng 8 byte); lý do hủy/từ chối bắt buộc, tối đa 1000 ký tự.
- Service kiểm tra trạng thái, giờ, lịch sân, cọc và phiên bản dưới khóa sân/transaction.
  Ghi trạng thái cùng thời điểm/lý do; gửi lặp hoặc phiên bản cũ không ghi đè dữ liệu.
- Chặn hủy/từ chối mọi đơn có cọc, kể cả dữ liệu cũ đang chờ. Theo v1.4, chưa hỗ trợ hoàn cọc;
  luồng bình thường chỉ ghi cọc sau xác nhận.
- Nút hiển thị theo trạng thái/giờ/cọc. JS riêng xác nhận hủy/từ chối, khóa ba nút khi gửi
  và khôi phục khi quay lại bằng lịch sử.
- Thành công redirect về chi tiết và dùng TempData Success. Lỗi dùng ModelState, giữ lý do đã nhập
  và nạp trạng thái/RowVersion mới nhất. Razor mã hóa lý do; URL quay lại chỉ nhận đường dẫn nội bộ.
- Comment đầu file ghi họ tên, MSSV và đúng nội dung. Không đổi schema/migration/layout/menu.

## Ma trận chuyển trạng thái

| Thao tác | Trạng thái đầu | Điều kiện | Kết quả |
| --- | --- | --- | --- |
| Nhân viên/Admin xác nhận | ChoXuLy | Chưa bắt đầu; sân/loại sân hoạt động; trong giờ mở; không bảo trì/trùng | DangXuLy, NgayXacNhan |
| Nhân viên/Admin từ chối | ChoXuLy | Lý do hợp lệ, không có cọc | TuChoi, NgayHuy, LyDoHuy |
| Nhân viên/Admin hủy | ChoXuLy/DangXuLy | Chưa bắt đầu, không có cọc, lý do hợp lệ | DaHuy, NgayHuy, LyDoHuy |
| Khách hủy qua service | ChoXuLy | Đúng chủ đơn, chưa bắt đầu, không có cọc, lý do hợp lệ | DaHuy, NgayHuy, LyDoHuy |

Đơn đã hủy/từ chối/hoàn thành không mở lại. Controller kiểm tra vai trò; service khách hủy
kiểm tra chủ đơn trước phiên bản. Mọi thao tác ghi đều dùng RowVersion.

## Hợp đồng để M3 sử dụng

1. Controller M3 xác thực KhachHang, lấy mã tài khoản từ phiên đăng nhập/claims.
2. Gọi `LayChiTietCaNhanAsync(maTaiKhoan, id, cancellationToken)`; chỉ trả đơn của chủ sở hữu
   và có `ChiTietDonDto.PhienBan`, không cần controller truy cập DbContext.
3. Mã hóa PhienBan Base64 vào form POST có antiforgery.
4. Giải mã và gọi `KhachHangHuyDonAsync(new HuyDonYeuCau(id, lyDo, phienBan), maTaiKhoan, cancellationToken)`.
5. Hiển thị kết quả. Không nhận mã tài khoản/vai trò từ form.

Đã test đọc chi tiết → lấy phiên bản → hủy thành công → chặn phiên bản cũ/khách khác.
Giao diện khách hủy thuộc #20 của M3; #23 cung cấp service và dữ liệu cần để tích hợp.

## File chính

- Controllers/QuanLyDonDatController.cs: ba POST, validation, quyền, giữ dữ liệu khi lỗi.
- Services/DatSan/DatSanService.XuLy.cs: bổ sung chặn cọc ở các đường hủy/từ chối.
- Services/DatSan/HopDongDatSan.cs: PhienBan trên DTO chi tiết dùng chung.
- Services/DatSan/DatSanService.Doc.cs và DatSanService.DocQuanLy.cs: đọc phiên bản cùng chi tiết.
- Services/DatSan/IDatSanService.cs và HopDongDocDonQuanLy.cs: comment hợp đồng đúng phạm vi.
- ViewModels/XacNhanDonViewModel.cs và QuanLyDonDatViewModel.cs: dữ liệu POST và điều kiện hiển thị.
- Views/QuanLyDonDat/ChiTiet.cshtml và wwwroot/js/quan-ly-don-dat.js: form và xử lý tương tác.
- tests/QuanLyDatSan.M4.Tests: QuanLyDonDatTests.cs, DatSanTests.cs, quan-ly-don-dat.test.cjs.

## Kết quả kiểm thử trước đây (13/10/2026)

- M4: **114 passed, 0 failed, 0 skipped**, SQL Server LocalDB thật trên instance riêng.
- JavaScript: **4 passed, 0 failed**, kiểm tra handler với DOM mô phỏng bằng Node.
- M103 **45**, M104 **24**, M201 **80** đạt trong lần chạy solution trước đó.
- Build toàn solution bằng dotnet build --no-restore thành công: 0 warning, 0 error; git diff --check đạt.

Lần chạy chung ban đầu và lần M4 trên MSSQLLocalDB mặc định gặp SQL timeout khi seed/login.
sqlcmd độc lập cũng gặp login timeout. Sáu ca tra cứu chạy lại riêng đạt; sau đó toàn bộ 114 ca M4
đạt trên instance riêng M4Issue23_20261013_7a64. Không ghi nhận lần solution đầu là pass toàn bộ.
Test tạo/xóa database ngẫu nhiên QuanLyDatSan_M4Test_<guid>, không dùng QuanLyDatSan_Local.
EnsureCreated không kiểm chứng chuỗi migration. Chưa kiểm tra trực quan bằng trình duyệt thật.

### Lệnh chạy lại

Cần .NET 10 SDK, SQL Server LocalDB và Node.js. Từ thư mục solution:

```powershell
$env:M4_TEST_SQL_SERVER = '(localdb)\MSSQLLocalDB'
dotnet test tests/QuanLyDatSan.M4.Tests/QuanLyDatSan.M4.Tests.csproj --no-restore --verbosity minimal
node --test tests/QuanLyDatSan.M4.Tests/quan-ly-don-dat.test.cjs
git diff --check
```

Có thể đặt M4_TEST_SQL_SERVER thành instance test riêng. Không đổi connection string ứng dụng.

### Kiểm tra nhanh khi review

1. Nhân viên/Admin mở Quản lý đơn đặt sân → chi tiết đơn chờ trong tương lai.
2. Xác nhận: trạng thái Đã xác nhận và có thời điểm; refresh không xử lý lại.
3. Với đơn chờ khác, nhập lý do từ chối/hủy; chọn Không để giữ nguyên, chọn Đồng ý để lưu.
4. Thử lý do trống/quá dài và hai tab dùng phiên bản cũ: lỗi rõ, không ghi đè, giữ lý do khi còn được phép.
5. Đơn có cọc không có nút hủy/từ chối; POST trực tiếp cũng bị chặn.
6. Khách/ẩn danh/tài khoản khóa không xử lý được qua URL trực tiếp.
7. Kiểm tra desktop/mobile, bàn phím và khôi phục nút khi quay lại.

## Bàn giao

Nội dung PR theo mẫu: [PR-M4-03.md](PR-M4-03.md).
Phần code tại HEAD `b58db79` đã được nghiệm thu kỹ thuật ngày 16/10/2026, gồm kiểm tra Chrome desktop/mobile.
Lượt nghiệm thu chỉ cập nhật tài liệu/bằng chứng, chưa commit/push/tạo PR hoặc đóng issue.
Còn đối chiếu main trước merge, review của thành viên khác và merge theo quy trình nhóm.
Issue #24 (ghi cọc, hoàn thành gắn tính tiền M5-03) là công việc tiếp theo.

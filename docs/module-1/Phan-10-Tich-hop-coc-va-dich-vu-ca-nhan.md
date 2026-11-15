# Tích hợp cọc mới, dịch vụ cá nhân và menu đối soát

**M1: Trần Trọng Tùng — 23103100202. Ngày 15/11/2026. Codex hỗ trợ triển khai và kiểm thử.**

Nền: main `43bc625` sau PR #75/#77. M1 tích hợp trên phần nghiệp vụ M3/M4/M5 đã có; giữ ghi nhận tác giả, cần M3/M4/M5 review các điểm nối trước khi merge.

## Hành vi mới

1. `TaoDonAsync` tạo đơn chính sách cọc 1, lưu cọc yêu cầu bằng 50% tiền sân từ `ITinhTienService`, làm tròn theo quy ước. Giá, khách và chính sách lấy phía server. Cọc đã nhận vẫn bằng 0.
2. Không phụ thuộc cờ mô phỏng để chọn chính sách. Khi mô phỏng tắt hoặc ngoài Development, đơn mới vẫn chính sách 1; trang cọc báo chưa sẵn sàng, không cho dùng đường xác nhận cũ để bỏ qua đối soát. Đơn miễn phí không tạo giao dịch/biên nhận 0 đồng, được xác nhận theo điều kiện thông thường.
3. Đơn đã tồn tại không bị đổi chính sách, cọc hoặc lịch sử. Không backfill.
4. Chi tiết khách hàng hiển thị từng dịch vụ, số lượng, đơn giá đã lưu, thành tiền và trạng thái. Dòng hủy đọc từ bảng `LichSuHuyDichVu` của M5; chỉ đọc sau khi kiểm tra chủ đơn và tiếp tục lọc chủ sở hữu trong SQL. Không trả tài khoản người hủy hay danh mục quản trị. Dòng hủy không cộng vào tiền đơn. Thời điểm hủy hiện ghi UTC đúng dữ liệu bảng M5.
5. Công thức thành tiền do M5 tính trong service, Controller/View chỉ ánh xạ/hiển thị. Đơn hoàn thành giữ tiền đã chốt.
6. Menu “Đơn đặt sân” nhận diện `DoiSoatCoc`, `XacNhanCoc`, `TaoGiaoDichMoPhong`, kể cả POST lỗi trả lại View. Không tạo liên kết GET tới action POST.

## Sửa lỗi chặn nghiệm thu

- Ánh xạ lỗi tính tiền dự kiến tại đường ghi cọc/chốt tiền về lỗi nghiệp vụ, rollback. Chỉ bắt quanh công thức; không che lỗi SQL/hạ tầng.
- M5 kiểm tra tổng sau thay đổi dịch vụ không thấp hơn cọc đã nhận. Nếu hủy gây vi phạm, rollback cả dòng dịch vụ và bản ghi lịch sử hủy trong cùng transaction.
- Cập nhật caller kiểm thử M4 theo chữ ký service M5 mới (actor, phiên bản). Test cạnh tranh dùng hai tác vụ, không đợi M5 bên trong khi M4 đang giữ cùng khóa sân.
- Fixture SQL có đọc bảng audit dùng migration thật trên database ngẫu nhiên riêng. Fixture ảnh chọn migration theo tên, không giả định migration ảnh luôn đứng cuối.

## File chính

| Nhóm | File |
|---|---|
| Tạo đơn/cọc | `Services/DatSanService.cs` |
| DTO và đọc cá nhân | `Services/DatSan/HopDongDatSan.cs`, `DatSanService.Doc.cs`, `Repositories/QuanLyDatSan/DatSanRepository.cs` |
| Giao diện cá nhân | `Controllers/DonDatController.cs`, `ViewModels/DonDat/KhachHang/LichDatViewModel.cs`, `Views/DonDat/ChiTiet.cshtml` |
| Menu | `Services/MenuQuanTri.cs` |
| Tiền/rollback | `Services/DatSan/DatSanService.Tien.cs`, `DatSanService.XuLy.cs`, `Services/QuanLyDichVuTrongDon/DichVuTrongDonService.cs` |
| Nghiệm thu mới | `tests/QuanLyDatSan.M4.Tests/TichHopCocDichVuTests.cs`, các ca menu và hồi quy SQL/HTTP |

## Điều kiện chạy và thử local

Không tạo migration mới cho phần tích hợp này. Database chạy ứng dụng cần có các migration trên main, bao gồm `20261109095838_ThemUniqueIndexTenDichVu` và `20261113210000_M5LuuLichSuHuyDichVu`. Kiểm tra dữ liệu trùng tên dịch vụ trước khi áp dụng unique index. Bảng lịch sử hiện được M5 quản lý bằng migration/SQL, không thuộc model EF; `EnsureCreated` không đủ để dựng schema ứng dụng hiện tại.

Không tự cập nhật database ứng dụng trong đầu việc này. Sau khi người quản lý database kiểm tra/backup, áp dụng migration bằng quy trình hiện có của nhóm.

Để chạy thử nguồn tiền giả, cấu hình bằng User Secrets hoặc cấu hình local không commit:

```json
{
  "CocMoPhong": {
    "Bat": true,
    "TaiKhoanNhan": "DEMO-UNETI"
  }
}
```

Chạy Development. Không dùng ngân hàng hay chuyển tiền thật. Việc bật chính sách không tự bật mô phỏng hoặc cấu hình trên máy khác.

1. Khách tạo đơn mới: 90 phút, giá 200.000đ/giờ → cọc yêu cầu 150.000đ; cọc nhận 0đ, trạng thái chờ.
2. Khách mở trang cọc. Nút “Đã chuyển” chỉ quay lại chi tiết; không tạo giao dịch hay xác nhận tiền.
3. Nhân viên/Admin mở đối soát từ chi tiết quản lý, tạo giao dịch mô phỏng đúng nội dung `COC <mã đơn>`, đúng số tiền và thời điểm hợp lệ.
4. Đối soát và xác nhận: tiền cọc/biên nhận/trạng thái cập nhật cùng transaction. Gửi lại không nhận cọc hai lần.
5. Thêm dịch vụ 60.000đ. Khách thấy chi tiết dòng. Đổi giá danh mục không làm đổi giá đã lưu.
6. Đến giờ kết thúc, xác nhận thu đủ và hoàn thành: tổng 360.000đ, cọc 150.000đ; khách đọc tiền đã chốt. Không cộng cọc lần nữa.
7. Kiểm tra đổi ID sang đơn người khác, tắt mô phỏng, đơn miễn phí, hủy/thêm lại dịch vụ và giữ menu khi POST lỗi.

## Phạm vi nghiệm thu

Kết quả ngày 15/11/2026:

- Build Release toàn solution thành công. Lượt biên dịch Razor có 2 warning CS0219 cũ tại `Views/ThongKe/Index.cshtml:28–29`; không thêm warning mới.
- Tổng 672 ca riêng biệt đạt, 0 lỗi, 0 bỏ qua theo lượt cuối của từng project: M103 151, M104 52, M201 180, M4 289.
- Lượt toàn solution đạt 671 ca; sau khi mở rộng test hoàn thành đơn và thêm một ca biên tiền, chạy lại toàn bộ M4 đạt 289/289. Không cộng trùng các lượt chạy. Một lượt test mở rộng ban đầu dùng lại context đã đọc trước đối soát HTTP; đã sửa fixture thao tác dùng scope mới đúng vòng đời request và chạy lại thành công.
- Kiểm tra `dotnet ef migrations has-pending-model-changes`: không có thay đổi model chưa được migration ghi nhận.
- Đã kiểm chứng luồng HTTP tạo đơn → trang cọc → tạo giao dịch mô phỏng → đối soát/retry; tiếp tục thêm dịch vụ/chốt tiền qua service và khách đọc tiền chốt qua HTTP. Các test kiểm tra tiền/actor giả mạo, chủ sở hữu, tắt nguồn mô phỏng, đơn miễn phí, làm tròn, overflow không lưu một phần, hủy/sửa dịch vụ thấp hơn cọc phải rollback và lịch sử hủy.
- Các database test tên ngẫu nhiên được fixture dọn dẹp; không áp dụng migration vào database ứng dụng. Bằng chứng local không commit nằm tại `obj/tich-hop-coc-tests/` và `obj/tich-hop-coc-final/` trong project web.
- Chưa kiểm tra trực quan desktop/mobile. Không thêm nguồn ngân hàng thật, xử lý hoàn tiền hoặc tự hết hạn giữ chỗ trong đợt này.

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --configuration Release --no-restore -p:UseAppHost=false
$env:QLDS_M1_TEST_SQLSERVER='1'
$env:QLDS_M2_TEST_SQLSERVER='1'
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --configuration Release --no-build --no-restore
```

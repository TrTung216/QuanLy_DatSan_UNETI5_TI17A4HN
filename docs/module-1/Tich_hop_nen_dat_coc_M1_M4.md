# M1 — Tích hợp nền dữ liệu và service đặt cọc M4

Ngày bàn giao: **03/11/2026**.

**M1:** Trần Trọng Tùng — MSSV 23103100202; tích hợp schema, migration, DI, lỗi lưu trữ và caller hiện có. Codex hỗ trợ triển khai/kiểm thử.

**M4:** Phạm Thành Nghĩa — MSSV 23103100308; service đối soát, DTO/model và bộ test cọc từ [PR #66](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/pull/66), commit `29a41e7`. Nhánh tích hợp giữ nguyên lịch sử commit M4, không ghi nhận lại phần nghiệp vụ này thành đóng góp M1.

Căn cứ [hợp đồng M3/M4/M5](../Hop_dong_M3_M4_M5.md), [phân công cọc](../Phan_cong_chuyen_khoan_QR.md) và [quy ước Controller–Service–Repository](../Quy_uoc_Controller_Service_Repository.md).

## 1. Phạm vi bàn giao

PR #66 đứng riêng có thể build nhưng chưa chạy đủ chức năng vì thiếu EF mapping/migration. Luồng hủy/từ chối cũ cũng đã gọi query biên nhận; chưa bật cọc mới vẫn gặp lỗi. Nhánh `feature/m1-tich-hop-coc` tích hợp nền còn thiếu trước khi đưa cả phần M4/M1 vào main.

| Thành phần | Thay đổi M1 |
| --- | --- |
| `Data/ApplicationDbContext.cs` | Đăng ký giao dịch/biên nhận; precision, độ dài, RowVersion, check constraints, unique và FK Restrict; hai trường chính sách trên đơn |
| `Migrations/20261103132533_ThemGiaoDichVaBienNhanCoc.*` và snapshot | Schema đồng bộ, giữ đơn cũ ở chính sách 0; không sửa tiền/trạng thái/dịch vụ cũ |
| `Repositories/Chung/DonViCongViec.cs` | Nhận diện chính xác ba unique index cọc, giữ nguyên lỗi hệ thống/FK ngoài phạm vi |
| `Extensions/DependencyInjection.cs`, `appsettings.json` | Bind `CocMoPhong`; mặc định tắt; nguồn nhận chỉ là nhãn mô phỏng |
| `ViewModels/ThongBaoLoiDatSanViewModel.cs` | Thông báo ba mã lỗi mới của M4, tránh exception tại caller hiện có |
| `Controllers/QuanLyDonDatController.cs` | Trả `Forbid()` khi service kiểm tra lại và trả `KhongDuQuyen` |
| `.gitignore` | Cho phép theo dõi migration mới; tiếp tục bỏ qua `*_CapNhatModel.cs` và `*_CapNhatModel.Designer.cs` |
| `tests/QuanLyDatSan.M4.Tests/Coc*Tests.cs` mới của M1 | Nâng cấp database, constraint/RowVersion, cấu hình DI, HTTP và cạnh tranh với hủy |

Đường dẫn code tính từ thư mục project, ngoại trừ `.gitignore` và `tests/` tính từ repo.

## 2. Database và bảo toàn dữ liệu

- `DatSan.PhienBanChinhSachCoc`: mặc định `0`; chỉ chấp nhận `0/1`.
- `DatSan.TienCocYeuCau`: `decimal(18,2)`, mặc định `0`, không âm; chính sách 0 có yêu cầu cọc bằng 0.
- `TienCoc` vẫn là số tiền đã nhận. Migration không chuyển số cọc cũ thành yêu cầu cọc và không tạo biên nhận giả.
- `GiaoDichCoc`: số tiền dương, nội dung tối đa 100 ký tự, nguồn tối đa 30, tài khoản nhận/mã ngoài tối đa 100; RowVersion SQL Server 8 byte.
- Unique `(Nguon, TaiKhoanNhan, MaGiaoDichNgoai)` chống ghi trùng giao dịch.
- `BienNhanCoc`: số tiền dương; unique riêng `MaDatSan` và `MaGiaoDich`, mỗi đơn/giao dịch có tối đa một biên nhận.
- FK tới đơn/giao dịch/người ghi/người xác nhận dùng Restrict, không cascade xóa lịch sử.
- Không đổi migration cũ, không tắt `PendingModelChangesWarning`.

Migration `Up` chỉ thêm cột, bảng, constraint và index. `Down` xóa nền cọc mới, vì vậy không chạy hạ migration trên database đã có giao dịch/biên nhận cần giữ; trao đổi nhóm để xử lý bằng migration tiếp theo nếu đã triển khai.

## 3. Cách lấy code và nâng cấp local

Sau khi PR tích hợp được merge, bảo toàn thay đổi local trước khi pull. Không chỉ lấy riêng commit M4 rồi chạy ứng dụng với schema cũ.

Chạy từ thư mục repo, với connection string local đã cấu hình qua User Secrets/biến môi trường:

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release
dotnet ef migrations has-pending-model-changes --project QuanLyDatSan_UNETI5_DHTI17A4HN --configuration Release --no-build -- --environment Development
dotnet ef database update --project QuanLyDatSan_UNETI5_DHTI17A4HN --configuration Release --no-build -- --environment Development
```

Hoặc Package Manager Console: chọn đúng startup/default project rồi chạy:

```powershell
Update-Database -Args '--environment Development'
```

Không sinh thêm migration chỉ vì chưa chạy update. Không xóa/reset database của thành viên. Lần tích hợp này chỉ áp dụng migration trên database test ngẫu nhiên riêng; database ứng dụng chưa được cập nhật thay người dùng.

## 4. Cấu hình mô phỏng và giới hạn hiện tại

Cấu hình mặc định đã commit:

```json
"CocMoPhong": {
  "Bat": false,
  "TaiKhoanNhan": "DEMO-UNETI"
}
```

`TaiKhoanNhan` là nhãn nguồn dữ liệu mô phỏng, không phải số tài khoản ngân hàng. Khi kiểm thử service ở local có thể bật qua biến môi trường trong terminal riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:CocMoPhong__Bat = 'true'
$env:CocMoPhong__TaiKhoanNhan = 'DEMO-UNETI'
```

Service M4 vẫn yêu cầu đồng thời Development, cấu hình bật, thông tin nhận hợp lệ và quyền Admin/Nhân viên. Bật cấu hình ở Production không mở mô phỏng. Cấu hình không thay thế kiểm tra quyền phía server.

**Đây là nền service, chưa phải luồng chuyển khoản dùng được trên website.** Chưa có các route/View mới `DonDat/Coc`, đối soát hay tạo giao dịch mô phỏng. Không tạo link/menu dẫn tới các action chưa tồn tại. Không có ngân hàng/QR thật.

`TaoDonAsync` vẫn tạo đơn chính sách `0`. Test chủ động tạo đơn chính sách `1` trong database test; ứng dụng không có seed hoặc công cụ đổi đơn thật sang chính sách 1. Chỉ bật chính sách mới trong service tạo đơn khi M3/M4 hoàn thiện UI và M5 bàn giao công thức cọc, cập nhật tất cả caller cùng đợt.

## 5. Hợp đồng lỗi và điểm nối

| Nguồn | Mã / cách xử lý |
| --- | --- |
| Unique mã giao dịch ngoài | `MaLoiLuuTru.TrungGiaoDichCoc` → service M4 trả `TrungMaGiaoDich` |
| Unique biên nhận theo đơn hoặc giao dịch | `MaLoiLuuTru.TrungBienNhanCoc` → M4 tải lại để phân biệt retry hợp lệ với dùng trùng |
| RowVersion | Giữ `XungDotPhienBan`; không ghi đè dữ liệu mới |
| Xác nhận/ghi cọc qua đường cũ cho chính sách mới | `MaLoiDatSan.YeuCauDoiSoatCoc` → thông báo phải đối soát |
| Hủy/từ chối có giao dịch chờ | `GiaoDichChoDoiSoat` → thông báo giữ đơn để xử lý tiền |
| Quyền actor thay đổi | `KhongDuQuyen` → Controller quản lý trả `Forbid()` |

Không đổi mọi lỗi unique/FK thành lỗi trùng cọc; ràng buộc không thuộc danh sách nhận diện vẫn đi qua handler lỗi hệ thống. Không thêm công thức tiền trong Controller/View hoặc repository.

## 6. Kiểm thử và bằng chứng

Chạy từ repo:

```powershell
$env:QLDS_M1_TEST_SQLSERVER = '1'
$env:QLDS_M2_TEST_SQLSERVER = '1'
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release --logger 'trx;LogFilePrefix=coc' --results-directory QuanLyDatSan_UNETI5_DHTI17A4HN/obj/m1-coc-full
```

Các fixture dùng database SQL Server/LocalDB tên ngẫu nhiên riêng và guard trước cleanup; không dùng connection string ứng dụng. SQLite tests chỉ chứng minh phạm vi HTTP/validation của chúng, không thay bằng chứng khóa/RowVersion SQL Server.

Các nhóm kiểm tra:

- Bộ `CocTests` của M4: đối soát, quyền, sai tiền/nội dung/giờ, gửi lặp/đồng thời, rollback, nguồn mô phỏng tắt/Production.
- `CocMigrationTests`: migrate từ schema trước cọc với đơn đã hoàn thành và dữ liệu cọc/dịch vụ kiểm thử; bảo toàn tiền/trạng thái/dòng dịch vụ, không tạo biên nhận lịch sử giả.
- `CocHaTangTests`: unique biên nhận theo cả đơn và giao dịch, FK chặn xóa lịch sử, optimistic concurrency và check số tiền dương.
- `CocCauHinhTests`: resolve service qua DI chung, cấu hình thiếu/tắt/bật và Production.
- `CocThongBaoHttpTests`: action xác nhận/hủy/từ chối hiện có trả thông báo thay lỗi 500; caller xử lý lỗi quyền.
- `CocCanhTranhTests`: tạo giao dịch cạnh tranh hủy; xác nhận cọc cạnh tranh hủy, giữ đơn/tiền/biên nhận nhất quán.
- Hồi quy M1/M2/M4 trong solution do thay DbContext và DI chung.

Kết quả thực chạy ngày 03/11/2026:

| Project | Số ca đạt sau tổng hợp các lượt chạy |
| --- | --- |
| M103 | 106 (98 ở lượt solution, 8 SQL Server chạy riêng với cờ bật) |
| M104 | 24 |
| M201 | 138, đã bật test SQL Server |
| M4 | 237, gồm 19 ca cọc của M4 và 21 ca tích hợp mới của M1 |
| Tổng | 505 ca khác nhau đã đạt; không còn ca bỏ qua chưa chạy |

Build Release solution đạt. Hai cảnh báo CS0219 có sẵn ở `Views/ThongKe/Index.cshtml`, ngoài phạm vi; cảnh báo blocking task trong test cạnh tranh mới đã sửa và hai test đó chạy lại đạt. EF `has-pending-model-changes` báo không có thay đổi chưa nằm trong migration. Đã sinh và xem SQL nâng cấp, không thực thi vào database ứng dụng.

Bằng chứng local ở `obj/m1-coc-*`; không commit log/TRX vào repo. Chưa nghiệm thu giao diện cọc khách/nhân viên, kết nối ngân hàng hoặc công thức cọc mới của M5.

## 7. Công việc bàn giao tiếp

1. **M4:** review mapping/index và lỗi lưu trữ; làm Controller/View đối soát, công cụ mô phỏng đúng quyền và POST + antiforgery; tiếp tục xử lý ngoại lệ theo hợp đồng.
2. **M5:** công thức cọc 50%/còn phải thu, dịch vụ và chốt tiền theo hợp đồng; không đổi giá snapshot đơn cũ.
3. **M3:** gọi đầu đọc cọc của đúng khách, hiển thị QR minh họa và trạng thái/biên nhận; không tự tính tiền hoặc xác nhận cọc.
4. **M1:** review tích hợp tiếp và kích hoạt có phối hợp. Cập nhật main khi đã được review, dùng **Create a merge commit**.

PR tích hợp chứa commit M4 của #66 và phần nền M1. Review cả hai phần; không merge riêng #66 trước nền này. Không tự đóng issue toàn luồng cọc khi mới hoàn tất service/hạ tầng.

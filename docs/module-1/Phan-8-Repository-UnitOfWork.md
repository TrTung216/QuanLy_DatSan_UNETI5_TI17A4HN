# M1 — Refactor Repository và Unit of Work

**Người phụ trách:** Trần Trọng Tùng — MSSV 23103100202. **Ngày:** 28/10/2026.

Codex hỗ trợ tách truy cập dữ liệu, giữ nghiệp vụ và bổ sung kiểm thử theo [quy ước Controller–Service–Repository](../Quy_uoc_Controller_Service_Repository.md).

## Phạm vi PR

Nhánh `feature/m1-repository-unit-of-work` được tạo từ `main@1a97883` trong worktree riêng. Chỉ chuyển cấu trúc M1 và hạ tầng lưu cần thiết, giữ public service interface, Controller, route, View và hành vi hiện có trên main.

Các thay đổi hồ sơ khách hàng, menu, layout và báo cáo chưa commit ở workspace gốc không thuộc PR này. Vì vậy bản service trong PR không bổ sung số điện thoại, tự tạo/đồng bộ hồ sơ khách hàng hoặc dependency đồng hồ chưa có trên main. Khi đưa các thay đổi đó lên sau, cần tích hợp vào cấu trúc mới và chạy lại test tương ứng.

## File và trách nhiệm

| Nhóm file | Trách nhiệm |
| --- | --- |
| `Repositories/QuanLyLoaiSan/ILoaiSanRepository.cs`, `LoaiSanRepository.cs` | Đọc/lọc/sort/phân trang, nạp loại sân để cập nhật, kiểm tra trùng/tham chiếu và chuẩn bị thêm/xóa |
| `Repositories/QuanLyTaiKhoan/ITaiKhoanRepository.cs`, `TaiKhoanRepository.cs` | Đọc/lọc tài khoản, nạp để cập nhật/xác thực, truy vấn quyền hiện hành và chuẩn bị thêm |
| `Repositories/TrangChu/ITrangChuRepository.cs`, `TrangChuRepository.cs` | Giữ truy vấn sân công khai theo loại và giới hạn/thứ tự cũ |
| `Repositories/Chung/IDonViCongViec.cs` | Hợp đồng lưu và callback có `QuyetDinhLuu<T>.DuocLuu` rõ ràng |
| `Repositories/Chung/DonViCongViec.cs` | SaveChanges, transaction, commit/rollback và cleanup tracking |
| `Repositories/Chung/LoiLuuTruException.cs` | Lỗi hạ tầng có mã, không làm service phụ thuộc EF/SqlClient |
| `Services/QuanLyLoaiSan/LoaiSanService.cs` | Giữ validation, quy tắc xóa và mapping lỗi nghiệp vụ |
| `Services/QuanLyTaiKhoan/QuanLyTaiKhoanService.cs` | Giữ kiểm tra Admin cuối cùng/quyền thực tế trong Serializable; thu hồi phiên sau commit |
| `Services/XacThuc/XacThucTaiKhoanService.cs` | Giữ kiểm tra mật khẩu/trạng thái/vai trò và nâng cấp hash |
| `Services/TrangChu/TrangChuService.cs` | Giữ điểm gọi hiện có cho controller, chuyển truy vấn sang repository |
| `Extensions/DependencyInjection.cs` | Đăng ký repository và Unit of Work scoped, cùng ApplicationDbContext |

Đường dẫn trong bảng tính từ project `QuanLyDatSan_UNETI5_DHTI17A4HN/`. Repository không tự SaveChanges/commit hoặc trả IQueryable ra service. Controller M1 đã gọi service nên không phải sửa.

## Hợp đồng lưu

- `LuuAsync` lưu mọi thay đổi đã chuẩn bị trong context cùng scope, không phải chỉ một repository. Thao tác được bọc trong transaction.
- `ThucHienTrongGiaoDichAsync` yêu cầu context không có thay đổi chờ từ thao tác khác; callback quyết định được lưu hay từ chối. Thành công chỉ trả sau commit.
- Không hỗ trợ transaction lồng/ambient transaction; service con chỉ chuẩn bị dữ liệu. Không tự retry thao tác.
- Rollback dùng token cleanup độc lập với request. Ghi nhớ entity bị tác động trước SaveChanges để detach đúng sau lỗi; không Clear toàn context. Không tái sử dụng Unit of Work nếu rollback/dispose lỗi.
- Thu hồi phiên đặt sau commit, ngoài khối bắt lỗi lưu. Ngoại lệ sau commit không được báo như DB đã rollback.
- Mapping unique/FK theo tên ràng buộc M1 đã biết. Check constraint/lỗi ngoài dự kiến không bị đổi thành lỗi trùng/tham chiếu chung.
- Test đồng thời phát hiện EF có thể bọc deadlock nhiều tầng; Unit of Work nhận diện SQL 1205 trong chuỗi InnerException để service trả mã xung đột hiện có.

## Ngoại lệ và giới hạn

- `PhienDangNhap` vẫn dùng EF trong cookie event; `KhoiTaoAdmin` vẫn dùng EF cho bootstrap theo ngoại lệ quy ước. Không thay cơ chế xác thực.
- Không thay Entity/DbContext/schema/migration/snapshot, không chạy Update-Database.
- M2–M5 giữ implementation hiện có. Trước khi dùng Unit of Work cho nghiệp vụ sân/cọc, các module phải review hợp đồng khóa/transaction và bổ sung test của mình.
- Không chỉnh package, seed dữ liệu hoặc triển khai tính năng cọc.
- Chưa kiểm tra thủ công trình duyệt desktop/mobile; bộ test HTTP vẫn kiểm tra endpoint, cookie, quyền, token và form.

## Kiểm thử và chạy lại

`ServiceM1Tests.cs` được cập nhật dependency và kiểm tra DI. Ba file test mới: `DonViCongViecTests.cs`, `LoaiSanServiceRepositoryTests.cs`, `RepositoryM1SqlTests.cs`.

Các ca mới bao gồm từ chối nghiệp vụ không lưu; lỗi sau SaveChanges/trước commit; hủy request; chặn lưu lồng; không ghi lại thao tác thất bại; giữ entity không liên quan; lỗi unique/FK/check/concurrency; phân trang; Serializable; hai Admin hạ quyền đồng thời và thu hồi phiên đúng sau commit.

Chạy từ gốc repo:

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release
$env:QLDS_M1_TEST_SQLSERVER = '1'
$env:QLDS_M2_TEST_SQLSERVER = '1'
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release --no-build --no-restore
dotnet ef migrations has-pending-model-changes --project QuanLyDatSan_UNETI5_DHTI17A4HN --configuration Release --no-build -- --environment Development
```

SQL tests cần LocalDB và chỉ dùng database test riêng có tên ngẫu nhiên, kiểm tra tên trước cleanup. Không dùng/reset database ứng dụng. Không có LocalDB thì bỏ cờ SQL tương ứng và ghi rõ test skipped; không coi SQLite/mock là bằng chứng kiểm thử khóa SQL Server.

Kết quả ngày 28/10/2026 trên đúng nội dung PR: build Release 0 lỗi/0 cảnh báo; **355/355 test đạt, 0 bỏ qua** (M103 70, M104 24, M201 115, M4 141, M5 5), gồm 8 test SQL Server mới của M1. EF xác nhận không có pending model changes. Log/TRX local ở `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/pr-tests.log` và `obj/pr-tests/` trong worktree; không commit artifact build.

Kết quả 383 test trên workspace gốc trước đó có các thay đổi hồ sơ/menu local, không dùng thay kết quả của PR độc lập.

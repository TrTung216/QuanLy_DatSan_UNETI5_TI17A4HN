# M1 — Phần 1: Dữ liệu nền

Phụ trách theo phân công: Trần Trọng Tùng. MSSV: 23103100202.
Codex hỗ trợ soạn mã và kiểm tra; sinh viên cần đọc, hiểu và rà soát trước khi bàn giao.

## Thứ tự thực hiện M1

1. **Dữ liệu nền:** model, enum, DbContext, migration — phần đang bàn giao.
2. Đăng nhập/đăng xuất: băm mật khẩu bằng thư viện, Session, lấy tài khoản hiện tại, kiểm tra khóa và vai trò mỗi yêu cầu.
3. Quản lý tài khoản và phân quyền: chỉ Admin, ViewModel riêng, chống giả mạo yêu cầu, cập nhật quyền khi vai trò đổi.
4. Quản lý loại sân: chỉ Admin, thêm/sửa/ngừng hoạt động, kiểm tra trùng, tìm kiếm và phân trang.

## Các thành phần và cách hoạt động

| Thành phần | Vai trò |
| --- | --- |
| `Models/TaiKhoan.cs` | Dữ liệu tài khoản với mã, tên đăng nhập, giá trị băm mật khẩu, họ tên, email, vai trò và trạng thái. |
| `Models/LoaiSan.cs` | Dữ liệu loại sân, số người tối đa, giá theo giờ và trạng thái. |
| `Enums/` | Cố định số của vai trò và trạng thái theo tài liệu; mỗi nghiệp vụ dùng enum riêng. |
| `Data/ApplicationDbContext.cs` | Ánh xạ hai model thành bảng SQL Server, cấu hình khóa và ràng buộc. |
| `Migrations/` | Các thay đổi để EF Core tạo hai bảng, index và 5 loại sân mẫu. |
| `Program.cs` | Đăng ký DbContext để Controller/Service có thể nhận qua constructor. |

Luồng sử dụng ở phần tiếp theo: ViewModel nhận dữ liệu → Controller/Service kiểm tra nghiệp vụ → DbContext chuẩn hóa chuỗi → SQL Server kiểm tra ràng buộc và lưu dữ liệu.

- Khóa chính `int` tự tăng; chuỗi Unicode với độ dài theo tài liệu; tiền `decimal(18,2)`.
- Unique index trên `TenDangNhap` và `TenLoai` dùng `Vietnamese_100_CI_AS`: không phân biệt hoa/thường, có phân biệt dấu. Kiểm tra trùng ở phần tiếp theo phải truy vấn trực tiếp cột này sau khi Trim, đồng thời xử lý lỗi unique khi có yêu cầu đồng thời.
- `SaveChanges`/`SaveChangesAsync` Trim các chuỗi đầu vào thuộc hai model, trừ giá trị băm mật khẩu. Lệnh SQL trực tiếp và cập nhật hàng loạt bỏ qua bước này; các module nên lưu qua DbContext hoặc tự chuẩn hóa trước.
- Database chặn tên chỉ có dấu cách/rỗng, giá âm, số người không dương và giá trị enum ngoài phạm vi.
- Có 5 loại sân với giá giả phục vụ kiểm thử. Chưa seed tài khoản; cơ chế băm mật khẩu và tạo Admin ban đầu thuộc phần 2.
- Các quan hệ `TaiKhoan.KhachHang` và `LoaiSan.DanhSachSan` chưa được tạo vì model M3/M2 chưa tồn tại. Khi tích hợp, bổ sung navigation và FK bằng migration mới, unique `KhachHang.MaTaiKhoan`, chặn cascade delete. Chưa có luồng tạo tài khoản khách ở phần này.
- Kiểm tra email, thông báo lỗi form và các quy tắc quyền sẽ nằm ở ViewModel/Service của các phần tiếp theo. Phần 1 chưa có giao diện quản lý hoặc đăng nhập.

## Cấu hình và chạy

Chạy PowerShell tại thư mục gốc repository. Cần .NET 10 SDK và SQL Server/LocalDB. Các package EF Core được cố định ở `10.0.10`.

```powershell
dotnet restore
dotnet build
# Nếu máy chưa cài dotnet-ef:
dotnet tool install --global dotnet-ef --version 10.0.10

# Lưu ngoài repository, thay Server theo SQL Server đang dùng.
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Server=(localdb)\MSSQLLocalDB;Database=QuanLyDatSan_M1;Trusted_Connection=True;TrustServerCertificate=True' --project .\QuanLyDatSan_UNETI5_DHTI17A4HN
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project .\QuanLyDatSan_UNETI5_DHTI17A4HN
dotnet run --project .\QuanLyDatSan_UNETI5_DHTI17A4HN
```

User Secrets được đọc trong môi trường Development. Môi trường khác dùng biến `ConnectionStrings__DefaultConnection`. `TrustServerCertificate=True` trong ví dụ dành cho LocalDB phát triển. Ứng dụng không tự chạy migration khi khởi động.

## Kiểm tra phần 1

- Build: không lỗi, không cảnh báo.
- `dotnet ef migrations has-pending-model-changes`: model khớp migration.
- SQL sinh từ migration: `docs/module-1/KhoiTaoM1.sql` để đọc và đối chiếu. Dùng `dotnet ef database update` để áp dụng migration.
- Migration đã chạy thành công trên LocalDB riêng `QuanLyDatSan_M1_KiemThu_20261002`.
- Chạy kiểm thử ràng buộc trên database kiểm thử đã tạo:

```powershell
powershell -NoProfile -File .\docs\module-1\KiemTraM1.ps1
```

Script kiểm tra seed, giá bằng 0, trùng tên khác hoa/thường, giá âm, số người bằng 0, tên rỗng, trạng thái và vai trò không hợp lệ. Mọi bản ghi thử đều rollback; SQL Server có thể vẫn tăng bộ đếm IDENTITY.

## Điểm cần hiểu trước phần 2

- Entity mô tả dữ liệu lưu; ViewModel sẽ mô tả dữ liệu được phép nhận từ form.
- Enum xác định ý nghĩa số lưu trong database, chưa tự cấp quyền truy cập.
- DbContext giúp EF Core đọc/ghi; migration là lịch sử thay đổi cấu trúc.
- Cột `MatKhau` chưa tự băm dữ liệu: phần 2 phải dùng thư viện băm trước khi gán và lưu.

# Tách Controller và service M1 theo quy ước 1.3

Người phụ trách: Trần Trọng Tùng — MSSV 23103100202. Codex hỗ trợ triển khai và kiểm thử.

## Phạm vi

Ba Controller M1 gọi interface service: loại sân, quản lý tài khoản, xác thực đăng nhập. Giữ route/action, ViewModel/View, thông báo, TempData, phân quyền, antiforgery, phân trang và Session/cookie hiện có. Không thay Entity, DbContext, schema hoặc tạo migration. Không chuyển đổi Controller/service M2–M5 trong đầu việc này.

Code cũ được chuyển theo mục 17.1/17.7. Khi chia Issue/PR, có thể tách theo ba nhóm bên dưới; phần sửa snapshot/package có sẵn từ xử lý lỗi migration là đầu việc riêng, không gộp vào PR chỉ refactor.

## File và trách nhiệm

Đường dẫn dưới đây nằm trong project `QuanLyDatSan_UNETI5_DHTI17A4HN`, trừ thư mục tests/docs.

| File | Trách nhiệm |
| --- | --- |
| `Controllers/LoaiSanController.cs` | Kiểm tra form/query, gọi ILoaiSanService, chuyển DTO sang ViewModel, ánh xạ mã lỗi sang thông báo/404/redirect |
| `Services/QuanLyLoaiSan/ILoaiSanService.cs` | Interface, DTO nhập/đọc/phân trang, mã lỗi và kết quả loại sân |
| `Services/QuanLyLoaiSan/LoaiSanService.cs` | Query EF, phân trang 10 dòng, kiểm tra nghiệp vụ/trùng tên, lưu và chặn xóa tham chiếu |
| `Controllers/QuanLyTaiKhoanController.cs` | Phân quyền đầu vào, form, ánh xạ DTO/kết quả sang View/Forbid/NotFound/redirect |
| `Services/QuanLyTaiKhoan/IQuanLyTaiKhoanService.cs` | Interface, DTO tạo/sửa/đọc/phân trang, danh sách mã lỗi; DTO đọc không có mật khẩu/hash |
| `Services/QuanLyTaiKhoan/QuanLyTaiKhoanService.cs` | Băm mật khẩu khi tạo; transaction Serializable; quyền người thao tác; Admin cuối cùng; tự khóa/hạ quyền; thu hồi phiên sau commit |
| `Controllers/TaiKhoanController.cs` | Gọi IXacThucTaiKhoanService; tiếp tục xử lý Session/cookie, ReturnUrl nội bộ, đăng xuất, thông tin và từ chối truy cập |
| `Services/XacThuc/IXacThucTaiKhoanService.cs` | DTO xác thực và kết quả; chỉ trả mã/họ tên/vai trò khi thành công |
| `Services/XacThuc/XacThucTaiKhoanService.cs` | Truy vấn tài khoản, kiểm tra trạng thái/vai trò/hash, rehash khi cần; không thao tác HTTP/cookie/Session |
| `Services/PhienDangNhap.cs` | Thêm overload nhận TaiKhoanDaXacThuc; giữ overload TaiKhoan cho nơi gọi cũ và giữ kiểm tra phiên hiện có |
| `Program.cs` | Đăng ký ba interface/service bằng AddScoped |
| `tests/QuanLyDatSan.M103.Tests/ServiceM1Tests.cs` | Ca kiểm thử mới cho ranh giới service, rollback, quyền, rehash và luồng HTTP đăng nhập/đăng xuất |
| `tests/QuanLyDatSan.M104.Tests/LoaiSanTests.cs` | Bổ sung hai cột giá trong schema RAM để tương thích M2 và ca gọi service với dữ liệu không hợp lệ |

## Hợp đồng gọi

Mọi phương thức async nhận `CancellationToken` và truyền tới EF Core. Service dùng DbContext scoped, không chạy song song các truy vấn trên cùng context. Không gọi service ghi bên trong một transaction độc lập khác; service quản lý tài khoản là bên điều phối transaction của thao tác.

### Loại sân

- `LayDanhSachAsync(tuKhoa?, trangThai?, sapXep, trang, token)` trả `TrangLoaiSan`: danh sách DTO, trang thực tế, tổng trang và tổng số dòng. Controller giữ kiểm tra query sai → 400. Filter/sort/count/skip/take thực hiện trong database.
- `LayChiTietAsync(id, token)` trả `LoaiSanDoc?`, null khi không tồn tại; không trả IQueryable/entity ra Controller.
- `TaoAsync(GhiLoaiSan, token)`, `SuaAsync(id, GhiLoaiSan, token)`, `XoaAsync(id, token)` trả `KetQuaLoaiSan`. Mã bản ghi chỉ có khi ghi thành công.
- `GhiLoaiSan` nhận thông tin danh mục do Admin quản lý, không phải giá đặt sân do khách gửi. Service kiểm tra tên/độ dài, số người dương, giá không âm/đồng nguyên, giới hạn giá và enum.

| Mã kết quả | Ánh xạ tại Controller |
| --- | --- |
| ThanhCong | TempData Success; thêm/sửa về ChiTiet, xóa về DanhSach |
| TrungTen | ModelState tại TenLoai, giữ form đã nhập |
| KhongTonTai | 404 |
| DangDuocThamChieu | TempData Error, về Xoa để hướng dẫn ngừng hoạt động |
| XungDotDuLieu | Thông báo bản ghi đã bị xóa khi đang sửa |
| DuLieuKhongHopLe | Lỗi form tổng quát; Data Annotation vẫn cung cấp lỗi từng trường trên luồng HTTP |

Giữ kiểm tra tham chiếu trước khi xóa và bắt SQL Server FK 547 nếu tham chiếu xuất hiện sau kiểm tra. Giữ xử lý unique 2601/2627 và DbUpdateConcurrencyException. Không cập nhật giá/trạng thái sân con khi sửa loại sân.

### Quản lý tài khoản

- `LayDanhSachAsync(tuKhoa?, vaiTro?, trangThai?, trang, token)` trả `TrangTaiKhoan`; `LayChiTietAsync(id, token)` trả `TaiKhoanDoc?`. DTO đọc không có mật khẩu/hash.
- `TaoAsync(TaoTaiKhoan, token)` và `SuaAsync(id, SuaTaiKhoan, token)` trả `KetQuaQuanLyTaiKhoan`. Danh sách lỗi rỗng nghĩa là đã lưu/commit thành công; có thể có cả lỗi Admin cuối cùng và tự khóa/hạ quyền.
- Mã người thao tác được lấy từ `ITaiKhoanHienTai`, không nhận từ form. VaiTro trong DTO nhập là vai trò tài khoản được Admin quản lý.
- Trạng thái khi tạo luôn là HoatDong. DTO sửa không có tên đăng nhập/mật khẩu. Xác nhận mật khẩu là kiểm tra form, không truyền thêm bản sao mật khẩu sang service.
- Service kiểm tra dữ liệu cần thiết cả khi gọi độc lập: thông tin bắt buộc/độ dài/email, enum và độ dài mật khẩu tạo mới.

| Mã lỗi | Ánh xạ tại Controller |
| --- | --- |
| KhongDuQuyen | Forbid |
| KhongTonTai | 404 |
| TrungTen | ModelState tại TenDangNhap |
| AdminCuoiCung | Thông báo không được mất Admin hoạt động cuối cùng |
| TuKhoaHoacHaQuyen | Thông báo không được tự khóa/hạ quyền |
| XungDotDuLieu | Thông báo tải lại và thử lại khi SQL deadlock 1205 |
| DuLieuKhongHopLe | Lỗi form tổng quát |

Service mở transaction Serializable, đọc tập Admin hoạt động, kiểm tra lại người thao tác rồi mới ghi. Kiểm tra Admin cuối cùng/tự khóa và ghi nằm trong cùng transaction. Chỉ vô hiệu hóa phiên sau commit thành công và khi vai trò/trạng thái thay đổi. Exception ngoài dự kiến được truyền về xử lý lỗi tập trung, không đổi thành thông báo thành công.

### Xác thực

`XacThucAsync(XacThucTaiKhoanYeuCau, token)` trả `KetQuaXacThuc`:

- Thành công: `ThanhCong` cùng `TaiKhoanDaXacThuc(MaTaiKhoan, HoTen, VaiTro)`.
- Thất bại: `ThongTinKhongHopLe`, TaiKhoan null. Sai mật khẩu/không tồn tại/bị khóa/vai trò sai/hash hỏng không làm lộ thông tin tài khoản qua mã lỗi riêng.
- Trim tên đăng nhập, không Trim mật khẩu. Giữ PasswordHasher của ASP.NET Core và lưu hash mới khi SuccessRehashNeeded.
- DTO chứa mật khẩu dùng class, không dùng positional record tự in trường mật khẩu qua ToString. Không log DTO/mật khẩu.

Controller tiếp tục tạo MaPhien mới, lưu Session MaTaiKhoan/HoTen/VaiTro, phát cookie, xử lý ReturnUrl bằng IsLocalUrl. Đăng xuất vẫn POST + antiforgery, xóa Session và cookie. PhienDangNhap/TaiKhoanHienTai là hạ tầng theo mục 17.2, không chuyển HttpContext vào service nghiệp vụ.

## Kiểm thử

Baseline sau khi cập nhật fixture M1-04: 67/67 test đạt trước refactor. Sau refactor và thêm ca kiểm thử: M1-03/service 35, M1-04 24, M2-01 24 — tổng 83 ca đạt.

Ca mới gồm: gọi trực tiếp service với dữ liệu sai; claim Admin cũ không vượt kiểm tra quyền DB; lỗi sau SQL nhưng trước commit phải rollback và không thu hồi phiên; đăng nhập sai/khóa/hash hỏng/vai trò không hợp lệ; nâng cấp hash IdentityV2; ReturnUrl nội bộ/ngoài; đăng xuất thiếu token và phiên sau đăng xuất.

Chạy tại gốc repo (đầu ra riêng để tránh exe ứng dụng đang chạy bị khóa):

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --artifacts-path QuanLyDatSan_UNETI5_DHTI17A4HN/obj/review-v13-artifacts
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --artifacts-path QuanLyDatSan_UNETI5_DHTI17A4HN/obj/review-v13-artifacts --no-build --no-restore
```

Test dùng SQLite RAM và host HTTP thật, không tạo/reset/update database người dùng. Chưa kiểm chứng deadlock/khóa Serializable/FK race trên SQL Server nhiều kết nối; phần xử lý này giữ nguyên từ code cũ. Không có thay đổi giao diện nên lần này không chạy lại trình duyệt desktop/mobile.

## Giới hạn tích hợp

- M3 chưa có trên main: việc tạo kèm hồ sơ KhachHang và đồng bộ HoTen/Email vẫn là đầu việc tích hợp riêng. Không bổ sung Entity/nghiệp vụ đó vào refactor này.
- Service M2/M5 giữ nguyên để người phụ trách chuyển đổi theo Issue của họ.
- PR refactor chỉ chứa phần tách service và kiểm thử/tài liệu liên quan. Việc sửa snapshot/package được giữ riêng; không tự đóng các Issue chức năng M1 chỉ vì đã tách service.

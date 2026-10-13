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
| `Program.cs` | Gọi AddUngDungServices một lần trước builder.Build(); giữ đăng ký hạ tầng xác thực/Session |
| `Extensions/DependencyInjection.cs` | AddUngDungServices gọi nhóm M1 và nhóm M5 hiện có; ba interface/service M1 được đăng ký AddScoped tại AddModuleTaiKhoanVaLoaiSan |
| `tests/QuanLyDatSan.M103.Tests/ServiceM1Tests.cs` | Ca kiểm thử mới cho ranh giới service, rollback, quyền, rehash và luồng HTTP đăng nhập/đăng xuất |
| `tests/QuanLyDatSan.M103.Tests/QuanLyTaiKhoanTests.cs` | Kiểm thử HTTP quản trị, giữ form/thông tin server khi lỗi, thông báo/redirect, phân quyền và thu hồi phiên |
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

Baseline sau khi cập nhật fixture M1-04: 67/67 test đạt trước refactor. Tại commit `4e4a707`: M1-03/service 35, M1-04 24, M2-01 24 — tổng 83 ca đạt. Lần đối chiếu bổ sung ngày 12/10/2026 đạt 92 ca; chi tiết bên dưới.

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

## Đối chiếu lại yêu cầu ngày 12/10/2026

Điểm bắt đầu là nhánh `feature/m1-tach-service`, commit `4e4a707`, so với main local `d4c2e98`. Cả ba nhóm service và phần tích hợp đã có trong commit này/PR #42. Lần đối chiếu bổ sung kiểm thử cho hành vi còn thiếu bằng chứng, không viết lại service đã hoàn thành. Các thay đổi bổ sung được để ở local; không commit, push hoặc sửa PR trong lượt này.

Không tìm thấy AGENTS.md hay bản quy ước v1.3 trong repository. Đã đọc bản `Quy_uoc_nhom_Quan_ly_dat_san_v1.3.md` người dùng cung cấp trong Downloads và đối chiếu với yêu cầu đính kèm. Không đưa bản tài liệu ngoài repo này vào commit thay cho người điều phối.

Kiểm tra kiến trúc trên code hiện tại:

- Ba Controller M1 không còn ApplicationDbContext, EF Core, SaveChanges hay transaction. Select ở Controller chỉ ánh xạ DTO đã materialize sang ViewModel.
- Ba service nghiệp vụ không dùng MVC, ModelState, TempData hoặc HttpContext/cookie/Session. IQueryable chỉ dùng nội bộ để xây truy vấn, không xuất hiện trong hợp đồng trả về.
- Tại lần đối chiếu 12/10, Program.cs đăng ký trực tiếp từng interface M1 bằng AddScoped. Góp ý review sau đó yêu cầu gom về điểm đăng ký chung; đã điều chỉnh ngày 13/10 như phần bên dưới.
- PhienDangNhap giữ overload cũ cho Entity và thêm overload nhận DTO an toàn; việc xác minh/cập nhật principal vẫn thuộc hạ tầng.
- Snapshot và .csproj có thay đổi local từ trước lượt này; không chỉnh sửa hoặc đưa chúng vào phần bổ sung kiểm thử.

### Kiểm thử bổ sung và kết quả thực tế

Các file sửa trong lần đối chiếu: LoaiSanTests.cs, QuanLyTaiKhoanTests.cs, ServiceM1Tests.cs và tài liệu này. Không có thay đổi mới vào code ứng dụng. Bổ sung 9 ca mới (3 DI scoped, 4 lỗi đăng nhập qua HTTP, 1 antiforgery đăng nhập, 1 rate limiting); tăng kiểm tra trong các ca hiện có về trường lỗi, dữ liệu form, tên tài khoản lấy từ server, thông báo và đích redirect. Ca quyền trong transaction kiểm tra cả tạo lẫn sửa tài khoản.

| Bước | Build và kiểm thử |
| --- | --- |
| 1 — Loại sân | Build M104: 0 warning/error; chạy M104: 24/24 đạt |
| 2 — Quản lý tài khoản | Build M103: 0 warning/error; lọc QuanLyTaiKhoanTests và các ca service tài khoản/rollback: 26/26 đạt |
| 3 — Xác thực, DI và hồi quy toàn solution | Build solution: 0 warning/error; M103: 44/44, M104: 24/24, M201: 24/24; tổng 92/92, không fail/skip |

Lệnh chạy lần cuối tại gốc repo:

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --artifacts-path QuanLyDatSan_UNETI5_DHTI17A4HN/obj/m1-nghiem-thu-artifacts -p:NuGetAudit=false
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --artifacts-path QuanLyDatSan_UNETI5_DHTI17A4HN/obj/m1-nghiem-thu-artifacts --no-build --no-restore --logger "console;verbosity=minimal"
```

Hai bước đầu dùng đường dẫn csproj M104/M103 tương ứng thay cho solution. Bước 2 dùng bộ lọc `FullyQualifiedName~QuanLyTaiKhoanTests|FullyQualifiedName~GoiTrucTiepServiceVanKiemTraQuyenTrongTransaction|FullyQualifiedName~LoiSauSaveChangesRollbackVaKhongThuHoiPhien|FullyQualifiedName~ServiceTuChanDuLieuTaoKhongHopLe`.

Build đầu tiên gặp NU1301 vì sandbox chặn api.nuget.org; chạy lại với quyền kết nối NuGet đã restore/build thành công. NuGetAudit=false chỉ áp dụng cho lệnh build; không thay cấu hình repo và không phải kết quả audit dependency. Lần này chạy trên .csproj local hiện có (EF Core 10.0.12); không thay phiên bản package trong quá trình đối chiếu.

Đã rà soát diff; git diff --check đạt. SHA-256 của hai file snapshot/.csproj trước và sau giống nhau. Chưa kiểm thử transaction/unique/FK cạnh tranh trên SQL Server thật hoặc giao diện trình duyệt. Các ca SQLite chứng minh hành vi trên SQLite và luồng HTTP, không thay thế bằng chứng khóa, collation hoặc mã lỗi riêng của SQL Server. Các script cũ KiemTraM1.ps1/KiemTraDangNhap.ps1 có tạo/xóa database thử nên không chạy trong lượt này.

### Cách chia thành ba PR nếu nhóm cần review độc lập

Đây là gợi ý phân chia, chưa tách nhánh hoặc thay đổi PR #42 đã mở. Không tạo ba PR trùng nguyên commit hiện có.

| PR | Phạm vi file và bằng chứng |
| --- | --- |
| 1 — Loại sân | LoaiSanController, Services/QuanLyLoaiSan, một đăng ký DI, LoaiSanTests; CRUD, trùng tên, chặn tham chiếu, thông báo/redirect, giữ form và phân trang |
| 2 — Quản lý tài khoản | QuanLyTaiKhoanController, Services/QuanLyTaiKhoan, một đăng ký DI, QuanLyTaiKhoanTests và các ca service tài khoản; Serializable, kiểm tra quyền, Admin cuối cùng, rollback/thu hồi phiên |
| 3 — Xác thực | TaiKhoanController, Services/XacThuc, overload PhienDangNhap, một đăng ký DI và các ca xác thực; rehash, thất bại chung, rate limiting, antiforgery, Session/cookie và ReturnUrl |

Khi thực sự chia PR, mỗi nhánh xuất phát từ main đã cập nhật, chỉ chứa phần DI/test/tài liệu của nhóm đó; tách các ca trong ServiceM1Tests theo trách nhiệm để tránh phụ thuộc service chưa được merge. Fixture loại sân vẫn cần hai cột giá của M2 đã có trên main. Các PR chỉ giữ interface/DTO/mã lỗi cần cho phạm vi của mình.

### Phát hiện ngoài phạm vi

- DichVuController vẫn nhận DbContext, chưa chuyển sang IDichVuService. DI M5 nay được kích hoạt qua AddUngDungServices; việc chuyển Controller/nghiệp vụ M5 vẫn thuộc người phụ trách module đó.
- Tạo kèm/đồng bộ hồ sơ KhachHang và sai lệch snapshot/migration cần xử lý theo đầu việc tích hợp riêng. Refactor M1 không xác nhận đã hoàn thành các phần đó.

## Điều chỉnh DI theo review PR #42 ngày 13/10/2026

Chuyển ba AddScoped của M1 từ Program.cs sang AddModuleTaiKhoanVaLoaiSan trong Extensions/DependencyInjection.cs. AddUngDungServices gọi nhóm M1 và AddModuleDichVu hiện có. Program.cs thêm using Extensions và gọi AddUngDungServices đúng một lần trước builder.Build(), không giữ đăng ký M1 trùng lặp. Các đăng ký hạ tầng xác thực, Session, password hasher và phiên bản quyền giữ nguyên.

Đã đọc DichVuService trước khi kích hoạt đăng ký M5: constructor chỉ nhận DbContext, không truy vấn/ghi dữ liệu hoặc chạy tác vụ nền. AddModuleDichVu chỉ đăng ký scoped; không đổi route, quyền hay Controller/nghiệp vụ M5. Bổ sung IDichVuService vào ca DI để kiểm tra cả bốn service resolve được, có một đăng ký, cùng instance trong một scope và khác instance giữa hai scope. Kiểm tra này không thay cho kiểm thử nghiệp vụ M5.

Kiểm tra sau sửa: build toàn solution đạt 0 warning/error; M103 45/45, M104 24/24, M201 24/24 — tổng 93/93 ca đạt, không fail/skip. Dùng cùng artifacts-path `obj/m1-nghiem-thu-artifacts` như trên; build với `--no-restore`, test với `--no-build --no-restore`. git diff --check đạt. Snapshot/.csproj giữ nguyên SHA-256 trước/sau; không chạy migration hoặc thay database. Phần sửa review và bổ sung kiểm thử được đưa vào PR #42; snapshot/package local vẫn tách riêng.

Trước khi commit/push, đã tạm cất riêng snapshot/.csproj local và kiểm tra lại đúng cấu hình trong PR (ứng dụng dùng EF Core SqlServer/Design 10.0.10, dependency project test giữ 10.0.12). Build và 93/93 test tiếp tục đạt với artifacts-path `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/pr-m1-service-artifacts`, build `--no-restore`, test `--no-build --no-restore`. Hai file local được giữ riêng để khôi phục; không nằm trong commit refactor.

# Refactor Controller – Service – Repository

Ngày kiểm chứng: **27/10/2026**. Thực hiện: **Codex theo yêu cầu refactor của người dùng**; giữ header và ghi nhận tác giả của code gốc M1–M5, không gán phần refactor này thành đóng góp của các thành viên.

Tham chiếu: tài liệu `Quy_uoc_Controller_Service_Repository.md` v1.0 do người dùng cung cấp. Các câu mô tả phạm vi của lần *biên soạn tài liệu* không được hiểu thành yêu cầu chỉ sửa tài liệu trong lần refactor này.

## Phạm vi đã chuyển

Luồng nghiệp vụ: **Controller → interface Service → interface Repository → ApplicationDbContext**. Service gọi **IDonViCongViec** để lưu hoặc thực thi giao dịch. Vẫn một project MVC, không thêm framework hoặc generic repository.

| Nhóm | Thay đổi |
| --- | --- |
| Loại sân | Tách projection, phân trang, kiểm tra tên và tham chiếu sang `LoaiSanRepository`; service giữ validation và mã lỗi. |
| Tài khoản/xác thực | Dùng `TaiKhoanRepository`; giữ Serializable, kiểm tra Admin cuối cùng/quyền thực tế, nâng cấp password hash và thu hồi phiên sau commit. |
| Sân | Dùng `SanTheThaoRepository`; service xác định phạm vi quyền xem; DB lọc/sắp xếp/phân trang; công thức giá hiện hành được giữ nguyên. |
| Hồ sơ khách | Dùng `KhachHangRepository`; mã chủ tài khoản lấy từ phiên, cập nhật hồ sơ và tài khoản trong một lần lưu. |
| Đăng ký/trang chủ khách | Tách code từ `KhachHangController` vào `DangKyKhachService`, `TaiKhoanRepository`, `TrangChuKhachRepository`; giữ mật khẩu đăng ký tối thiểu 8 ký tự và vai trò KhachHang do server quyết định. |
| Đặt sân | Tách toàn bộ truy vấn, projection khả dụng, đọc tiền và SQL khóa vào `DatSanRepository`; service giữ quy tắc giờ, trạng thái, cọc, ownership và tính tiền qua M5. |
| Dịch vụ | `DichVuController` gọi `IDichVuService`; truy vấn ở `DichVuRepository`; danh sách/chi tiết dùng `DichVuDoc`. |
| Dịch vụ trong đơn | `DichVuTrongDonRepository` quản lý tracking dòng dịch vụ và đánh dấu riêng cột TienDichVu; Unit of Work lưu, service giữ giá snapshot, số lượng và trạng thái. |
| Trang chủ công khai | Dùng `TrangChuRepository`, giữ giới hạn 6 sân mỗi loại tại DB. |
| Điều hành | `QuanTriController` gọi `IDieuHanhService`; `DieuHanhRepository` thực hiện Count tại DB và trả DTO. |

Repository không gọi service/repository khác, không trả IQueryable và không tự SaveChanges/commit. Các interface không chứa kiểu EF. DI đăng ký scoped tại `Extensions/DependencyInjection.cs`, giữ `AddUngDungServices()` là điểm tích hợp.

## Hợp đồng lưu và giao dịch

Xem [IDonViCongViec](../QuanLyDatSan_UNETI5_DHTI17A4HN/Repositories/Chung/IDonViCongViec.cs) và [DonViCongViec](../QuanLyDatSan_UNETI5_DHTI17A4HN/Repositories/Chung/DonViCongViec.cs).

- `LuuAsync` dành cho thao tác dùng một lần SaveChanges nguyên tử. Không gọi trong giao dịch đang mở.
- `TrongGiaoDichAsync` nhận callback có CancellationToken, predicate xác nhận kết quả được lưu, isolation level và token request. Kết quả thất bại không lưu/commit; chỉ trả thành công sau commit.
- Giao dịch tài khoản giữ Serializable; tạo/xử lý đơn giữ ReadCommitted và khóa sân `UPDLOCK, ROWLOCK` trước khi đọc lại điều kiện. Không retry tự động.
- Cấm giao dịch/lưu lồng. Khi bắt đầu giao dịch mới, context không được chứa thay đổi chưa lưu của ca sử dụng khác.
- Rollback dùng token cleanup độc lập. Nếu rollback cũng lỗi sau deadlock/hủy request, giữ lỗi gốc và ghi log lỗi cleanup.
- Khi thất bại, detach entity bị thay đổi hoặc mới nạp trong giao dịch; giữ entity không liên quan. Phát hiện cả dữ liệu đã được AcceptChanges trước khi commit thất bại. Không Clear toàn bộ context.
- Lỗi lưu có kiểu phân biệt concurrency, tên loại sân trùng, tên tài khoản trùng, FK loại sân/sân và deadlock. Chỉ map đúng tên ràng buộc SQL đã biết; CHECK hoặc unique khác tiếp tục tới handler lỗi chung.
- Thu hồi phiên nằm sau khi Unit of Work trả về thành công; lỗi tác vụ sau commit không bị báo thành rollback database.

**Dịch vụ trong đơn giữ giao thức cạnh tranh hiện hành:** một lần lưu nguyên tử, ép UPDATE riêng TienDichVu để kiểm tra RowVersion. Không bổ sung khóa sân cho M5 trong lần refactor này. Bộ test hiện hành chủ động cho M5 ghi giữa lúc M4 tính tiền và UPDATE, rồi kiểm chứng M4 phát hiện xung đột và rollback. Đổi đồng thời giao thức khóa M4/M5 là thay đổi hợp đồng cần thiết kế riêng.

## Thay đổi hợp đồng và xử lý lỗi có chủ đích

- `IDichVuService.LayChiTietAsync` trả `DichVuDoc?`; `DanhSachDichVu.DanhSach`, ViewModel danh sách và `ChiTietDichVuTrongDon.DichVuCoTheThem` dùng DTO thay Entity. Các caller đã được cập nhật. Các service contract khác giữ nguyên; constructor chuyển sang interface repository và Unit of Work, test khởi tạo trực tiếp được cập nhật tương ứng.
- Đăng ký và quản lý danh mục dịch vụ kiểm tra lại các ràng buộc đầu vào tại service, ngoài validation form hiện có.
- Bỏ catch mọi `DbUpdateException` trong dịch vụ của đơn. Lỗi ngoài dự kiến đi tới handler chung; lỗi concurrency vẫn giữ thông báo cũ. Không biến lỗi hệ thống thành kết quả nghiệp vụ bình thường.
- Các lỗi SQL 547/unique không thuộc ràng buộc đã biết không bị gán nhầm thông báo tham chiếu/trùng tên.

Không đổi route, quyền endpoint, antiforgery, công thức tiền, trạng thái nghiệp vụ, Entity, mapping hoặc migration. Không áp dụng migration hay reset database vận hành. Test migration có sẵn chỉ chạy trên database test do fixture tự tạo.

## Ngoại lệ hạ tầng còn dùng EF

- `Services/KhoiTaoAdmin.cs`: bootstrap admin trong scope riêng.
- `Services/PhienDangNhap.cs`: hạ tầng CookieAuthenticationEvents kiểm tra phiên/quyền tài khoản.
- `Data/`, migration và bootstrap hạ tầng hiện hành.

Không có Controller hoặc service nghiệp vụ đã chuyển nào truy vấn DbContext/EF trực tiếp. Công thức và đồng hồ không có repository. Việc kiểm tra phụ thuộc bằng tìm kiếm chỉ là bước bổ trợ; kết luận hành vi dựa trên test dưới đây.

## Kiểm chứng thực chạy

Môi trường: Windows, .NET SDK **10.0.402**, SQL Server LocalDB **17.0.4025.3** và SQLite RAM của các fixture hiện có. LocalDB cần chạy ngoài sandbox vì sandbox không đọc được cấu hình instance; không phải lỗi refactor. Các fixture SQL tạo/xóa database tên riêng, có guard cleanup.

| Bộ test | Kết quả |
| --- | --- |
| M5 tính tiền | 5/5 pass |
| M103 tài khoản/xác thực/trang chủ và repository giả | 53/53 pass |
| M104 loại sân | 24/24 pass |
| M201 sân, bật cả SQL Server | 115/115 pass |
| M4 đặt sân/thanh toán, HTTP mới và Unit of Work | 196/196 pass trong lần chạy toàn solution |
| Deadlock SQL bổ sung sau lần chạy toàn solution | 1/1 pass |
| **Tổng số test khác nhau đã chạy** | **394 pass, không có test bị bỏ qua ở lần nghiệm thu** |

Build solution: **0 warning, 0 error**. `git diff --check` đạt. Diff Entity/Data/migration rỗng.

Lệnh chạy toàn solution:

```powershell
$env:QLDS_M2_TEST_SQLSERVER = '1'
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --no-restore --logger "trx;LogFilePrefix=final"
```

Test mới:

- [RepositoryServiceTests](../tests/QuanLyDatSan.M103.Tests/RepositoryServiceTests.cs): repository giả kiểm tra validation, tên chuẩn hóa, lỗi unique đúng module và CancellationToken.
- [DonViCongViecTests](../tests/QuanLyDatSan.M4.Tests/DonViCongViecTests.cs): callback thất bại, isolation, lỗi sau SQL/trước commit, cancellation, cấm lưu/giao dịch lồng, không lưu lại sau lỗi, unique/FK/CHECK, deadlock SQL thật.
- [RepositoryHttpTests](../tests/QuanLyDatSan.M4.Tests/RepositoryHttpTests.cs): đăng ký tạo hồ sơ đúng vai trò, mật khẩu 8 ký tự, trùng tên, trang chủ khách, CRUD dịch vụ qua service, antiforgery, quyền và DI của trang điều hành.

Log/TRX của phiên làm việc được giữ cục bộ trong `obj/refactor-verification/` (được Git bỏ qua). Chưa chạy nghiệm thu thủ công trên trình duyệt hoặc benchmark tải; test cạnh tranh không thay thế kiểm thử tải thực tế.

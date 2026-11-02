# Quy ước Controller – Service – Repository

**Nhóm 5 — Đề 18: Quản lý đặt sân thể thao — DHTI17A4HN**

**Phiên bản:** 1.0 — 27/10/2026.

**Điều phối:** Trần Trọng Tùng — MSSV 23103100202 (M1).

**Nội dung thực hiện:** biên soạn quy ước phân tầng, truy cập dữ liệu, transaction, lỗi và chuyển đổi code; Codex hỗ trợ rà soát và soạn tài liệu.

**Nguồn tham khảo nội bộ:** bản đề xuất Repository v1.1 của Nguyễn Hữu Quang (M5), quy ước nhóm v1.4 và hợp đồng M3/M4/M5. Giữ ghi nhận tác giả bản đề xuất; không coi tài liệu này là đóng góp triển khai code của các thành viên.

**Phạm vi:** kiến trúc đích do M1 điều phối theo yêu cầu của Tùng; áp dụng từng phần qua PR được review. Không có nghĩa toàn bộ repo đã chuyển đổi hoặc các thành viên đã duyệt. Lần biên soạn này chỉ thêm tài liệu, không refactor code hay tạo migration.

## 1. Mục tiêu và quan hệ với quy ước hiện có

- Controller xử lý HTTP; Service xử lý nghiệp vụ; Repository xử lý truy cập dữ liệu; Unit of Work quản lý lưu và transaction chung.
- Giữ một project ASP.NET Core MVC, EF Core và SQL Server hiện có. Không thêm framework kiến trúc hoặc Generic Repository chỉ để đủ tầng.
- Với phần đã chuyển đổi theo tài liệu này, thay quy định Service dùng trực tiếp DbContext tại mục phân tầng của quy ước nhóm v1.4. Các quy định nghiệp vụ, quyền, Git, tác giả và kiểm thử vẫn giữ nguyên.
- Phần chưa chuyển đổi được tiếp tục dùng Service → DbContext trong giai đoạn bàn giao. Không chỉ xóa `using` để đạt checklist hoặc buộc đổi toàn bộ module cùng lúc.
- Không thay các quyết định nghiệp vụ trong [hợp đồng M3/M4/M5](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/pull/55), [quy ước cọc](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/pull/55) và [phân công](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/pull/55). Khi chuyển đổi phải giữ chữ ký service, DTO, mã lỗi và hành vi đã thống nhất, trừ thay đổi được nêu rõ và cập nhật toàn bộ caller.
- Refactor thuần túy không thay Entity/schema, không tạo migration, không đổi route, quyền, công thức tiền, trạng thái hoặc thông báo hiện có.

## 2. Luồng gọi và trách nhiệm

```text
HTTP request
    → Controller → Service → Repository → ApplicationDbContext → SQL Server
                       └──→ Unit of Work ──────────┘
    ← View / Response ← DTO hoặc kết quả nghiệp vụ
```

| Thành phần | Được làm | Không được làm |
| --- | --- | --- |
| Controller | Bind ViewModel; ModelState; Authorize/antiforgery; gọi service; ánh xạ kết quả sang View, redirect, HTTP và TempData | Truy vấn DbContext/repository, tự tính tiền, mở transaction hoặc quyết định quy tắc chuyển trạng thái |
| Service | Validation nghiệp vụ; quyền trên bản ghi; điều phối repository/công thức; quyết định phạm vi nguyên tử; trả DTO/kết quả | Phụ thuộc MVC, TempData, ModelState, HttpContext; dùng EF/SQL trực tiếp sau khi đã chuyển đổi |
| Repository | LINQ trên DB; projection; tracking; truy vấn/thêm/xóa/đánh dấu thay đổi; thao tác khóa dữ liệu đã quy định | Quyết định nghiệp vụ, trả IActionResult/KetQuaNghiepVu, tạo thông báo giao diện, gọi service |
| Unit of Work | Dùng cùng DbContext; thực thi lưu, transaction, commit/rollback; ánh xạ lỗi lưu trữ đã biết | Quyết định khách được hủy hay không; tự tính tiền; tự commit từng repository giữa một nghiệp vụ |
| View/ViewModel | Form, validation định dạng, dữ liệu hiển thị | Truy cập DB, quyết định quyền hoặc tính tiền chính thức |

Quy tắc phân biệt: Service quyết định **điều kiện nghiệp vụ**; Repository thực thi **truy vấn để kiểm tra điều kiện đó**. Repository được lọc theo trạng thái, chủ sở hữu và thời gian; không được tự quyết định trạng thái nào được phép chuyển sang trạng thái nào.

## 3. Controller

1. Dùng interface service; không inject repository hoặc DbContext vào controller nghiệp vụ.
2. Nhận ViewModel riêng cho form, không bind Entity EF. Không nhận role, người xác nhận, giá chính thức hoặc mã chủ tài khoản từ client làm căn cứ tin cậy.
3. Dùng `[Authorize]` theo quyền endpoint. Các thao tác ghi dùng POST và anti-forgery; GET không tạo/sửa/xóa dữ liệu.
4. ModelState kiểm tra định dạng/độ dài/bắt buộc; khi lỗi nạp lại dữ liệu lựa chọn qua service và giữ dữ liệu form phù hợp.
5. Lấy danh tính qua cơ chế tài khoản hiện có; không tạo hệ thống xác thực khác. Định danh từ route/form vẫn phải được service kiểm tra quyền sở hữu.
6. Ánh xạ mã lỗi sang thông báo/HTTP; không phân tích chuỗi lỗi để quyết định nhánh. Dùng key TempData đang có: `Success`, `Error`, `Warning`, `Info`.
7. Không trả SQL, stack trace hoặc connection string cho người dùng. Lỗi ngoài dự kiến đi qua xử lý lỗi chung.

## 4. Service

1. Mỗi phương thức thể hiện một ca sử dụng: tạo đơn, hủy đơn, đổi trạng thái tài khoản, xác nhận cọc… Không chỉ sao chép CRUD repository mà bỏ nghiệp vụ.
2. Kiểm tra lại đầu vào nghiệp vụ dù controller đã kiểm tra ModelState; service có thể được gọi từ nơi khác.
3. Kiểm tra quyền đối với bản ghi và tài khoản hiện tại bằng cơ chế M1; thao tác nhạy cảm nạp lại quyền/trạng thái bên trong phạm vi giao dịch cần nhất quán.
4. Quyền ở controller không thay thế quyền sở hữu trong service. Khách đổi ID không được xem/sửa dữ liệu người khác; kết quả không tồn tại/không thuộc khách theo hợp đồng hiện hành.
5. Tính tiền qua `ITinhTienService` của M5; giá, khách và dữ liệu chính thức lấy từ server. Không nhân bản công thức trong service khác hoặc View.
6. Không dùng DbContext, DbSet, EF Entry, SQL hoặc exception EF/provider sau khi chuyển đổi. LINQ trên collection trong bộ nhớ vẫn được dùng khi phù hợp; không cấm mọi `Where`/`Select`.
7. Không tải toàn bộ bảng để lọc/phân trang trong service. Gửi bộ lọc đã chuẩn hóa xuống repository để truy vấn ở database.
8. Giữ các kết quả hiện có như `KetQuaLoaiSan`, `KetQuaQuanLyTaiKhoan`, `KetQuaXuLyDon`, `KetQuaNghiepVu`. Không buộc tất cả module đổi sang một kiểu kết quả chung.
9. DTO ra controller không chứa Entity EF, IQueryable, DbContext hoặc thành phần MVC. Entity được phép đi giữa repository và service để cập nhật; không đi thẳng ra form/View.
10. Gọi service khác chỉ theo hướng phụ thuộc đã chốt, không tạo vòng DI. Service con tham gia transaction chung không tự lưu/commit độc lập.

## 5. Repository

1. Tổ chức theo nhóm dữ liệu/ca sử dụng có liên quan, không tạo một repository cho mỗi action hoặc bắt buộc một repository cho mỗi bảng. Không dùng `Repository<T>` CRUD chung trong đợt này.
2. Chỉ thêm phương thức đang cần. Tên nói rõ mục đích: lấy chi tiết, tìm danh sách, kiểm tra tồn tại, nạp để cập nhật…
3. Không trả IQueryable, DbSet, EF Entry hoặc truy vấn chưa được thực thi ra service. Trả DTO, entity, danh sách hoặc giá trị vô hướng.
4. Ưu tiên DTO projection cho đọc. Khi đọc entity chỉ để xem, dùng AsNoTracking; khi sửa, nạp entity tracking trong cùng scope. Truy vấn scalar/projection thuần DTO không cần thêm AsNoTracking máy móc.
5. Phân biệt rõ phương thức đọc và nạp để cập nhật, ví dụ `LayChiTietAsync` / `LayDeCapNhatAsync`. Không Attach/Update một Entity do client gửi vào.
6. Truy vấn async nhận CancellationToken cuối cùng và truyền xuyên suốt. Hàm chỉ đánh dấu thay đổi trong bộ nhớ không cần hậu tố Async giả.
7. Thực hiện lọc, sort, phân trang, Count và phép tổng hợp thích hợp trên DB; sort phân trang có tiêu chí phụ ổn định. Tránh N+1, không tải cả bảng để trả vài dòng.
8. Có thể truy vấn nhiều bảng/join theo hợp đồng; không đồng nghĩa có quyền sửa tất cả bảng đó.
9. Repository không gọi service hoặc repository khác. Có thể dùng helper hạ tầng thuần túy/ILogger; không phụ thuộc MVC hoặc tài khoản từ HttpContext.
10. Quyền ghi bảng/cột chung do người phụ trách thống nhất. Cập nhật `DatSan.TienDichVu` phải phối hợp phiên bản/khóa với M4; không Update toàn entity khiến ghi đè cột của module khác.
11. Repository chuẩn bị thay đổi; **không tự gọi SaveChanges hoặc commit**. Việc lưu do Unit of Work cùng scope thực hiện.

## 6. Unit of Work và transaction — bắt buộc chốt trước khi chuyển phần ghi

### 6.1. Một đầu mối lưu

- Tên hợp đồng dùng chung dự kiến: `IDonViCongViec`; triển khai EF: `DonViCongViec`. Đây là tên thiết kế, chưa khẳng định đã có trong repo.
- Repository và Unit of Work đăng ký scoped, dùng cùng một `ApplicationDbContext` trong một thao tác. Không tự tạo context riêng trong từng repository.
- `SaveChanges` lưu mọi thay đổi đang được theo dõi trong context, không chỉ entity thuộc repository vừa gọi. Vì vậy không đặt `LuuAsync` độc lập ở mỗi repository.
- Không gọi đồng thời các truy vấn trên cùng DbContext, không chia sẻ scope/context giữa request.
- Khi chuyển đổi dần, không để service cũ có SaveChanges riêng được gọi bên trong một luồng mới mà chưa kiểm tra phạm vi lưu/commit.

### 6.2. Hợp đồng thực thi

M1 cùng M4/M5 phải cung cấp implementation và test chung trước khi các module chuyển phần ghi nhiều bước. Contract tối thiểu phải quy định:

| Nội dung | Yêu cầu |
| --- | --- |
| Chủ giao dịch | Service của ca sử dụng quyết định phạm vi; Unit of Work thực thi kỹ thuật |
| Isolation level | Hỗ trợ lựa chọn cần thiết; M1 quản lý tài khoản giữ Serializable, không âm thầm thay bằng mặc định |
| Callback nghiệp vụ | Nếu dùng callback, nhận CancellationToken; kết quả phải có chỉ dấu rõ được lưu hay thất bại, không dùng `Func<Task<T>>` rồi mặc định mọi lần trả về đều thành công |
| Thứ tự | Mở transaction → lấy khóa → nạp/kiểm tra lại dữ liệu → áp dụng nghiệp vụ → SaveChanges → commit |
| Kết quả thất bại | Không lưu hoặc commit; rollback phần đã ghi, xử lý tracking trước khi tiếp tục sử dụng scope |
| Kết quả thành công | Chỉ trả thành công sau khi lưu và commit hoàn tất; SaveChanges thành công chưa đồng nghĩa commit thành công |
| Lỗi/hủy yêu cầu | Rollback/dispose đúng cách; không nuốt OperationCanceledException hoặc đổi thành thành công; cleanup không bị bỏ qua chỉ vì token request đã hủy |
| Gọi lồng | Cấm mở transaction/commit độc lập trong service con; tham gia phạm vi hiện có theo hợp đồng hoặc tách helper chuẩn bị thay đổi |
| Tác vụ sau commit | Thu hồi phiên/thông báo chỉ chạy sau commit thành công; lỗi sau commit không được báo như DB đã rollback. Cần bảo đảm gửi tin bền vững thì thiết kế cơ chế riêng |

Không đưa một lớp bọc giao dịch chưa đầy đủ vào dùng chung chỉ để service hết EF. PR nền phải có kiểm thử chứng minh các nhánh trên.

### 6.3. Giữ nguyên bảo đảm hiện có

- M1: giữ kiểm tra Admin cuối cùng, quyền hiện tại và đồng bộ tài khoản/hồ sơ trong cùng phạm vi nhất quán; chỉ vô hiệu hóa phiên sau commit.
- M3/M4: kiểm tra lịch và ghi đơn trong transaction có khóa sân hiện có; không đổi thành AnyAsync rồi lưu riêng rẽ.
- M4/M5: thống nhất thứ tự khóa sân → đơn/giao dịch, kiểm tra RowVersion và trạng thái dưới khóa. Không dùng khóa trong bộ nhớ thay bảo đảm database.
- RowVersion từ form được kiểm tra đúng định dạng và so với bản ghi hiện hành; hạ tầng phải giữ cơ chế optimistic concurrency khi ghi. Nếu cần ép UPDATE đơn khi thay đổi dòng dịch vụ, đóng gói EF Entry/IsModified vào phương thức repository rõ mục đích.
- Rollback không tự giải quyết toàn bộ trạng thái entity trong bộ nhớ. Sau lỗi phải detach/reload phần liên quan hoặc kết thúc scope theo chính sách chung; không Clear toàn context tùy tiện làm mất thay đổi khác.
- Không tự retry thao tác có tác dụng phụ; chỉ retry có giới hạn khi đã chứng minh idempotency và giữ nguyên hợp đồng lỗi. Unique/FK/RowVersion vẫn phải bảo vệ ở DB.

## 7. Mã lỗi và kết quả lưu

Phân biệt ba loại:

| Loại | Nơi nhận diện | Cách xử lý |
| --- | --- | --- |
| Đầu vào HTTP sai | Controller/ViewModel | ModelState, không gọi thao tác ghi |
| Điều kiện nghiệp vụ không đạt | Service | Trả mã lỗi nghiệp vụ hiện có; controller ánh xạ thông báo |
| Ràng buộc/lỗi lưu trữ | Repository hoặc Unit of Work ở ranh giới EF | Ánh xạ trường hợp đã biết sang kết quả hạ tầng có kiểu; service chuyển sang lỗi nghiệp vụ |

- Kết quả hạ tầng cần phân biệt xung đột phiên bản, trùng khóa và vi phạm tham chiếu được nhận diện. Cần định danh ràng buộc/loại xung đột đủ rõ để service không nhầm trùng tên với trùng giao dịch.
- Không ánh xạ mọi lỗi SQL 547 thành “đang được tham chiếu” hoặc mọi lỗi unique thành “trùng tên” trên toàn hệ thống. Giới hạn mapping theo thao tác/ràng buộc đã biết.
- Giữ mapping hiện hành: loại sân trùng tên/đang được tham chiếu; tài khoản trùng tên/deadlock; đơn đặt xung đột phiên bản… Không gom thành một `LoiCoSoDuLieu` làm thay đổi thông báo/hành vi.
- Lỗi ngoài dự kiến được ghi log và truyền tới handler chung; không che thành kết quả nghiệp vụ bình thường. Log không chứa mật khẩu, token hoặc chuỗi kết nối.
- Service không bắt DbUpdateException/SqlException. Tầng truy cập dữ liệu xử lý chi tiết provider; kết quả/exception có kiểu riêng không để lộ EF ra service.
- Không bắt lỗi chỉ tại SaveChanges rồi coi đã bao phủ toàn bộ: lỗi có thể phát sinh lúc truy vấn, lấy khóa hoặc commit.
- Không ép đổi tất cả kết quả service trong PR refactor. Nếu cần bổ sung mã lỗi, phải cập nhật caller, mapping và test trong phạm vi được thống nhất.

## 8. Cấu trúc thư mục, tên và DI

```text
Controllers/
ViewModels/
Services/
  <NhomChucNang>/
    I<Ten>Service.cs
    <Ten>Service.cs
    <DTO va ket qua nghiep vu>
Repositories/
  <NhomChucNang>/
    I<Ten>Repository.cs
    <Ten>Repository.cs
  Chung/
    IDonViCongViec.cs
    DonViCongViec.cs
    <Ket qua va ma loi luu tru>
Data/
  ApplicationDbContext.cs
Extensions/
  DependencyInjection.cs
```

- Giữ một project; interface không phụ thuộc kiểu EF dù nằm cùng thư mục triển khai.
- Namespace theo project/thư mục; tên tiếng Việt không dấu, PascalCase cho kiểu/phương thức, camelCase cho biến/tham số. Tên kỹ thuật Controller/Service/Repository giữ nhất quán.
- Không bắt buộc lớp `RepositoryCoSo`; chỉ thêm khi có nhu cầu dùng chung thực tế. Không sao chép mẫu lớp cơ sở nuốt mọi lỗi DB.
- Đăng ký repository scoped trong `AddModuleXxx` tại `Extensions/DependencyInjection.cs`. Unit of Work dùng chung do M1 tích hợp một lần; không đăng ký singleton.
- Giữ `AddUngDungServices()` là điểm gọi chung; không rải đăng ký trực tiếp vào Program.cs hoặc đăng ký lặp interface.
- Header ghi đúng người thực hiện, MSSV, phần việc. File nhiều người ghi phần đóng góp thực tế; không thay tên tác giả cũ bằng người refactor.

## 9. Ví dụ phân trách nhiệm

**Khách hủy đơn:**

1. Controller nhận ID/phiên bản, kiểm tra đăng nhập/token/định dạng, gọi service hủy hiện có.
2. Service xác định khách hiện tại; nhờ repository nạp đơn giới hạn theo chủ sở hữu.
3. Service điều phối transaction và khóa; nạp lại dữ liệu, kiểm tra quyền, thời gian, trạng thái và cọc theo hợp đồng.
4. Repository thực hiện truy vấn/khóa và theo dõi thay đổi; không tự quyết định khách có được hủy.
5. Service áp dụng quy tắc hủy; Unit of Work lưu/commit hoặc rollback.
6. Controller ánh xạ kết quả sang thông báo/redirect; không tự đổi trạng thái.

**Tìm sân:** Controller nhận bộ lọc → Service chuẩn hóa/kiểm tra → Repository lọc, sort, Count, Skip/Take và Select DTO trên DB → Service trả kết quả → Controller trả View.

**Tính tiền:** Service điều phối lấy giá snapshot/dòng dịch vụ từ repository → gọi `ITinhTienService` tính tiền thuần túy → lưu qua Unit of Work nếu là thao tác chốt. Không tạo repository cho phép tính thuần túy.

## 10. Phạm vi từng module và thứ tự chuyển đổi

| Phần | Chủ trì | Điều kiện cần giữ |
| --- | --- | --- |
| Hạ tầng Unit of Work/lỗi/DI chung | M1, M4/M5 phối hợp review | Một context, transaction và lỗi rõ ràng; test trước khi dùng |
| Danh mục dịch vụ | M5 | Thử nghiệm đầu tiên sau khi có nền lưu; giữ validation, quyền và thông báo |
| Sân/tìm kiếm | M2 | Lọc/sort/phân trang trên DB, ràng buộc, giá và khả dụng |
| Khách hàng/hồ sơ | M3 | Ownership và đồng bộ họ tên/email tài khoản–khách hàng nguyên tử |
| Loại sân, tài khoản, xác thực | M1 | FK/unique, Admin cuối cùng, Serializable, phiên và hành vi đăng nhập |
| Đặt sân/trạng thái/cọc | M4 phối hợp M3/M5 | Khóa sân, RowVersion, rollback, tiền và hợp đồng dùng chung |
| Dịch vụ trong đơn/chốt tiền | M5 phối hợp M4 | Giá snapshot, trạng thái, cạnh tranh cập nhật và chốt đơn |

- Chuyển theo từng ca sử dụng build được; không tạo PR đổi cả M1–M5.
- Ưu tiên phần đọc và danh mục ít phụ thuộc; phần tài khoản/đặt sân chuyển sau khi hạ tầng transaction được kiểm chứng.
- `QuanTriController` đang có truy vấn trực tiếp ở bản local đã kiểm tra: cần chuyển qua service rồi repository; phối hợp M3 và M5 để chốt trách nhiệm trang điều hành/thống kê.
- Giữ service công thức/đồng hồ không phụ thuộc repository. DbContext/mapping/migration và bootstrap/seed vẫn được dùng EF trực tiếp theo phạm vi hạ tầng.
- Thành phần xác thực hạ tầng như kiểm tra cookie/phiên được khảo sát riêng; không gộp thay cơ chế đăng nhập vào PR repository. Ngoại lệ EF phải được ghi cụ thể, không mở ngoại lệ chung cho mọi service.

## 11. Quy trình và kiểm thử

1. Kiểm tra nhánh, diff, quy ước và code thực tế; bảo toàn thay đổi local. Ghi phạm vi service và hành vi trước refactor.
2. Liệt kê truy vấn, quyền, mã lỗi, transaction, khóa, RowVersion, tác vụ sau commit và caller/test bị ảnh hưởng.
3. Chốt phương thức repository và cách tham gia Unit of Work trước khi chuyển phần ghi; không đưa stub trả thành công vào main.
4. Tách truy vấn, thay constructor và DI, giữ public service contract. Không sửa schema hoặc làm thêm nghiệp vụ trong cùng PR.
5. Chạy test service hiện có và bổ sung ca hồi quy cần thiết; kiểm thử repository với database phù hợp.
6. Chạy build, xem toàn bộ diff, xác nhận migration/model không đổi. Không tự tạo/reset database vận hành để test.
7. PR ghi rõ phạm vi đã chuyển, phần còn dùng EF, file chung, test và phần chưa kiểm chứng.

**Kiểm thử bắt buộc theo phạm vi:**

- Unit test service dùng repository giả cho validation, quyền và nhánh nghiệp vụ; mock không chứng minh LINQ dịch đúng sang SQL.
- Integration test trên SQL Server cho unique/FK, collation, RowVersion, transaction, khóa sân, deadlock và gửi đồng thời khi có liên quan. Không dùng EF InMemory để kết luận các bảo đảm này đạt.
- Với nền transaction: callback thất bại không ghi; lỗi lưu/commit không trả thành công; hủy request dọn transaction; thao tác thất bại không bị lưu lại trong scope; service con không commit sớm.
- Hồi quy endpoint: đúng/sai role, khóa tài khoản, đổi ID, thiếu token, lỗi form, redirect, thông báo và giao diện hiện có.
- So sánh bộ lọc/sort/phân trang trước và sau; không giảm tính đúng hoặc tải toàn bảng do tách lớp.
- Ghi rõ môi trường và kết quả thực chạy; không lấy test cũ làm bằng chứng nghiệm thu code mới.

## 12. Git, review và checklist

- Nhánh theo quy ước nhóm: `feature/mX-ten-dau-viec`, ví dụ `feature/m5-repository-dich-vu`.
- Commit: `[MSSV] [Module] Noi dung cong viec`, ví dụ `[23103100184] [M5] Tach truy van dich vu sang repository`.
- PR theo template repo; refactor không migration phải ghi rõ. Không đổi mẫu commit sang `refactor(m5): ...` nếu nhóm chưa duyệt đổi quy ước Git.
- Chỉ stage file đúng đầu việc, không `git add .` khi workspace lẫn thay đổi. Không tự commit migration local ngoài phạm vi.
- File chung cần phối hợp chủ trì; cập nhật caller/test cùng PR hoặc chuỗi PR có thứ tự build được. Không force push nhánh dùng chung.
- Một thành viên khác review; Tùng merge bằng **Create a merge commit** khi đủ điều kiện. Chỉ dùng `Closes #...` khi hoàn thành toàn bộ Issue thực tế.

Checklist cho phần đã chuyển đổi:

- [ ] Controller chỉ gọi service; không DbContext/repository.
- [ ] Service không phụ thuộc EF/MVC; giữ validation, quyền và kết quả hiện hành.
- [ ] Repository không trả IQueryable/Entity ra controller, không quyết định nghiệp vụ, không tự SaveChanges/commit.
- [ ] Truy vấn và tracking đúng mục đích; không tải toàn bảng hoặc N+1 ngoài ý muốn.
- [ ] Một Unit of Work/context cho một thao tác nguyên tử; lỗi và rollback được kiểm thử.
- [ ] Giữ isolation level, khóa, RowVersion và tác vụ sau commit.
- [ ] Mapping lỗi không làm mất thông tin nghiệp vụ hoặc che lỗi hệ thống.
- [ ] DI, header tác giả, tên nhánh/commit đúng quy ước.
- [ ] Build/test phù hợp đã chạy; ghi rõ ca chưa chạy.
- [ ] Không thay schema/migration, route hoặc nghiệp vụ ngoài phạm vi.
- [ ] Đã review diff và ghi rõ phần còn chuyển đổi sau.

Tìm kiếm tên DbContext/EF chỉ là kiểm tra sơ bộ, không đủ kết luận kiến trúc hoặc nghiệp vụ đúng.

## 13. Tài liệu kỹ thuật tham khảo

- [Microsoft: Transactions trong EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions) — phân biệt SaveChanges, commit và phạm vi giao dịch.
- [Microsoft: Choosing a testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy) — repository hỗ trợ mock nhưng vẫn cần kiểm thử truy cập dữ liệu thực tế.

Việc thêm Repository/Unit of Work là lựa chọn của nhóm cho kiến trúc đích, không phải khẳng định ASP.NET Core MVC hoặc đề bài bắt buộc mọi dự án có các tầng này.

# M4 — Refactor Controller/Service và kết quả kiểm thử

Ngày thực hiện: 13/10/2026. Codex hỗ trợ theo yêu cầu người dùng.
Đối chiếu quy ước nhóm v1.4, đặc biệt mục 17.

> Cập nhật sau đợt refactor: đã bổ sung màn hình nhân viên/Admin theo issue #22.
> Xem [M4-02 — Danh sách và chi tiết đơn](M4-02-Danh-sach-va-chi-tiet-don.md)
> để biết chức năng hiện tại và kết quả hồi quy 231 test. Phạm vi và số liệu dưới đây
> ghi nhận thời điểm refactor ban đầu.

## 1. Phạm vi thực tế

Code M4 hiện có nằm trong `DatSanService`: xác nhận, từ chối, khách hủy,
nhân viên hủy, hoàn thành và cơ chế kiểm tra lịch/khóa dùng chung M3–M4.
`DonDatController` hiện là controller của khách hàng (M3), dùng chung service này.
Chuyển toàn bộ truy vấn của controller đó sang service, gồm GET danh sách,
chi tiết, dropdown và nạp lại dropdown khi POST lỗi.

Chưa có controller/view tiếp nhận và xử lý đơn cho nhân viên trong bản code được kiểm tra.
Kết quả dưới đây xác nhận nghiệp vụ M4 ở tầng service và HTTP hiện có của khách;
chưa phải nghiệm thu toàn bộ giao diện M4 cho nhân viên.

## 2. Cấu trúc sau refactor

| File | Trách nhiệm |
| --- | --- |
| `Controllers/DonDatController.cs` | Phân quyền, ModelState, ánh xạ DTO sang ViewModel, View/redirect/TempData |
| `ViewModels/ThongBaoLoiDatSanViewModel.cs` | Ánh xạ đầy đủ mã lỗi sang trường form và thông báo tiếng Việt |
| `Services/DatSan/IDatSanService.cs` | Interface dùng chung M3–M4 |
| `Services/DatSan/HopDongDatSan.cs` | DTO yêu cầu/kết quả, mã lỗi, kết quả nghiệp vụ có invariant |
| `Services/DatSanService.cs` | Tạo đơn, kiểm tra giao lịch, khóa sân, công thức dự kiến hiện có |
| `Services/DatSan/DatSanService.Doc.cs` | Query danh sách/chi tiết/dropdown, trả DTO đã materialize |
| `Services/DatSan/DatSanService.XuLy.cs` | Nghiệp vụ M4 và transaction xử lý đơn |

Các file `partial` vẫn là **một** `DatSanService`, cùng DbContext scoped và cùng cơ chế khóa.
Đăng ký `IDatSanService`/`IDongHo` trong Program đã có và được kiểm tra qua HTTP host thật.
Không thêm tầng repository. Controller không còn phụ thuộc EF Core/DbContext.
Bỏ thuộc tính entity `DanhSach` không được sử dụng trong `DanhSachDonViewModel`.

Route `/DonDat/Index`, `/DonDat/ChiTiet/{id}`, `/DonDat/Tao`, quyền KhachHang,
thông báo form, TempData và redirect được giữ. Entity, mapping, migration và enum trạng thái
không thay đổi trong đợt này.

## 3. Hợp đồng dùng chung cần M3/M4 nắm

- `TaoDonAsync`: các tham số rời chuyển thành `TaoDonYeuCau`; controller lấy MaTaiKhoan
  từ `ITaiKhoanHienTai`, không bind từ form.
- `XacNhanDonAsync`: nhận `XuLyDonYeuCau` chứa mã đơn và RowVersion.
- Từ chối/hủy: nhận `HuyDonYeuCau`; khách hủy còn truyền mã tài khoản từ phiên tin cậy.
- `HoanThanhDonAsync`: nhận `HoanThanhDonYeuCau`; tiền là dữ liệu nội bộ tin cậy,
  không phải DTO được bind từ trình duyệt.
- Bổ sung `LayDanhSachCaNhanAsync`, `LayChiTietCaNhanAsync`, `LayLuaChonSanAsync`.
  Không trả IQueryable, entity EF hoặc SelectListItem. Controller chỉ Select trên DTO đã tải.
- `KetQuaTaoDon` và `KetQuaXuLyDon` có constructor private: thành công không kèm lỗi,
  thất bại bắt buộc có mã lỗi hợp lệ. Controller ánh xạ mã không biết thành exception để
  phát hiện hợp đồng thiếu, không âm thầm coi là thành công.
- Caller sản phẩm hiện có của `TaoDonAsync` đã cập nhật tại `DonDatController`.
  Các phương thức chuyển trạng thái chưa có caller HTTP; test gọi service trực tiếp.
- Exception database ngoài dự kiến đi lên handler toàn cục; bỏ catch DbUpdateException
  quá rộng vốn biến lỗi hệ thống thành thông báo nghiệp vụ của thao tác tạo đơn.

## 4. Hai điều chỉnh bảo vệ dữ liệu được kiểm thử riêng

1. Khách hủy đơn: lọc chủ sở hữu ngay từ truy vấn tìm đơn, trước kiểm tra RowVersion.
   Mã đơn của khách khác trả `KhongTonTai` với cả phiên bản đúng và cũ.
2. Khi SaveChanges báo xung đột: rollback transaction và detach đơn đang sửa.
   Một lần SaveChanges tiếp theo trong cùng scope không được lưu thao tác đã báo thất bại.

Đây là các điều chỉnh bảo vệ dữ liệu ngoài việc di chuyển code. Khi tách Issue/PR theo quy ước
nhóm, cần nêu riêng hai thay đổi này và các test đi kèm để người review kiểm tra.

## 5. Kiểm thử đã chạy

Môi trường: .NET 10, EF Core SQL Server 10.0.12, SQL Server LocalDB
`(localdb)\MSSQLLocalDB`. Chạy trên bản sao tạm của source project đang dùng ở ổ C.

| Bộ test | Đạt | Lỗi | Bỏ qua |
| --- | ---: | ---: | ---: |
| `QuanLyDatSan.M4.Tests` | 58 | 0 | 0 |
| `QuanLyDatSan.M103.Tests` | 45 | 0 | 0 |
| `QuanLyDatSan.M104.Tests` | 24 | 0 | 0 |
| `QuanLyDatSan.M201.Tests` | 24 | 0 | 0 |
| Tổng | **151** | **0** | **0** |

Build trong lệnh test thành công. Có cảnh báo NU1900 vì không lấy được dữ liệu kiểm tra
lỗ hổng package từ NuGet; kết quả này không xác nhận audit dependency thành công.

Nhóm ca M4:

- Xác nhận hợp lệ; sai trạng thái; sân/loại sân ngừng hoạt động; bảo trì; ngoài giờ; quá giờ.
- Trùng lịch, hai ca nối tiếp, đơn hủy/từ chối không giữ chỗ, đơn hoàn thành giữ lịch sử chiếm chỗ.
- Từ chối/hủy có lý do; trim/độ dài; khách chỉ hủy đơn của mình còn chờ trước giờ bắt đầu;
  nhân viên hủy theo trạng thái, thời gian và cọc.
- Hoàn thành: đúng trạng thái, đến giờ kết thúc, xác nhận thu đủ, số tiền không âm,
  tổng không thấp hơn cọc; ghi tiền/trạng thái/thời điểm cùng transaction.
- RowVersion cũ, mã đơn/phiên bản sai, không mở lại trạng thái kết thúc.
- Hai yêu cầu tạo cùng sân/giờ và hai yêu cầu xác nhận cùng đơn: dùng hai DbContext,
  một transaction giữ khóa sân và interceptor quan sát cả hai lệnh chờ khóa trước khi thả.
  Chỉ một yêu cầu thành công. Không dùng EF InMemory hoặc khóa trong bộ nhớ để chứng minh tính đúng.
- Lỗi được chèn sau SQL SaveChanges nhưng trước commit: tiền và trạng thái rollback.
  Lỗi concurrency được chèn riêng để kiểm tra cleanup tracking.
- HTTP thực qua MVC host: cookie đăng nhập, quyền GET/POST hiện có, antiforgery,
  chi tiết riêng tư, form lỗi/dropdown, redirect/TempData, chống giả mạo giá/cọc/trạng thái.
- Lọc/phân trang và DTO không còn tracking; Production exception trả HTTP 500,
  trang lỗi chung không lộ exception nội bộ và có log. Cơ chế tạo lỗi chỉ nằm trong project test.

Chạy tại thư mục chứa `.slnx`:

```powershell
dotnet test .\tests\QuanLyDatSan.M4.Tests\QuanLyDatSan.M4.Tests.csproj --logger "trx;LogFileName=m4.trx"
dotnet test .\QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --logger "trx;LogFileName=regression.trx"
```

Test tạo database ngẫu nhiên `QuanLyDatSan_M4Test_<guid>` và tự xóa đúng database đó khi
kết thúc. Không sử dụng `QuanLyDatSan_Local`. Mỗi class test có database riêng.
Có thể đặt biến `M4_TEST_SQL_SERVER` thành tên SQL Server khác có Windows Authentication
và quyền tạo/xóa database kiểm thử. Máy không có SQL Server phù hợp sẽ báo test thất bại,
không âm thầm chuyển sang InMemory.

Fixture dùng `EnsureCreated` từ model hiện hành, vì vậy **không kiểm chứng lịch sử migration**.
TRX nằm trong `tests/<tên project>/TestResults/` sau khi chạy; không đưa bin/obj/TRX vào source.

## 6. Phần chưa có cần triển khai riêng

- Danh sách/chi tiết xử lý cho nhân viên với tìm/lọc/sắp xếp và POST xác nhận/từ chối/hủy/hoàn thành;
  các endpoint đó phải phân quyền Admin/NhanVien, kiểm tra antiforgery và gửi RowVersion.
- HTTP hủy cho khách cũng chưa có; service đã kiểm tra quyền sở hữu, nhưng chưa có nút/action gọi.
- Chốt hợp đồng M5: chưa có `ITinhTienService` để tự tính tiền khi hoàn thành.
  Không nối `HoanThanhDonYeuCau` trực tiếp vào form nhận tiền từ khách.
- Chưa kiểm thử UI trình duyệt thủ công hoặc migration trong đợt refactor này.

Tài liệu này là bàn giao local; không xác nhận đã tạo Issue, PR hoặc được nhóm review/merge.

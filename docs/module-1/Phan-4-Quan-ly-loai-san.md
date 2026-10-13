# M1-04 — Quản lý loại sân

Phụ trách: **Trần Trọng Tùng — MSSV 23103100202**. Codex hỗ trợ triển khai và kiểm thử. Ngày thực hiện: **09/10/2026**. Phạm vi theo [issue #10](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/10): quản lý danh mục loại sân chỉ dành cho Admin, kiểm tra dữ liệu và bảo toàn tham chiếu khi xóa.

## 1. Chức năng và quyền

| Endpoint | Phương thức | Chức năng |
| --- | --- | --- |
| `LoaiSan/DanhSach` | GET | Tìm tên, lọc trạng thái, sắp xếp tên A–Z/Z–A, phân trang 10 dòng trên truy vấn; giữ điều kiện khi đổi trang. |
| `LoaiSan/ChiTiet/{id}` | GET | Thông tin loại sân và số sân tham chiếu. |
| `LoaiSan/Tao` | GET, POST | Thêm loại sân bằng form riêng. |
| `LoaiSan/Sua/{id}` | GET, POST | Sửa tên, mô tả, số người, giá mặc định và trạng thái. |
| `LoaiSan/Xoa/{id}` | GET, POST | GET hiển thị xác nhận/khóa thao tác; POST kiểm tra lại và xóa nếu chưa được tham chiếu. |

Controller đặt `[Authorize(Roles = nameof(VaiTro.Admin))]`, không có action anonymous. Khách chưa đăng nhập được chuyển đến đăng nhập; Nhân viên/Khách hàng bị chuyển đến trang từ chối truy cập theo cookie handler có sẵn. Tất cả POST có antiforgery. GET không thay đổi dữ liệu. Trang quản trị không cache.

Menu **Loại sân** chỉ hiện với Admin, active ở danh sách/chi tiết/thêm/sửa/xóa. Các View dùng `_AdminLayout`, không tạo lại header hoặc footer. Không thay đổi cơ chế xác thực và thu hồi phiên của M1-03.

## 2. Validation và bảo toàn dữ liệu

- Tên bắt buộc, tối đa 100 ký tự, được Trim; mô tả tối đa 1000 ký tự, chuỗi trắng lưu thành null.
- So sánh trùng trực tiếp trên cột dùng `Vietnamese_100_CI_AS`: không phân biệt hoa/thường, phân biệt dấu. Khi sửa loại trừ mã hiện tại. Unique index có sẵn là lớp bảo vệ cuối; bắt lỗi SQL Server 2601/2627 để trả thông báo vào form nếu có tranh chấp.
- Số người là số nguyên dương; đơn giá là số đồng nguyên không âm, tối đa 9.999.999.999.999.999, nằm trong cột `decimal(18,2)`. Dùng trường nullable + Required để phát hiện thiếu dữ liệu thay vì tự nhận 0.
- Trạng thái bắt buộc, chỉ `NgungHoatDong = 0` hoặc `HoatDong = 1`; cập nhật bằng giá trị cụ thể, không đảo trạng thái theo mỗi lần gửi form.
- Form không bind Entity hoặc `DanhSachSan`; mã loại lấy từ route của thao tác sửa/xóa. Validation và quyền đều được kiểm tra phía server.
- Sửa giá loại sân chỉ thay đổi giá mặc định, không sửa `SanTheThao.DonGia`, trạng thái sân hoặc lịch sử giao dịch. M2/M3/M4 phải kiểm tra trạng thái loại sân trong nghiệp vụ tương ứng; M1-04 không triển khai thay những luồng đó.
- Chỉ xóa loại sân khi không có bất kỳ sân nào tham chiếu, kể cả sân ngừng hoạt động. Loại có tham chiếu bị chặn xóa và hướng dẫn sang form cập nhật trạng thái; không tự ngừng hoạt động khi người dùng yêu cầu xóa.
- POST kiểm tra lại tham chiếu dù GET xác nhận trước đó chưa có. FK `NoAction` có sẵn ngăn xóa nếu tham chiếu xuất hiện sau bước kiểm tra; lỗi SQL Server 547 được chuyển thành thông báo, không cascade xóa sân.
- Mã không tồn tại trả 404. Bộ lọc enum, số trang sai kiểu, từ khóa quá dài hoặc lựa chọn sắp xếp không hợp lệ trả 400. Xóa đồng thời khiến bản ghi không còn cũng trả 404; sửa bản ghi đã bị xóa trả thông báo yêu cầu về danh sách.
- Chưa bổ sung RowVersion cho danh mục: hai Admin sửa cùng bản ghi vẫn theo lần lưu cuối. Không nhận đây là tính năng kiểm soát xung đột chỉnh sửa.

## 3. File và tích hợp

Đường dẫn dưới đây tính từ project ứng dụng nếu không ghi khác:

| File | Trách nhiệm |
| --- | --- |
| `Controllers/LoaiSanController.cs` | Endpoint Admin, LINQ, validation trùng, lưu và chặn xóa tham chiếu. |
| `ViewModels/LoaiSanViewModel.cs` | Form với Data Annotation, dữ liệu danh sách/chi tiết. |
| `Views/LoaiSan/` | Danh sách, chi tiết, thêm, sửa, xác nhận xóa, partial form và chọn layout. |
| `Views/LoaiSan/DanhSach.cshtml.css` | CSS isolation để bảng cuộn riêng trên điện thoại. |
| `wwwroot/js/loai-san.js` | Chống gửi lặp form; khôi phục nút khi quay lại bằng lịch sử trình duyệt. |
| `Views/Shared/_Header.cshtml` | Menu Loại sân theo quyền Admin và active trang con. |
| `tests/QuanLyDatSan.M104.Tests/` tại gốc repo | Test HTTP tích hợp, tái sử dụng host cookie/Session của M1-03; schema loại sân/sân trong RAM. |
| Solution và tài liệu layout | Đăng ký project test, cập nhật menu và TempData. |

TempData thực tế: `Success` sau thêm/sửa/xóa thành công; `Error` khi chặn xóa do tham chiếu. Lỗi form dùng ModelState. Tài nguyên JS riêng chỉ được nạp bởi View M1-04.

**Không có migration mới:** M1-01/M2 đã có Entity, cột, unique index và FK cần dùng. Không sửa Entity, DbContext, snapshot hay migration. Các thay đổi local có sẵn ở `.csproj`, snapshot và hai file `20261006021847_CapNhatModel*` được giữ nguyên, không thuộc đầu việc này. Không tạo/reset/update database thật khi kiểm thử.

## 4. Chạy thử

Từ gốc repo, với .NET 10:

```powershell
dotnet build
dotnet test tests/QuanLyDatSan.M104.Tests --no-restore
dotnet run --no-build --project .\QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http
```

Nếu ứng dụng đang chạy giữ `.exe`, dừng phiên chạy trước khi build; hoặc dùng `--artifacts-path` tới thư mục `obj` riêng cho cả build và test như lượt kiểm tra dưới đây. Không sửa SDK hoặc migration để xử lý lỗi khóa file.

Mở `http://localhost:5024`, đăng nhập Admin đã có, chọn **Loại sân**. Database phải có migration nền M1/M2 đã được người phụ trách áp dụng; đầu việc này không yêu cầu migration mới. Không đưa tài khoản/mật khẩu hoặc connection string cá nhân vào repo.

Thử thêm tên mới, tên trùng sau Trim/khác hoa thường, sửa giữ nguyên tên, đổi trạng thái, xem chi tiết, tìm/lọc/sắp xếp và chuyển trang. Với loại có sân, xác nhận xóa phải bị chặn và giá sân không đổi sau sửa giá loại. Với loại chưa dùng, GET không xóa, POST xác nhận mới xóa. Đăng nhập Nhân viên/Khách hàng và truy cập URL/POST trực tiếp phải bị chặn.

## 5. Kết quả và giới hạn kiểm thử

- Build dùng đầu ra riêng `obj/m104-build`: **0 lỗi, 0 cảnh báo**, không dừng hoặc ghi đè ứng dụng local đang chạy.
- M1-04: **21/21 tests đạt**; M1-03: **22/22 tests đạt**, không bỏ qua. Kết quả TRX cục bộ ở `obj/m104-build/results/`, Git bỏ qua.
- Trước khi push, xuất riêng đúng nội dung Git index sang `obj/m104-pr-check`, không gồm bốn thay đổi local bị loại khỏi commit; chạy `dotnet build` đạt **0 lỗi, 0 cảnh báo**, rồi `dotnet test --no-build --no-restore` đạt lại **43/43 tests**. Kết quả TRX ở `obj/m104-pr-check/TestResults/`, Git bỏ qua.
- Test dùng cookie/Session/authorization/antiforgery và Razor thật với SQLite trong RAM; không dùng database hoặc tài khoản thật. Kiểm tra CRUD, sai quyền GET/POST, dữ liệu thiếu/sai, tên trùng, giữ giá/trạng thái sân, chặn tham chiếu mới sau GET xác nhận, mã không tồn tại và phân trang giữ bộ lọc.
- Đã chạy Edge headless qua Kestrel loopback với dữ liệu RAM: các trang danh sách/thêm/sửa/chi tiết/xóa được kiểm tra ở 1440, 1280, 1200, 1024, 390 và 320px; không tràn ngang toàn trang, bảng cuộn riêng, menu active và Escape hoạt động. Đã xem ảnh desktop/mobile.
- Đã thao tác qua trình duyệt: thêm loại sân → sửa sang Ngừng hoạt động → xác nhận xóa loại chưa tham chiếu → đăng xuất; Nhân viên/Khách hàng không thấy menu Loại sân. Lượt kiểm tra cuối không có lỗi JavaScript hoặc HTTP lỗi tài nguyên. Ảnh cục bộ ở `tests/QuanLyDatSan.M104.Tests/obj/ui-preview/screenshots/` (Git bỏ qua).
- Đã sửa lỗi giá có đuôi `.0` khiến jQuery validation `step=1` chặn form cập nhật: giá trên GET được định dạng số nguyên. Có kiểm thử hồi quy HTML và thao tác trình duyệt xác nhận lưu được.
- SQLite không chứng minh đầy đủ collation SQL Server hoặc tranh chấp unique/FK trên SQL Server. Các nhánh SQL Server 2601/2627/547 đã có xử lý nhưng chưa được kiểm thử tích hợp trên SQL Server; cần kiểm tra trên database kiểm thử được phép trước nghiệm thu thực tế.
- Cần một thành viên khác review và Tùng merge theo quy ước; không coi triển khai cục bộ là đã đóng issue hoặc đã merge.

Bàn giao trên nhánh `feature/m1-quan-ly-loai-san`, tạo từ `main` đã đồng bộ với `origin/main`. Commit chỉ gồm các file M1-04 và phần tích hợp menu, test, tài liệu liên quan; bốn thay đổi local có sẵn ở `.csproj`, snapshot và hai file `20261006021847_CapNhatModel*` được giữ ngoài commit. PR cần một thành viên khác review trước khi Tùng merge bằng **Create a merge commit**.

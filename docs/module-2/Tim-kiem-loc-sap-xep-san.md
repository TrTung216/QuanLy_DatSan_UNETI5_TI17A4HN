# M2 — Tìm kiếm, lọc và sắp xếp sân

Người thực hiện: Phan Giang Tâm — 23103100196.

Ngày kiểm tra: 13/10/2026. Nhánh làm việc: `feature/m2-tim-kiem-loc-san`.

## 1. Phần đã làm

- Tìm theo tên sân, tên loại sân hoặc địa chỉ; bỏ khoảng trắng ở đầu/cuối từ khóa, tìm không phân biệt chữ hoa/thường trên SQL Server.
- Lọc kết hợp loại sân, trạng thái, giá từ và giá đến. Hai đầu khoảng giá được tính vào kết quả.
- Sắp xếp tên A–Z/Z–A, giá tăng/giảm, giờ mở cửa tăng/giảm.
- Mỗi trang tối đa 10 sân; giữ điều kiện khi bấm Đầu, Trước, số trang, Sau hoặc Cuối. Gửi form tìm/lọc mới sẽ trở về trang 1.
- Đếm, lọc, sắp xếp và phân trang tại database. Dùng thêm MaSan để thứ tự ổn định khi giá/tên/giờ bằng nhau.
- Báo lỗi khi giá âm, có phần thập phân, giá đến nhỏ hơn giá từ hoặc điều kiện URL không hợp lệ; giữ form để sửa lại.
- Khách chỉ nhận sân và loại sân đang hoạt động. Admin/Nhân viên có thêm bộ lọc trạng thái và được xem sân bảo trì/ngừng hoạt động.

Giá dùng để lọc và sắp xếp là **giá giờ thường T2–T6** (`DonGia`). Bảng vẫn hiển thị cả giá cao điểm và cuối tuần. Ví dụ nhập `200000` là 200.000 đồng/giờ; không nhập `200.000` trong ô số.

Tra cứu sân trống theo ngày, giờ bắt đầu và giờ kết thúc còn cần kết nối hợp đồng kiểm tra lịch của M4. Kết quả hiện tại là danh sách sân theo thông tin quản lý, chưa xác nhận sân còn trống tại một khoảng giờ.

## 2. Các file và cách dữ liệu đi qua

```text
Form GET /SanTheThao
    → TraCuuSanViewModel: nhận và kiểm tra dữ liệu trình duyệt
    → SanTheThaoController.Index: chuyển điều kiện sang DTO
    → ISanTheThaoService / SanTheThaoService: truy vấn và kiểm tra quyền
    → KetQuaSan<TrangSanDoc>: dữ liệu danh sách hoặc mã lỗi
    → DanhSachSanViewModel → Views/SanTheThao/Index.cshtml
```

| File | Công việc |
| --- | --- |
| `ViewModels/TraCuuSanViewModel.cs` | Giữ từ khóa, loại sân, trạng thái, khoảng giá, cách sắp xếp, trang; kiểm tra dữ liệu nhập và đặt nhãn tiếng Việt. |
| `Services/QuanLySan/TraCuuSan.cs` | DTO điều kiện cho service và DTO lựa chọn loại sân. DTO không phụ thuộc MVC. |
| `Services/QuanLySan/ISanTheThaoService.cs` | Khai báo cách Controller gọi danh sách và lấy loại sân cho bộ lọc. |
| `Services/QuanLySan/SanTheThaoService.cs` | Kiểm tra điều kiện, giới hạn quyền, lọc, đếm, sắp xếp rồi Skip/Take. |
| `Controllers/SanTheThaoController.cs` | Action Index nhận query, nạp dropdown qua service, ánh xạ dữ liệu và mã lỗi cho View. |
| `ViewModels/DanhSachSanViewModel.cs` | Chứa điều kiện đã chọn, các sân của trang hiện tại, danh sách loại sân và số trang. |
| `Views/SanTheThao/Index.cshtml` | Hiển thị form tìm/lọc, bảng sân, lỗi và liên kết phân trang giữ điều kiện. |
| `tests/QuanLyDatSan.M201.Tests/TraCuuSanTests.cs` | Kiểm thử form, tìm/lọc, quyền, dữ liệu sai, URL và phân trang qua HTTP. |
| `tests/QuanLyDatSan.M201.Tests/TraCuuSanSqlServerTests.cs` | Kiểm thử giá decimal, chữ hoa tiếng Việt và phân trang trên SQL Server LocalDB riêng. |
| `tests/QuanLyDatSan.M201.Tests/PhanHoiSanTests.cs` | Cập nhật service giả theo chữ ký mới để giữ test xử lý lỗi. |

Chữ ký danh sách mới:

```csharp
Task<KetQuaSan<TrangSanDoc>> LayDanhSachAsync(
    TraCuuSan dieuKien, CancellationToken cancellationToken);

Task<IReadOnlyList<LoaiSanLoc>> LayLoaiSanLocAsync(
    CancellationToken cancellationToken);
```

`LayDanhSachAsync` trả `DuLieuKhongHopLe` khi điều kiện sai. Controller đổi mã đó thành lỗi trên form. Lỗi hệ thống vẫn đi qua xử lý lỗi chung. Chỉ Controller M2 và service giả trong test M2 đang gọi chữ ký danh sách này; đã cập nhật cùng đợt.

Nhánh này được tạo từ phần refactor M2 trong PR #43. PR #43 đã được merge vào main; phần tìm/lọc là công việc tiếp theo, gửi PR riêng cho Issue #14 (M2-02). Kiểm tra diff với main trước khi gửi để chỉ còn thay đổi của nhiệm vụ này.

## 3. Tự kiểm tra từng bước

### Bước 1 — Kiểm tra đang mở đúng nhánh

Mở Terminal tại VS Code hoặc Visual Studio, nhập:

```powershell
cd D:\ThucHanhLT.NET
git branch --show-current
git status
```

Dòng tên nhánh cần là `feature/m2-tim-kiem-loc-san`. Sau khi commit, các file tìm/lọc không còn xuất hiện trong Changes; kiểm tra lại bằng `git log -1 --oneline`. `.vscode/` là cấu hình IDE, không chọn nó khi commit phần này.

### Bước 2 — Build

Nếu ứng dụng đang chạy, dừng trước: Ctrl+C trong terminal hoặc Shift+F5 trong Visual Studio.

```powershell
dotnet build .\QuanLyDatSan_UNETI5_DHTI17A4HN.slnx
```

Chờ `Build succeeded`. Build chỉ kiểm tra biên dịch; không chứng minh mọi thao tác giao diện đều đúng.

### Bước 3 — Chạy web

```powershell
dotnet run --project .\QuanLyDatSan_UNETI5_DHTI17A4HN\QuanLyDatSan_UNETI5_DHTI17A4HN.csproj --launch-profile http
```

Chờ dòng `Now listening on: http://localhost:5024`, rồi mở:

`http://localhost:5024/SanTheThao`

Terminal sẽ tiếp tục giữ ứng dụng chạy, không nhập lệnh khác trong cửa sổ đó. Muốn chạy lệnh mới thì mở terminal thứ hai.

Nếu dùng Visual Studio, chọn đúng project khởi động và Ctrl+F5. Khi ứng dụng dùng cổng khác, lấy địa chỉ từ thông báo chạy ứng dụng và thêm `/SanTheThao`.

### Bước 4 — Kiểm tra tìm kiếm

1. Nhập một phần tên sân đã có, bấm **Tìm / Lọc**. Chỉ các sân khớp từ khóa xuất hiện.
2. Thử lại bằng một phần tên loại sân, ví dụ `Bóng đá`, rồi bằng địa chỉ, ví dụ `Hà Nội`.
3. Thử viết hoa `BÓNG ĐÁ`, hoặc thêm khoảng trắng trước/sau từ khóa. Kết quả vẫn phải khớp.
4. Nhập tên không tồn tại: phải thấy **Không có sân phù hợp**, không có lỗi hệ thống.
5. Bấm **Đặt lại** để bỏ điều kiện và xem danh sách mặc định.

Từ khóa tìm đồng thời ở ba trường bằng điều kiện OR: tên sân khớp, hoặc tên loại khớp, hoặc địa chỉ khớp. Các bộ lọc khác được kết hợp bằng AND: sân phải đáp ứng mọi điều kiện đã chọn.

### Bước 5 — Kiểm tra bộ lọc và sắp xếp

1. Chọn một loại sân, nhập giá từ `80000`, giá đến `200000`, bấm **Tìm / Lọc**. Các dòng phải đúng loại và có **giá giờ thường** nằm trong khoảng đó.
2. Chọn **Giá tăng dần**, bấm lại. Giá thường của dòng phía dưới không được thấp hơn dòng phía trên. Thử **Giá giảm dần** ngược lại.
3. Thử tên A–Z/Z–A và giờ mở cửa tăng/giảm.
4. Thử giá từ bằng giá đến của một sân: sân giá đúng mức đó vẫn được nhận.
5. Nhập giá từ `300000`, giá đến `100000`: form phải báo giá đến lớn hơn hoặc bằng giá từ. Sửa giá rồi tìm lại.
6. Giá âm hoặc có phần thập phân bị chặn; kiểm tra server trực tiếp bằng URL sau nếu trình duyệt chặn trước khi gửi:

```text
http://localhost:5024/SanTheThao?giaTu=-1
http://localhost:5024/SanTheThao?giaTu=100.5
http://localhost:5024/SanTheThao?sapXep=khong_co
```

Phải nhận thông báo trên trang, không phải trang lỗi 500.

### Bước 6 — Kiểm tra quyền

1. Đăng nhập Admin hoặc Nhân viên bằng tài khoản local đang có. Bộ lọc trạng thái xuất hiện; chọn Bảo trì để kiểm tra sân bảo trì. Nút Thêm/Sửa/Xóa vẫn có theo quyền.
2. Mở cửa sổ ẩn danh, không đăng nhập, vào `/SanTheThao`. Sân bảo trì/ngừng hoạt động và sân thuộc loại ngừng hoạt động phải bị ẩn. Không có bộ lọc trạng thái và các nút quản lý.
3. Trong cửa sổ ẩn danh, tự đổi URL thành `/SanTheThao?trangThai=2`. Không được lộ sân bảo trì.

Cửa sổ ẩn danh kiểm tra **khách chưa đăng nhập**. Kiểm tra tài khoản có vai trò KhachHang cần tài khoản do M1 cấp; không tự tạo tài khoản website chỉ để thử chức năng này. Bộ test tự động đã kiểm tra cả hai trường hợp.

### Bước 7 — Kiểm tra phân trang

Khi có hơn 10 sân phù hợp, bấm **Sau**, một số trang, **Trước**, **Đầu/Cuối**. Mỗi trang tối đa 10 sân và từ khóa/loại/giá/cách sắp xếp vẫn giữ. Khi đổi điều kiện rồi bấm **Tìm / Lọc**, danh sách trở lại trang 1.

Nếu database local chưa đủ 11 sân, sẽ chưa có nút chuyển trang. Đây là hành vi đúng; test tự động đã dùng dữ liệu riêng để kiểm tra nhiều trang. Không cần thêm 11 sân vào database làm bài chỉ để xuất hiện nút.

### Bước 8 — Chạy test khi cần

Từ thư mục `D:\ThucHanhLT.NET`:

```powershell
dotnet test .\tests\QuanLyDatSan.M201.Tests\QuanLyDatSan.M201.Tests.csproj
```

Bình thường các test SQLite chạy trong bộ nhớ; 7 ca SQL Server được đánh dấu bỏ qua khi chưa bật cờ. SQLite có giới hạn sắp xếp decimal và chuyển chữ hoa tiếng Việt, nên các phần đó có test SQL Server riêng, không đổi kiểu tiền thành double để chạy test.

Nếu máy Windows có SQL Server LocalDB, muốn chạy đầy đủ:

```powershell
$env:QLDS_M2_TEST_SQLSERVER = '1'
dotnet test .\QuanLyDatSan_UNETI5_DHTI17A4HN.slnx
Remove-Item Env:\QLDS_M2_TEST_SQLSERVER
```

SQL test tạo database `QLDS_M2_TraCuu_Test_<32 ký tự ngẫu nhiên>` và xóa đúng database đó sau khi chạy. Nó không đọc User Secrets, không dùng database làm bài và không tạo tài khoản trong website của bạn. Không cần chạy `database update` hoặc tạo migration cho thay đổi tìm/lọc này.

## 4. Kết quả kiểm tra ngày 13/10/2026

- Biên dịch thành công.
- M2: **115/115 ca đạt**, trong đó 7 ca dùng SQL Server LocalDB.
- Test M1 hiện có: **45/45 M103**, **24/24 M104** đạt.
- Tổng: **184/184 ca đạt**, không có ca bỏ qua trong lần chạy đã bật LocalDB.
- Kiểm tra tìm tiếng Việt viết hoa, tiền decimal lớn vẫn đúng thứ tự, lọc kết hợp, giá biên, điều kiện sai, phân quyền, liên kết phân trang và lỗi hệ thống.
- Kết quả nằm ở `%TEMP%\M2-TraCuu-20261013\test-full.log` và `%TEMP%\M2-TraCuu-20261013\results-full\`.

Test xác nhận xử lý HTTP và dữ liệu; bạn vẫn cần chạy giao diện với database local theo các bước trên để xem bố cục và dữ liệu thực tế của mình.

## 5. Công việc tiếp theo

Sau khi tự kiểm tra giao diện, commit riêng phần tìm/lọc/sort, không chọn `.vscode/`. Khi làm PR, dùng mẫu PR nhóm đã gửi, chọn base `main`, compare `feature/m2-tim-kiem-loc-san` và ghi `Closes #14` vì đã làm đủ phạm vi M2-02. PR #43 đã được merge.

Phần phối hợp M4 còn lại: nhập ngày và giờ bắt đầu/kết thúc, gọi chung kiểm tra lịch, loại sân không khả dụng và hiển thị kết quả. Cần chốt interface/DTO với M4 trên Issue #15 và #21 trước khi tích hợp; thao tác đặt đơn phải kiểm tra lại trong transaction của M3/M4.

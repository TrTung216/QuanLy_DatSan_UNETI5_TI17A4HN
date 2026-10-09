# Hướng dẫn sử dụng layout chung

Phụ trách: **Trần Trọng Tùng — M1, MSSV: 23103100202**. Nội dung: khung giao diện, menu, hợp đồng CSS/JS và hướng dẫn tích hợp; Codex hỗ trợ triển khai và rà soát. Các thành viên trao đổi với Tùng trước khi sửa layout, menu, partial thông báo hoặc CSS/JS chung và đề xuất thay đổi qua PR. Mỗi module tự quản lý View và tài nguyên riêng; không tạo lại header/menu/footer trong View.

Đã đối chiếu tài liệu **Quy_uoc_nhom_Quan_ly_dat_san.docx**, phiên bản 1.0 ngày 29/09/2026, do Tùng cung cấp ngày 05/10/2026. Phần quy ước đề xuất trong tài liệu không được coi là đã có xác nhận của cả nhóm; đầu việc này chỉ áp dụng các quy ước liên quan giao diện, không triển khai thêm nghiệp vụ. Việc commit/push/tạo PR được thực hiện theo yêu cầu riêng của Tùng; merge phải chờ review và approve của thành viên khác theo mục 8.

## 1. Cấu trúc

Các đường dẫn bên dưới tính từ thư mục project `QuanLyDatSan_UNETI5_DHTI17A4HN/`.

| File | Chức năng |
| --- | --- |
| `Views/Shared/_Layout.cshtml` | Khung tiếng Việt, header, `RenderBody()`, thông báo, footer, thư viện và các section tùy chọn. |
| `Views/Shared/_AdminLayout.cshtml` | Layout Nhân viên/Admin, dùng `_Layout` làm khung cha, đánh dấu khu vực quản trị và chuyển tiếp section `Styles`/`Scripts`; không lặp lại HTML/thư viện. |
| `Views/Shared/_Header.cshtml` | Thương hiệu, menu, mục hiện tại theo Controller/Action, tài khoản/vai trò và form đăng xuất. |
| `Views/Shared/_ThongBao.cshtml` | Đọc thông báo TempData, mã hóa nội dung bằng Razor và hiển thị nhãn tiếng Việt. |
| `wwwroot/css/layout.css` | Màu xanh dương, bố cục responsive, focus, thành phần `ql-*` và quy ước Bootstrap chung trong `.ql-app`. |
| `wwwroot/js/layout.js` | Escape đóng menu và trả focus; chống gửi lặp form đăng xuất chung, khôi phục nút khi quay lại bằng lịch sử trình duyệt. Bootstrap xử lý đóng/mở, `aria-expanded`. |
| `wwwroot/css/site.css` | Cỡ chữ nền tảng và margin body; bỏ quy tắc footer tuyệt đối của template cũ. |
| `Views/Shared/_Layout.cshtml.css` | Giữ file/pipeline CSS isolation, bỏ kiểu template cũ; kiểu khung được chuyển sang `layout.css` để áp dụng cả partial. |
| `Views/_ViewStart.cshtml` | Chọn `_AdminLayout` cho principal đã xác thực có vai trò Admin/NhanVien; khách chưa đăng nhập và KhachHang dùng `_Layout`. |
| `Views/_ViewImports.cshtml` | Đã có sẵn, import namespace và MVC Tag Helpers. Không thay đổi. |
| `wwwroot/js/site.js` | Giữ nguyên file chung hiện có, chưa có logic. |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Giữ nguyên jQuery Validation/Unobtrusive cho các form. |

Dùng Bootstrap **5.3.3**, jQuery và validation sẵn trong `wwwroot/lib`; không thêm framework/CDN. CSS module nạp sau CSS chung; JS module chạy sau jQuery, Bootstrap và JS chung. Các View vẫn có thể dùng CSS isolation qua bundle `.styles.css`.

Menu thu gọn dưới 1200px (điều chỉnh khi thêm menu M1-03 để tránh chen chúc ở tablet). Footer theo luồng flex, không đè nội dung. Có link bỏ qua menu, nhãn nút rõ ràng, `aria-current="page"` và focus bàn phím. Bảng rộng cần vùng cuộn riêng.

## 2. Tạo View, tiêu đề và tài nguyên riêng

Tạo View dưới `Views/<Controller>/<Action>.cshtml` cho action thực sự tồn tại. Không cần khai báo lại `Layout`, `<html>`, `<head>` hoặc các thư viện chung. View tự đặt một tiêu đề `h1`; layout chỉ sử dụng `ViewData["Title"]` cho tab trình duyệt.

`_AdminLayout` chuyển tiếp nội dung và section tới `_Layout`; các ví dụ dưới đây dùng được với cả hai. `ViewData["KhuVucQuanTri"]` là cờ trình bày nội bộ của layout, không phải quyền truy cập và không phải TempData. Việc chọn layout không bảo vệ URL; Controller vẫn phải kiểm tra quyền. Đầu mỗi file mã nguồn ghi họ tên, MSSV và nội dung thực hiện; file nhiều người ghi đúng phần việc từng người.

```cshtml
@* Họ tên: <thành viên>; MSSV: <mã sinh viên>; Nội dung: <phần việc thực tế>. *@
@{
    ViewData["Title"] = "Tên chức năng";
}
<div class="ql-page-header">
    <h1>@ViewData["Title"]</h1>
    <p class="ql-subtitle">Mô tả ngắn về công việc trên trang.</p>
</div>
<section class="ql-panel" aria-label="Nội dung chức năng">
    <!-- Nội dung của module -->
</section>

@section Styles {
    <link rel="stylesheet" href="~/css/ten-module.css" asp-append-version="true" />
}
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
    <script src="~/js/ten-module.js" asp-append-version="true"></script>
}
```

`Styles` và `Scripts` đều tùy chọn. Chỉ khai báo tài nguyên đã tạo; bỏ validation partial nếu trang không cần. Section phải nằm trong View chính, không đặt trong partial. CSS riêng nên có lớp gốc của module, tránh selector toàn cục như `body`, `table`, `.btn`. Không nạp lại Bootstrap/jQuery.

## 3. Thành phần giao diện

### Nút và form

Dùng `btn-primary` cho thao tác chính, `btn-outline-primary` cho thao tác phụ, `btn-outline-secondary` để quay lại, `btn-danger` cho thao tác xóa/hủy có tính phá hủy. Ghi rõ nhãn hành động; không chỉ dùng biểu tượng. Link để điều hướng, button submit để gửi form.

Ví dụ form đăng nhập dưới đây sử dụng ViewModel/action đang có; các module thay bằng model và action đã triển khai của mình:

```cshtml
@model QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DangNhapViewModel
<form asp-controller="TaiKhoan" asp-action="DangNhap" method="post" asp-antiforgery="true">
    <input asp-for="ReturnUrl" type="hidden" />
    <div asp-validation-summary="ModelOnly" class="text-danger" role="alert"></div>
    <div class="mb-3">
        <label asp-for="TenDangNhap" class="form-label"></label> <span aria-hidden="true">*</span>
        <input asp-for="TenDangNhap" class="form-control" autocomplete="username"
               required maxlength="50" aria-describedby="loi-ten" />
        <span asp-validation-for="TenDangNhap" class="text-danger" id="loi-ten"></span>
    </div>
    <div class="mb-3">
        <label asp-for="MatKhau" class="form-label"></label> <span aria-hidden="true">*</span>
        <input asp-for="MatKhau" class="form-control" autocomplete="current-password"
               required maxlength="128" aria-describedby="loi-mat-khau" />
        <span asp-validation-for="MatKhau" class="text-danger" id="loi-mat-khau"></span>
    </div>
    <div class="d-flex flex-wrap gap-2">
        <button type="submit" class="btn btn-primary">Đăng nhập</button>
        <a class="btn btn-outline-secondary" asp-controller="Home" asp-action="Index">Về trang chủ</a>
    </div>
</form>
```

Form có label liên kết input; placeholder không thay thế label. Dùng `form-select` cho select, `form-check` cho checkbox. Giữ validation server; không chỉ kiểm tra bằng JS. Với form sửa dữ liệu, luôn dùng phương thức và anti-forgery theo Controller.

Giải thích dấu * là trường bắt buộc trên form. Giữ dữ liệu nhập khi có lỗi, riêng mật khẩu không điền lại. Vô hiệu hóa nút khi form hợp lệ đang gửi; trang đăng nhập hiện có xử lý tại section Scripts, đăng xuất được xử lý trong `layout.js`. Ví dụ trên chỉ minh họa markup; module cần thêm xử lý submit/khôi phục nút phù hợp với form thường hoặc AJAX của mình. Không gắn trình xử lý chung lên mọi form. Hủy/xóa dữ liệu phải có bước xác nhận trước khi gửi; hiện chưa có action hủy/xóa để tích hợp. Nút nghiệp vụ phải xét cả quyền và trạng thái, không cho form chọn trạng thái tùy ý.

### Bảng và trạng thái rỗng

```html
<div class="table-responsive" role="region" aria-label="Danh sách kết quả" tabindex="0">
    <table class="table table-hover align-middle ql-table">
        <caption>Danh sách kết quả</caption>
        <thead><tr><th scope="col">Tên</th><th scope="col">Trạng thái</th></tr></thead>
        <tbody><!-- Render dữ liệu thật từ ViewModel tại đây --></tbody>
    </table>
</div>
```

Chỉ render bảng khi có dữ liệu. Khi rỗng dùng:

```html
<div class="ql-empty">
    <h2 class="h5">Chưa có dữ liệu</h2>
    <p class="mb-0">Chưa có kết quả phù hợp với điều kiện tìm kiếm.</p>
</div>
```

### Nhãn trạng thái

Mỗi nhãn luôn chứa chữ; không truyền đạt trạng thái chỉ bằng màu. Module tự ánh xạ enum nghiệp vụ của mình, không dùng các lớp này để thay thế enum hay kiểm tra nghiệp vụ.

| Trạng thái | Lớp | Màu |
| --- | --- | --- |
| Chờ xác nhận (`ChoXuLy`) | `ql-status ql-status--waiting` | Vàng |
| Đã xác nhận | `ql-status ql-status--confirmed` | Xanh dương |
| Hoàn thành | `ql-status ql-status--completed` | Xanh lá |
| Hủy | `ql-status ql-status--cancelled` | Xám |
| Từ chối | `ql-status ql-status--rejected` | Đỏ |

```html
<span class="ql-status ql-status--confirmed">Đã xác nhận</span>
```

Theo quy ước mục 4, `ChoXuLy` hiển thị “Chờ xác nhận”, `DangXuLy` hiển thị “Đã xác nhận”. Đây là hướng dẫn tích hợp; không tạo enum đặt sân mới trong đầu việc layout.

### Thông báo và TempData

M1-03 (`QuanLyTaiKhoanController`), M1-04 (`LoaiSanController`) và M5 (`DichVuController`) ghi `Success` sau khi lưu. M1-04 ghi `Error` khi chặn xóa loại sân đang được tham chiếu. Lỗi form dùng `ModelState`. Partial đọc bốn key dưới đây; `Warning`, `Info` là hợp đồng sẵn cho các module tích hợp:

| Key (phân biệt cách viết theo quy ước) | Bootstrap | Nhãn |
| --- | --- | --- |
| `Success` | `alert-success` | Thành công |
| `Error` | `alert-danger` | Lỗi |
| `Warning` | `alert-warning` | Lưu ý |
| `Info` | `alert-info` | Thông tin |

Gán chuỗi sau khi thao tác thực sự thành công/thất bại rồi redirect; ví dụ trong Controller của module: `TempData["Success"] = "Đã lưu thay đổi.";`. Layout đọc một lần ở request kế tiếp, không tự tạo thông báo. Không lưu HTML hay dữ liệu nhạy cảm; không dùng `Html.Raw`. View không render lại các key này để tránh thông báo trùng. Lỗi có `role="alert"`; thông báo khác có `role="status"`.

Thông báo tại chỗ không qua redirect:

```html
<div class="alert alert-info ql-notice" role="status">
    <strong>Thông tin:</strong> Chọn điều kiện tìm kiếm để xem kết quả.
</div>
```

### Phân trang

Dùng `pagination ql-pagination`, `aria-label` trên nav và `aria-current="page"` ở trang hiện tại. Trang bị vô hiệu hóa dùng span, không dùng link vẫn bấm được. Controller của module chịu trách nhiệm phân trang thật và giữ bộ lọc; layout không tự phân trang dữ liệu.

Mặc định **10 dòng/trang**, dùng `Skip/Take` trên server; giữ từ khóa tìm kiếm, bộ lọc và sort khi chuyển trang theo quy ước mục 7. Có thể tham khảo `LoaiSan/DanhSach` để kết hợp tìm kiếm, trạng thái, sắp xếp và phân trang.

```html
<nav aria-label="Phân trang kết quả">
    <ul class="pagination ql-pagination">
        <li class="page-item disabled"><span class="page-link">Trước</span></li>
        <li class="page-item active"><span class="page-link" aria-current="page">1</span></li>
        <li class="page-item disabled"><span class="page-link">Sau</span></li>
    </ul>
</nav>
```

Đây là ví dụ trạng thái một trang trong tài liệu, không phải dữ liệu đưa vào ứng dụng. Khi có trang khác, module sinh link bằng Tag Helpers tới action thật và tham số phân trang thật.

### Định dạng ngày, giờ và tiền

Ngày hiển thị `dd/MM/yyyy`, giờ `HH:mm`, tiền theo văn hóa `vi-VN`, ví dụ `200.000 ₫`. Trong View dùng `giaTriNgay.ToString("dd/MM/yyyy")`, `giaTriGio.ToString("HH:mm")` và `soTien.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))` kèm `₫` cho giá trị đồng nguyên. Đây chỉ là định dạng; công thức và làm tròn thuộc service nghiệp vụ. Input dùng `date`/`datetime-local` và giá trị chuẩn HTML, không nhét chuỗi `dd/MM/yyyy` vào thuộc tính value của input date.

Thời điểm nghiệp vụ phải theo giờ Việt Nam từ lớp đồng hồ chung khi nhóm triển khai; layout không tự dùng giờ máy chủ để sinh thời điểm hoặc chuyển đổi dữ liệu. Không sửa culture toàn ứng dụng trong đầu việc giao diện này.

## 4. Menu và quyền truy cập

Nguồn quyền: `TaiKhoanController` có `[Authorize]`, riêng `DangNhap` và `TuChoiTruyCap` có `[AllowAnonymous]`; `HomeController` công khai. `QuanLyTaiKhoanController` và `DichVuController` yêu cầu vai trò Admin ở cấp lớp. `Services/PhienDangNhap.cs` kiểm tra lại tài khoản, Session, phiên bản quyền, trạng thái và vai trò mỗi request có cookie. `ITaiKhoanHienTai` đọc principal đã xác thực. Enum hiện có: `Admin = 1`, `NhanVien = 2`, `KhachHang = 3`.

Layout dùng `User.Identity.IsAuthenticated` để chọn nhóm menu và inject `ITaiKhoanHienTai` để lấy họ tên/vai trò. Không đọc quyền từ form/URL, không tạo cơ chế đăng nhập mới. Mọi kiểm tra quyền phía server được giữ nguyên.

### Menu đang hoạt động

| Tên | Controller/Action | Chưa đăng nhập | Khách hàng | Nhân viên | Admin |
| --- | --- | --- | --- | --- | --- |
| Trang chủ | `Home/Index` | Có | Có | Có | Có |
| Quyền riêng tư | `Home/Privacy` | Có | Có | Có | Có |
| Đăng nhập | `TaiKhoan/DangNhap` (GET) | Có | Ẩn | Ẩn | Ẩn |
| Thông tin tài khoản | `TaiKhoan/ThongTin` (GET) | Ẩn | Có | Có | Có |
| Đăng xuất | `TaiKhoan/DangXuat` (**POST**) | Ẩn | Có | Có | Có |
| Quản lý tài khoản | `QuanLyTaiKhoan/DanhSach` (GET) | Ẩn | Ẩn | Ẩn | Có |
| Loại sân | `LoaiSan/DanhSach` (GET) | Ẩn | Ẩn | Ẩn | Có |
| Dịch vụ | `DichVu/DanhSach` (GET) | Ẩn | Ẩn | Ẩn | Có |

Admin có thêm Quản lý tài khoản (M1-03), Loại sân (M1-04) và Dịch vụ (M5), tương ứng Controller yêu cầu vai trò Admin. Nhân viên/Admin dùng `_AdminLayout` với nhãn “Khu vực quản trị”, còn khách dùng `_Layout`. Header hiển thị tên vai trò tiếng Việt. `TaiKhoan/TuChoiTruyCap` (HTTP 403) và `Home/Error` tồn tại nhưng không là mục điều hướng thường xuyên.

Đăng xuất luôn là form POST có `asp-antiforgery="true"`, tương ứng `[HttpPost, ValidateAntiForgeryToken]`. Không đổi thành link GET. Mục active so sánh cả Controller lẫn Action, không phân biệt hoa/thường; Quản lý tài khoản và Loại sân giữ active ở các trang con cùng Controller. Các trang lỗi không đánh dấu nhầm Trang chủ.

### Thêm mục menu

1. Trao đổi với Tùng; xác nhận action đã có, phương thức và quyền server thực tế. Ghi tên hiển thị tiếng Việt, Controller/Action, danh sách vai trò được phép.
2. Sửa danh sách `menu` ở `_Header.cshtml`. Mục công khai thêm vào danh sách ban đầu; mục cần đăng nhập thêm trong `if (daDangNhap)`; mục theo vai trò kiểm tra `User.IsInRole(nameof(VaiTro.Admin))` hoặc các vai trò đúng với `[Authorize(Roles = ...)]` của action. Nếu action dùng policy, đánh giá cùng policy qua authorization service, không đoán theo tên module.
3. Thêm tuple `("Tên hiển thị", "Controller đã tồn tại", "Action đã tồn tại")` trong điều kiện đúng. Vòng lặp tự sinh link, active và `aria-current`. Nếu muốn một mục active cho cả Index/Create/Edit, mở rộng điều kiện active một cách rõ ràng, vẫn kiểm tra Controller. Nếu thêm Areas, cần mở rộng cả thông tin area và điều kiện active; hiện project chưa dùng Areas.
4. Kiểm tra truy cập URL trực tiếp bằng vai trò không được phép và khách chưa đăng nhập. Ẩn menu chỉ là giao diện, không thay thế `[Authorize]` hay kiểm tra sở hữu dữ liệu.
5. Cập nhật bảng menu và kiểm tra desktop/mobile. Khi menu dài thêm, có thể điều chỉnh breakpoint với Tùng.

### Menu dự kiến, chưa tạo link

Theo ma trận quyền mục 6 và hợp đồng layout mục 7 của tài liệu quy ước. Chỉ bổ sung link sau khi có action và kiểm tra server tương ứng; tên route dưới đây không được tự giả định.

| Mục dự kiến | Quyền theo tài liệu | Controller/Action |
| --- | --- | --- |
| Xem/tìm sân hoạt động (M2) | Công khai; cả ba vai trò được dùng | Chưa có |
| Tạo đơn đặt sân (M3) | Chỉ Khách hàng; Nhân viên/Admin không được tạo theo ma trận hiện tại | Chưa có |
| Lịch của tôi (M3) | Khách hàng chỉ dữ liệu của mình; Nhân viên/Admin xem đơn theo quyền xử lý | Chưa có |
| Hồ sơ cá nhân (M3) | Cả ba vai trò, chỉ dữ liệu của mình; khác trang ThongTin chỉ đọc hiện có | Chưa có |
| Quản lý sân (M2), khách hàng (M3) | Nhân viên/Admin; quản lý khách hàng không cấp quyền sửa mật khẩu/vai trò | Chưa có |
| Quản lý đơn, xác nhận/từ chối (M4) | Nhân viên/Admin; hủy đơn chờ cho khách chỉ là thao tác trên đơn của mình | Chưa có |
| Dịch vụ trong đơn và tính tiền (M5 phối hợp M4) | Nhân viên/Admin | Chưa có |
| Dashboard và thống kê (M5) | Nhân viên/Admin | Chưa có |
| Đổi/khôi phục mật khẩu | Tài liệu M1 ghi chưa triển khai; quy ước chưa chốt luồng cụ thể | Chưa có |

Không gắn link giả, `href="#"` hoặc Controller tạm cho các mục dự kiến.

## 5. Tích hợp View hiện có

- `Home/Index`: thay lời chào template bằng nội dung tiếng Việt và nút đăng nhập/thông tin tài khoản đúng trạng thái, không có thống kê giả.
- `Home/Privacy`: thay placeholder bằng mô tả cookie/phiên đúng cơ chế hiện có và nhắc đăng xuất trên máy dùng chung.
- `TaiKhoan/DangNhap`: thêm khung/tiêu đề và liên kết mô tả lỗi bằng `aria-describedby`; giữ model, ReturnUrl, form POST, validation, section Scripts và xử lý submit cũ.
- `TaiKhoan/ThongTin`: thêm khung/tiêu đề, giữ nguyên dữ liệu và ánh xạ vai trò.
- `TaiKhoan/TuChoiTruyCap`: thêm khung và nhãn 403, giữ link về trang chủ và HTTP 403 của Controller.
- `Shared/Error`: Việt hóa, giữ mã yêu cầu; thay hướng dẫn bật Development trong template bằng hướng dẫn liên hệ quản trị.

Không sửa Controller, service xác thực, Entity, DbContext, migration, cấu hình kết nối hay logic M2–M5. `_ViewStart` được cập nhật chọn layout theo vai trò, `_ViewImports` giữ nguyên. Thay đổi package có sẵn trong `.csproj` của Tùng được giữ nguyên.

## 6. Chạy thử và kiểm tra

Tại thư mục gốc repository, với .NET 10 SDK và cấu hình/database hiện có:

```powershell
dotnet build
dotnet run --no-build --project .\QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http
```

Mở `http://localhost:5024`. Cấu hình kết nối bằng User Secrets hoặc biến môi trường theo tài liệu M1; không lưu thông tin cá nhân vào code. Các action tài khoản cần có cấu hình kết nối vì Controller nhận DbContext, kể cả GET đăng nhập. Dùng database và tài khoản hợp lệ đã có để kiểm tra đăng nhập; không chạy script `KiemTraDangNhap.ps1` cho đầu việc layout vì script đó tạo/xóa database.

Checklist bàn giao: kiểm tra ở 1440px, 1024px, 390px và 320px; mở/đóng menu và Escape; Tab/Shift+Tab và link bỏ qua menu; active đúng trang; đăng nhập, thông tin tài khoản và đăng xuất lần lượt với Khách hàng/Nhân viên/Admin; không có link đến action chưa tồn tại; kiểm tra HTTP 403, form validation, chuỗi tên dài, bảng cuộn ngang và footer khi nội dung dài. POST đăng xuất phải có anti-forgery và sau đăng xuất URL thông tin phải yêu cầu đăng nhập lại.

### Kết quả thực tế ngày 05/10/2026

- `dotnet build`: thành công, **0 lỗi, 0 cảnh báo**. Lần đầu NuGet bị chặn bởi mạng sandbox; đã build thành công khi được phép truy cập mạng, không thay package của người dùng.
- Chạy app bằng cấu hình Development hiện có trên `http://127.0.0.1:5129`, dùng Chrome headless qua Playwright tạm trong `obj/` (không thêm dependency project). Sandbox gặp lỗi quyền Windows Data Protection; chạy ngoài sandbox thành công, không sửa cơ chế xác thực.
- Đã kiểm tra Trang chủ, Quyền riêng tư, Đăng nhập, Từ chối truy cập và trang lỗi ở **1440, 1024, 390, 320px**: đúng mã HTTP (403 cho trang từ chối), tiêu đề tiếng Việt, một main/h1, active theo trang, không tràn ngang. Đã xem ảnh desktop và đăng nhập mobile.
- Đã kiểm tra nút menu, `aria-expanded`, Escape đóng và trả focus, link bỏ qua menu bằng bàn phím; khách không thấy form đăng xuất. Đã kiểm tra màu chữ nút link chính khi hover/focus.
- Liên kết công khai và tài nguyên CSS/JS trả 200; không có lỗi JavaScript. Khách vào `/TaiKhoan/ThongTin` được chuyển tới đăng nhập với ReturnUrl.
- Trang đăng nhập có label, input password, anti-forgery; client chặn trường bắt buộc rỗng; POST form rỗng với token hiển thị lỗi validation server (không truy vấn database); POST thiếu token trả 400.
- **Chưa kiểm tra bằng tài khoản thật**: đăng nhập thành công, nội dung ThongTin, menu/tên/vai trò của Khách hàng/Nhân viên/Admin, đăng xuất và phiên sau đăng xuất. Chưa có thông tin tài khoản kiểm thử được cung cấp; không tạo/reset database hay tạo tài khoản để thử. Nhánh đăng xuất POST và quyền hiện tại đã được rà soát ở mã nguồn, chưa coi là kiểm thử tích hợp.
- Bảng/phân trang và nhãn trạng thái đã có kiểu/ví dụ; chưa có Controller nghiệp vụ sử dụng nên chưa kiểm tra toàn luồng. Thông báo TempData và section CSS/JS đã được kiểm tra render riêng sau đợt đối chiếu quy ước bên dưới; `Scripts` của trang đăng nhập hoạt động trên app thật.
- Đã rà soát diff: chỉ thay layout, View trình bày, CSS/JS và tài liệu; `.csproj` vẫn là thay đổi có sẵn. Sau khi xác nhận nhánh đăng nhập đã merge qua PR #2 và được Tùng đồng ý, đã tạo nhánh `feature/m1-layout-menu` từ `origin/main` (`9ccd919`), giữ nguyên toàn bộ thay đổi tại thời điểm chuyển nhánh. PR layout không bao gồm thay đổi package có sẵn trong `.csproj`.

Ảnh và kết quả kiểm tra cục bộ nằm trong `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/layout-check/` (Git bỏ qua), không phải tài nguyên triển khai.

## 7. Rà soát theo quy ước nhóm ngày 05/10/2026

Nguồn đối chiếu: `Quy_uoc_nhom_Quan_ly_dat_san.docx` phiên bản 1.0, mục 1, 4, 6, 7, 8. Không sửa tài liệu gốc trong Downloads.

| Điểm rà soát | Kết quả xử lý |
| --- | --- |
| Đầu file ghi họ tên, MSSV, nội dung | Bổ sung vào các file layout, partial, CSS/JS, trang chủ/quyền riêng tư/lỗi và `_ViewStart`; các View tài khoản đã có. Ghi rõ Codex hỗ trợ phần triển khai. |
| Hai layout khách/quản trị | Thêm `_AdminLayout`, chọn trong `_ViewStart` theo vai trò đã xác thực; dùng layout cha để không trùng header/footer hoặc tài nguyên. |
| Ma trận quyền và menu dự kiến | Thay các dòng “chưa xác định quyền” bằng phân quyền có căn cứ trong tài liệu; vẫn không tạo link tới action chưa có. |
| Nhãn trạng thái | Hướng dẫn rõ “Chờ xác nhận” cho ChoXuLy và “Đã xác nhận” cho DangXuLy, giữ màu kèm chữ. |
| Ngày/giờ/tiền và danh sách | Bổ sung định dạng `dd/MM/yyyy`, `HH:mm`, tiền Việt; mặc định 10 dòng/trang, Skip/Take server và giữ tìm/lọc/sort. Chưa triển khai nghiệp vụ thay module khác. |
| Phản hồi khi gửi form | Đăng nhập giữ xử lý nút sẵn có; form đăng xuất khóa nút, chặn gửi lặp, khôi phục khi quay lại từ lịch sử. Hướng dẫn xác nhận hủy/xóa cho module sau. |
| Quy trình Git và schema | Nhánh `feature/m1-layout-menu` từ `origin/main`; commit/push/PR theo yêu cầu của Tùng. Không đổi schema hoặc package; cần thành viên khác approve trước khi Tùng merge bằng Create a merge commit. |

Kiểm tra bổ sung:

- Build project và Razor thành công, 0 lỗi/0 cảnh báo. Kiểm tra lại ứng dụng thật ở bốn độ rộng trên cho các trang công khai, validation, anti-forgery và chuyển hướng tài khoản: đạt.
- Bộ kiểm tra render độc lập trong `obj/layout-render-check/` dùng principal mô phỏng Guest/KhachHang/NhanVien/Admin, không đăng ký database hoặc thay cơ chế xác thực của app. Đã kiểm tra đúng layout, một header/main/footer, section Styles/Scripts chuyển tiếp đúng một lần (cả View không có section), thông báo được mã hóa và form đăng xuất có token.
- HTML render mô phỏng cho ba vai trò đã được xem bằng Chrome ở 1440/1024/390/320px: không tràn ngang, có menu tài khoản; JS chặn lần submit tiếp theo, khóa nút và khôi phục qua pageshow. Dữ liệu mô phỏng chỉ nằm trong `obj/`, không đưa vào giao diện thực tế của project.
- Các kiểm tra render/JS này **không thay thế kiểm thử đăng nhập/đăng xuất và phân quyền server bằng tài khoản thật**. Giới hạn kiểm thử tài khoản ở mục 6 vẫn còn; không coi token tạo trong bộ render là đã xác minh toàn luồng đăng xuất.

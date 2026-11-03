# Hướng dẫn sử dụng layout chung

Phụ trách: **Trần Trọng Tùng — M1, MSSV 23103100202**. Cập nhật tích hợp M1-05/M1-06 ngày 18/10/2026, trên main `ce5de11`. Codex hỗ trợ triển khai và kiểm thử. Tài liệu mô tả code hiện tại và hợp đồng giao diện theo quy ước nhóm v1.3; không thay thế quy tắc nghiệp vụ hay phân quyền server.

**Thành viên khác trao đổi với Tùng trước khi sửa layout/menu/partial thông báo/CSS/JS chung.** Giữ thông tin tác giả và ghi đúng phần đóng góp khi sửa file của nhiều người. Layout khách và thanh bên quản trị đã được M3 phát triển; menu đơn đặt sân được M4 tích hợp. Mỗi module quản lý View và tài nguyên riêng, không tạo lại header/menu/footer trong View.

## 1. Các file dùng chung

Đường dẫn trong bảng tính từ thư mục project `QuanLyDatSan_UNETI5_DHTI17A4HN/`.

| File | Chức năng |
| --- | --- |
| `Views/_ViewStart.cshtml` | Chọn `_AdminLayout` cho Admin/NhanVien đã đăng nhập; các trường hợp khác dùng `_LayoutKhachHang`. |
| `Views/_ViewImports.cshtml` | Namespace và MVC Tag Helpers dùng cho View. |
| `Views/Shared/_LayoutKhachHang.cshtml` | Khung khách: header, thông báo, `RenderBody()`, footer và section tài nguyên. |
| `Views/Shared/_HeaderKhachHang.cshtml` | Menu ngang, tên tài khoản, đăng nhập/đăng ký hoặc form đăng xuất. |
| `Services/MenuKhachHang.cs` | Danh sách mục công khai và mục riêng của khách hàng, nhóm action để đánh dấu active. |
| `Views/Shared/_AdminLayout.cshtml` | Khung quản trị độc lập: thanh bên, header tài khoản, nội dung, footer. |
| `Services/MenuQuanTri.cs` | Nguồn menu theo quyền cho thanh bên và trang điều hành; chứa `MucMenu`, `NhomMenu`. |
| `Views/Shared/_ThongBao.cshtml` | Thông báo TempData được Razor mã hóa, có nhãn và vai trò trợ năng. |
| `wwwroot/css/layout.css` | Màu, cỡ chữ, focus, nút/form/bảng, trạng thái, nền và khoảng cách chung trong `.ql-app`. |
| `wwwroot/css/thanh-phan.css` | Thành phần dùng chung như banner, thẻ, các bước hướng dẫn. |
| `wwwroot/css/quan-tri.css` | Thanh bên, header và bố cục riêng của khu vực quản trị. |
| `wwwroot/js/layout.js` | Escape đóng menu khách và trả focus; chống gửi lặp form đăng xuất, khôi phục khi quay lại lịch sử. |
| `wwwroot/css/site.css`, `wwwroot/js/site.js` | Tài nguyên nền tảng hiện có. |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | jQuery Validation/Unobtrusive cho form của module. |
| `Views/Shared/_Layout.cshtml`, `_Header.cshtml`, `_Layout.cshtml.css` | Khung cũ còn giữ để tương thích; không được `_ViewStart` chọn mặc định. Không thêm menu mới chỉ vào khung cũ. |

Dùng Bootstrap 5.3.3, jQuery và validation sẵn trong `wwwroot/lib`; không nạp lại thư viện trong View hoặc thêm CDN. Hai layout hiện tại độc lập, đều hỗ trợ `Styles` và `Scripts` tùy chọn. Menu khách thu gọn dưới **1200px**, khớp CSS/JS; thanh bên quản trị chuyển sang offcanvas dưới **992px**.

## 2. Tạo View, tiêu đề và CSS/JS riêng

Tạo `Views/<Controller>/<Action>.cshtml` cho action đã tồn tại. Để `_ViewStart` chọn layout; không cần khai báo lại `Layout`, `<html>` hoặc `<head>`. View tự đặt một `h1`; tiêu đề tab lấy từ `ViewData["Title"]`.

```cshtml
@* Họ tên: <thành viên>; MSSV: <mã>; Nội dung: <phần việc thực tế>. *@
@{
    ViewData["Title"] = "Tên chức năng";
}
<div class="ql-page-header">
    <h1>@ViewData["Title"]</h1>
    <p class="ql-subtitle">Mô tả ngắn.</p>
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

Chỉ khai báo file đã tạo, bỏ section/validation partial nếu không cần. Section nằm ở View chính, không đặt trong partial. CSS module nạp sau CSS chung và cần lớp gốc riêng, tránh selector toàn cục như `body`, `table`, `.btn`. Việc chọn layout và ẩn menu không bảo vệ URL: giữ `[Authorize]`, kiểm tra vai trò và quyền sở hữu ở server.

## 3. Nút, form, bảng, thông báo

Dùng `btn-primary` cho thao tác chính, `btn-outline-primary` cho thao tác phụ, `btn-outline-secondary` để quay lại, `btn-danger` cho thao tác phá hủy. Link dùng để điều hướng; button submit dùng để gửi form. Nút luôn có chữ mô tả, không chỉ biểu tượng.

Ví dụ form dùng ViewModel/action đăng nhập đã có:

```cshtml
@model QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DangNhapViewModel
<form asp-controller="TaiKhoan" asp-action="DangNhap" method="post" asp-antiforgery="true">
    <input asp-for="ReturnUrl" type="hidden" />
    <div asp-validation-summary="ModelOnly" class="text-danger" role="alert"></div>
    <div class="mb-3">
        <label asp-for="TenDangNhap" class="form-label"></label>
        <input asp-for="TenDangNhap" class="form-control" autocomplete="username" aria-describedby="loi-ten" />
        <span asp-validation-for="TenDangNhap" class="text-danger" id="loi-ten"></span>
    </div>
    <div class="mb-3">
        <label asp-for="MatKhau" class="form-label"></label>
        <input asp-for="MatKhau" class="form-control" autocomplete="current-password" aria-describedby="loi-mat-khau" />
        <span asp-validation-for="MatKhau" class="text-danger" id="loi-mat-khau"></span>
    </div>
    <button type="submit" class="btn btn-primary">Đăng nhập</button>
    <a class="btn btn-outline-secondary" asp-controller="Home" asp-action="Index">Về trang chủ</a>
</form>
```

Thêm `_ValidationScriptsPartial` trong section Scripts của View form. Dùng `form-select` và `form-check` cho select/checkbox. Label không được thay bằng placeholder. Giữ validation server và token chống giả mạo cho thao tác thay đổi dữ liệu; xác nhận trước xóa/hủy, có phản hồi lỗi và cho phép thử lại. JS chung chỉ quản lý submit đăng xuất, module tự quản lý form nghiệp vụ của mình.

```html
<div class="table-responsive">
    <table class="table table-hover ql-table">
        <caption>Danh sách kết quả</caption>
        <thead><tr><th scope="col">Tên</th><th scope="col">Trạng thái</th></tr></thead>
        <tbody><!-- Lặp dữ liệu thực từ ViewModel; Razor tự mã hóa nội dung. --></tbody>
    </table>
</div>
<!-- Khi không có kết quả, hiển thị thay bảng -->
<div class="ql-empty">Không có kết quả phù hợp. Hãy thử thay đổi bộ lọc.</div>
```

Phân trang dùng `pagination ql-pagination`, mỗi mục có `page-item`, link có `page-link`, trang hiện tại thêm `active` và `aria-current="page"`. Mục không khả dụng dùng span thay link. Link trang giữ tìm kiếm/lọc/sắp xếp; truy vấn phân trang ở server bằng Skip/Take, mặc định 10 dòng theo quy ước nhóm. Ngày `dd/MM/yyyy`, giờ `HH:mm`, tiền theo `vi-VN` kèm đơn vị; không thêm dữ liệu hoặc thống kê giả.

### TempData và trạng thái

Controller đặt `TempData["Success"] = "Đã lưu dữ liệu.";` trước redirect. Layout tự render `_ThongBao` một lần, View không render lại và không dùng `Html.Raw` cho nội dung thông báo.

| Key | Hiển thị | Sử dụng thực tế |
| --- | --- | --- |
| `Success` | Thành công, xanh lá, `role="status"` | Tài khoản, loại sân, sân, hồ sơ, đăng ký, đặt/xử lý đơn, dịch vụ. |
| `Error` | Lỗi, đỏ, `role="alert"` | Loại sân được tham chiếu; hồ sơ khách thiếu/bị khóa. |
| `Warning` | Lưu ý, vàng, `role="status"` | M2: sân đã bị xóa bởi thao tác khác. |
| `Info` | Thông tin, xanh dương, `role="status"` | Partial hỗ trợ; chưa có Controller ghi key này. |

Thông báo nội tuyến có thể dùng `<div class="alert alert-warning ql-notice" role="status"><strong>Lưu ý:</strong> Nội dung.</div>` khi không qua redirect.

| Trạng thái đơn | Chữ hiển thị | Lớp CSS ngoài `ql-status` |
| --- | --- | --- |
| ChoXuLy | Chờ xác nhận | `ql-status--waiting` (vàng) |
| DangXuLy | Đã xác nhận | `ql-status--confirmed` (xanh dương) |
| HoanThanh | Hoàn thành | `ql-status--completed` (xanh lá) |
| DaHuy | Đã hủy | `ql-status--cancelled` (xám) |
| TuChoi | Từ chối | `ql-status--rejected` (đỏ) |

Ví dụ: `<span class="ql-status ql-status--waiting">Chờ xác nhận</span>`. Luôn có chữ, không truyền trạng thái chỉ bằng màu; không đổi giá trị enum.

## 4. Menu đang hoạt động

Bảng liệt kê Controller/Action (GET), URL thực do Tag Helpers sinh. Quyền dựa trên code server hiện tại.

| Mục | Controller/Action | Người thấy trên menu |
| --- | --- | --- |
| Trang chủ công khai | `Home/Index` | Chưa đăng nhập |
| Trang chủ khách | `KhachHang/TrangChu` | Khách hàng |
| Danh sách sân | `SanTheThao/Index` | Tất cả; khách chỉ thấy dữ liệu server cho phép |
| Đăng nhập | `TaiKhoan/DangNhap` | Chưa đăng nhập, góc phải |
| Đăng ký | `KhachHang/DangKy` | Chưa đăng nhập, bên phải Đăng nhập |
| Đặt sân | `DonDat/Tao` | Khách hàng |
| Lịch của tôi | `DonDat/Index` | Khách hàng |
| Hồ sơ | `HoSo/Index` | Khách hàng |
| Trang điều hành | `QuanTri/Index` | Admin/Nhân viên |
| Thêm sân | `SanTheThao/Create` | Admin/Nhân viên |
| Đơn đặt sân | `QuanLyDonDat/DanhSach` | Admin/Nhân viên |
| Loại sân | `LoaiSan/DanhSach` | Admin |
| Dịch vụ | `DichVu/DanhSach` | Admin |
| Quản lý tài khoản | `QuanLyTaiKhoan/DanhSach` | Admin |
| Thông tin tài khoản | `TaiKhoan/ThongTin` | Khách hàng bấm tên trên header; Admin/Nhân viên có mục ở thanh bên |
| Quyền riêng tư | `Home/Privacy` | Tất cả, footer |

Đăng xuất là **POST `TaiKhoan/DangXuat` có anti-forgery**, hiển thị khi đã đăng nhập. Không tạo link GET. Trang `TaiKhoan/TuChoiTruyCap` trả 403 và `Home/Error` tồn tại nhưng không cần mục menu thường xuyên.

`MucMenu.KhopTrang` so sánh Controller và Action không phân biệt hoa/thường. `CungNhomAction` giữ active ở trang con; ví dụ Đơn đặt sân gồm `ChiTiet`, `XacNhan`, `TuChoi`, `Huy`. Ba action POST chỉ dùng để nhận diện khi server trả lại View lỗi, không biến thành link điều hướng. Mục Thêm sân tách riêng khỏi Danh sách sân.

### Cách bổ sung mục

1. Xác nhận với Tùng: tên hiển thị, Controller/Action đã tồn tại, phương thức GET và quyền/policy thực tế. Không đoán quyền từ tên module.
2. Menu khách thêm trong `MenuKhachHang.Lay`; thanh bên/trang điều hành thêm trong `MenuQuanTri.Lay`, đúng nhánh vai trò. Dùng `new MucMenu("Tên", "Controller", "Action", ["TrangCon"])`. Đây là cấu hình giao diện thuần, không truy vấn DB hoặc cần đăng ký DI.
3. Nếu route cần ID, đặt liên kết trên bản ghi ở View thay vì menu toàn cục. Action POST là form trong View. Nếu thêm Area/policy mới, mở rộng cấu hình và kiểm thử tương ứng, không hardcode đường dẫn đoán.
4. Cập nhật bảng này và `MenuVaiTroTests`; kiểm tra route tồn tại, quyền server, active, URL trực tiếp và desktop/mobile. Kiểm tra menu dài hoặc tên tài khoản dài.

### Chức năng dự kiến chưa có lối vào hoàn chỉnh

- Quản lý khách hàng dành cho Admin/Nhân viên: chưa có màn hình/route quản lý tương ứng. Hồ sơ hiện có chỉ dành cho khách hàng tự xem/sửa.
- Khách hàng hủy đơn; nhân viên hoàn thành đơn/chốt tiền; dịch vụ trong đơn và báo cáo thống kê: bổ sung sau khi module phụ trách triển khai đầy đủ. Các thao tác trên đơn đặt trong trang chi tiết, không tạo mục menu POST.
- Đổi/khôi phục mật khẩu: chưa có luồng tương ứng.

Trang điều hành và danh mục dịch vụ đã tồn tại; không đồng nghĩa phần báo cáo/tính tiền M5 đã hoàn tất. Không tạo `href="#"`, Controller giả hoặc link chết để lấp chỗ trống.

## 5. Chạy và kiểm tra

Từ gốc repo:

```powershell
dotnet build -c Release
dotnet test tests/QuanLyDatSan.M103.Tests/QuanLyDatSan.M103.Tests.csproj -c Release --filter FullyQualifiedName~MenuVaiTroTests
dotnet run -c Release --no-build --project .\QuanLyDatSan_UNETI5_DHTI17A4HN --launch-profile http
```

Mở `http://localhost:5024`; dùng cấu hình/database và tài khoản hợp lệ đã có. Không đưa connection string cá nhân vào repo, không reset database để kiểm tra layout. Nếu executable đang bị khóa, dừng đúng ứng dụng/debug session trước khi build.

Kiểm tra khách, khách hàng, nhân viên, admin ở 1440/1200/1024/390/320px: menu, tên tài khoản, mở/đóng/Escape, focus, liên kết, active ở trang con, form lỗi, thông tin tài khoản, 403 và đăng xuất. Mở URL quản trị bằng vai trò khác phải bị server chặn. Không coi ẩn menu là đã kiểm chứng phân quyền.

Bằng chứng và giới hạn kiểm thử đợt tích hợp này: [Phần 7 — Tích hợp menu](module-1/Phan-7-Tich-hop-menu.md). Các kết quả 05/10 của bản layout cũ nằm trong lịch sử Git; không dùng thay bằng chứng cho layout sau tích hợp.

// M3: Nguyễn Văn Quý; MSSV: 23103100181. nguồn menu duy nhất của khu vực quản trị, dùng cho thanh bên và trang điều hành.
using System.Security.Claims;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public record MucMenu(string Ten, string Controller, string Action, string[]? CungNhomAction = null, string MoTa = "")
{
    public bool KhopTrang(string? controller, string? action) =>
        string.Equals(Controller, controller, StringComparison.OrdinalIgnoreCase)
        && (string.Equals(Action, action, StringComparison.OrdinalIgnoreCase)
            || (CungNhomAction?.Any(x => string.Equals(x, action, StringComparison.OrdinalIgnoreCase)) ?? false));
}

public record NhomMenu(string Ten, IReadOnlyList<MucMenu> DanhSachMuc);

public static class MenuQuanTri
{
    // Thêm chức năng mới: thêm một dòng MucMenu vào đúng nhóm, chỉ khi action và [Authorize] của nó đã tồn tại.
    // Điều kiện hiển thị phải khớp với [Authorize(Roles = ...)] của action; ẩn menu không thay thế kiểm tra quyền ở server.
    public static IReadOnlyList<NhomMenu> Lay(ClaimsPrincipal nguoiDung)
    {
        var laAdmin = nguoiDung.IsInRole(nameof(VaiTro.Admin));

        var vanHanh = new List<MucMenu>();
        // Vận hành: đơn đặt sân (M4), sân thể thao (M2), khách hàng (M3) bổ sung tại đây khi có màn hình quản lý.

        var danhMuc = new List<MucMenu>();
        if (laAdmin)
        {
            danhMuc.Add(new MucMenu("Dịch vụ", "DichVu", "DanhSach", ["Tao", "Sua"],
                "Danh mục dịch vụ đi kèm đơn đặt sân."));
        }

        var taiKhoan = new List<MucMenu>();
        if (laAdmin)
        {
            taiKhoan.Add(new MucMenu("Quản lý tài khoản", "QuanLyTaiKhoan", "DanhSach", ["Tao", "Sua"],
                "Tạo, sửa và phân quyền tài khoản hệ thống."));
        }
        taiKhoan.Add(new MucMenu("Thông tin tài khoản", "TaiKhoan", "ThongTin", null,
            "Xem họ tên và vai trò của tài khoản đang đăng nhập."));

        var ketQua = new List<NhomMenu>
        {
            new("Tổng quan", [new MucMenu("Trang điều hành", "QuanTri", "Index", null,
                "Số liệu hiện tại và lối vào các chức năng.")])
        };
        if (vanHanh.Count > 0) ketQua.Add(new NhomMenu("Vận hành", vanHanh));
        if (danhMuc.Count > 0) ketQua.Add(new NhomMenu("Danh mục", danhMuc));
        ketQua.Add(new NhomMenu("Tài khoản", taiKhoan));
        return ketQua;
    }
}

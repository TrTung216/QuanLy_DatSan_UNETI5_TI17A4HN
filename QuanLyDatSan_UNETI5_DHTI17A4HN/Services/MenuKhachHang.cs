// M1: Trần Trọng Tùng; MSSV: 23103100202. Menu công khai/khách hàng, tích hợp các route đã có.
using System.Security.Claims;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public static class MenuKhachHang
{
    public static IReadOnlyList<MucMenu> Lay(ClaimsPrincipal nguoiDung)
    {
        var laKhachHang = nguoiDung.Identity?.IsAuthenticated == true
            && nguoiDung.IsInRole(nameof(VaiTro.KhachHang));
        var menu = new List<MucMenu>
        {
            new("Trang chủ", laKhachHang ? "KhachHang" : "Home", laKhachHang ? "TrangChu" : "Index"),
            new("Danh sách sân", "SanTheThao", "Index", ["Details"])
        };
        if (laKhachHang)
        {
            menu.Add(new("Đặt sân", "DonDat", "Tao"));
            menu.Add(new("Lịch của tôi", "DonDat", "Index", ["ChiTiet"]));
            menu.Add(new("Hồ sơ", "HoSo", "Index", ["Sua"]));
        }
        return menu;
    }
}

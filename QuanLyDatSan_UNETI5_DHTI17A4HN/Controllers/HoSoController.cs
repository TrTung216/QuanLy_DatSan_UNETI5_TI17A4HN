// M3: Nguyễn Văn Quý; MSSV: 23103100181. xem và sửa hồ sơ cá nhân của khách hàng đang đăng nhập.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HoSoController(ApplicationDbContext db, ITaiKhoanHienTai taiKhoanHienTai) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var khachHang = await LayHoSoAsync(cancellationToken);
        if (khachHang is null) return KhongCoHoSo();
        return View(TaoViewModel(khachHang));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(CancellationToken cancellationToken)
    {
        var khachHang = await LayHoSoAsync(cancellationToken);
        if (khachHang is null) return KhongCoHoSo();
        if (khachHang.TrangThai != TrangThaiKhachHang.HoatDong) return HoSoBiKhoa();
        return View(TaoViewModel(khachHang));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(HoSoViewModel model, CancellationToken cancellationToken)
    {
        var khachHang = await LayHoSoAsync(cancellationToken);
        if (khachHang is null) return KhongCoHoSo();
        if (khachHang.TrangThai != TrangThaiKhachHang.HoatDong) return HoSoBiKhoa();

        if (!ModelState.IsValid)
        {
            model.TenDangNhap = khachHang.TaiKhoan.TenDangNhap;
            model.NgayDangKy = khachHang.NgayDangKy;
            model.DiemTichLuy = khachHang.DiemTichLuy;
            return View(model);
        }

        var hoTen = model.HoTen.Trim();
        var email = model.Email.Trim();
        khachHang.HoTen = hoTen;
        khachHang.Email = email;
        khachHang.SoDienThoai = model.SoDienThoai.Trim();
        khachHang.NgaySinh = model.NgaySinh;
        khachHang.GioiTinh = model.GioiTinh;
        khachHang.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
        khachHang.TaiKhoan.HoTen = hoTen;
        khachHang.TaiKhoan.Email = email;
        await db.SaveChangesAsync(cancellationToken);

        TempData["ThanhCong"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }

    private Task<KhachHang?> LayHoSoAsync(CancellationToken cancellationToken)
    {
        var maTaiKhoan = taiKhoanHienTai.MaTaiKhoan ?? 0;
        return db.KhachHangs.Include(x => x.TaiKhoan)
            .SingleOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan, cancellationToken);
    }

    private static HoSoViewModel TaoViewModel(KhachHang khachHang) => new()
    {
        TenDangNhap = khachHang.TaiKhoan.TenDangNhap,
        HoTen = khachHang.HoTen,
        Email = khachHang.Email,
        SoDienThoai = khachHang.SoDienThoai,
        NgaySinh = khachHang.NgaySinh,
        GioiTinh = khachHang.GioiTinh ?? GioiTinh.KhongKhaiBao,
        DiaChi = khachHang.DiaChi,
        NgayDangKy = khachHang.NgayDangKy,
        DiemTichLuy = khachHang.DiemTichLuy
    };

    private RedirectToActionResult KhongCoHoSo()
    {
        TempData["ThatBai"] = "Không tìm thấy hồ sơ khách hàng của tài khoản này.";
        return RedirectToAction("ThongTin", "TaiKhoan");
    }

    private RedirectToActionResult HoSoBiKhoa()
    {
        TempData["ThatBai"] = "Hồ sơ đang bị khóa, không thể chỉnh sửa.";
        return RedirectToAction(nameof(Index));
    }
}

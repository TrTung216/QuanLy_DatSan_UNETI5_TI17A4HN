// M3: Nguyễn Văn Quý; MSSV: 23103100181. đăng ký tài khoản khách hàng kèm hồ sơ trong cùng giao dịch.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class KhachHangController(ApplicationDbContext db, IPasswordHasher<TaiKhoan> hasher, IDongHo dongHo) : Controller
{
    [HttpGet]
    public IActionResult DangKy()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "HoSo");
        return View(new DangKyKhachViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyKhachViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "HoSo");
        if (!ModelState.IsValid) return View(model);

        var tenDangNhap = model.TenDangNhap.Trim();
        if (await db.TaiKhoans.AnyAsync(x => x.TenDangNhap == tenDangNhap, cancellationToken))
        {
            return TenDangNhapDaDung(model);
        }

        var hoTen = model.HoTen.Trim();
        var email = model.Email.Trim();
        var taiKhoan = new TaiKhoan
        {
            TenDangNhap = tenDangNhap,
            HoTen = hoTen,
            Email = email,
            VaiTro = VaiTro.KhachHang,
            TrangThai = TrangThaiTaiKhoan.HoatDong
        };
        taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, model.MatKhau);
        taiKhoan.KhachHang = new KhachHang
        {
            HoTen = hoTen,
            Email = email,
            SoDienThoai = model.SoDienThoai.Trim(),
            NgaySinh = model.NgaySinh,
            GioiTinh = model.GioiTinh,
            DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim(),
            NgayDangKy = dongHo.BayGio,
            DiemTichLuy = 0,
            TrangThai = TrangThaiKhachHang.HoatDong
        };

        try
        {
            db.TaiKhoans.Add(taiKhoan);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return TenDangNhapDaDung(model);
        }

        TempData["ThanhCong"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction("DangNhap", "TaiKhoan");
    }

    private ViewResult TenDangNhapDaDung(DangKyKhachViewModel model)
    {
        ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
        return View(model);
    }
}

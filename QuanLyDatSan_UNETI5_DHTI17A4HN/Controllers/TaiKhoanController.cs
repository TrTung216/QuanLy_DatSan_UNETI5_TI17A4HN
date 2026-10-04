// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ đăng nhập/đăng xuất.
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TaiKhoanController(ApplicationDbContext db, IPasswordHasher<TaiKhoan> hasher,
    ITaiKhoanHienTai taiKhoanHienTai) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return VeTrangSauDangNhap(returnUrl);
        return View(new DangNhapViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    [EnableRateLimiting("DangNhap")]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var tenDangNhap = model.TenDangNhap.Trim();
        var taiKhoan = await db.TaiKhoans.SingleOrDefaultAsync(x => x.TenDangNhap == tenDangNhap, cancellationToken);
        var ketQua = PasswordVerificationResult.Failed;
        if (taiKhoan is not null && taiKhoan.TrangThai == TrangThaiTaiKhoan.HoatDong && Enum.IsDefined(taiKhoan.VaiTro))
        {
            try { ketQua = hasher.VerifyHashedPassword(taiKhoan, taiKhoan.MatKhau, model.MatKhau); }
            catch (FormatException) { /* Giá trị băm hỏng: từ chối đăng nhập. */ }
        }
        if (taiKhoan is null || ketQua == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
            return View(model);
        }
        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
        {
            taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, model.MatKhau);
            await db.SaveChangesAsync(cancellationToken);
        }
        HttpContext.Session.Clear();
        // Mỗi lần đăng nhập có mã mới, cookie cũ không thể dùng lại sau khi đăng nhập lần sau.
        var maPhien = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("MaPhien", maPhien);
        PhienDangNhap.LuuSession(HttpContext.Session, taiKhoan);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            PhienDangNhap.TaoPrincipal(taiKhoan, maPhien), new AuthenticationProperties { IsPersistent = false });
        return VeTrangSauDangNhap(model.ReturnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangXuat()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet]
    public IActionResult ThongTin() => View(taiKhoanHienTai);

    [AllowAnonymous, HttpGet]
    public IActionResult TuChoiTruyCap()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    private IActionResult VeTrangSauDangNhap(string? returnUrl) => Url.IsLocalUrl(returnUrl)
        ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(ThongTin));
}

// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ gọi service xác thực, giữ Session/cookie và đăng xuất.
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TaiKhoanController(IXacThucTaiKhoanService service,
    ITaiKhoanHienTai taiKhoanHienTai, PhienBanQuyenTaiKhoan phienBanQuyen) : Controller
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
        var ketQua = await service.XacThucAsync(new XacThucTaiKhoanYeuCau
        {
            TenDangNhap = model.TenDangNhap, MatKhau = model.MatKhau
        }, cancellationToken);
        if (ketQua.MaLoi != MaLoiXacThuc.ThanhCong || ketQua.TaiKhoan is null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.");
            return View(model);
        }
        var taiKhoan = ketQua.TaiKhoan;
        HttpContext.Session.Clear();
        // Mỗi lần đăng nhập có mã mới, cookie cũ không thể dùng lại sau khi đăng nhập lần sau.
        var maPhien = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("MaPhien", maPhien);
        PhienDangNhap.LuuSession(HttpContext.Session, taiKhoan);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            PhienDangNhap.TaoPrincipal(taiKhoan, maPhien, phienBanQuyen.Lay(taiKhoan.MaTaiKhoan)),
            new AuthenticationProperties { IsPersistent = false });
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

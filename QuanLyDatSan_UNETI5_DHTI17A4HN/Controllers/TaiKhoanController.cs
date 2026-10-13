// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ gọi service xác thực, giữ Session/cookie và đăng xuất.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ đăng nhập/đăng xuất.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. điều hướng sau đăng nhập theo vai trò (khách hàng, quản trị).
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TaiKhoanController(IXacThucTaiKhoanService service,
    ITaiKhoanHienTai taiKhoanHienTai, PhienBanQuyenTaiKhoan phienBanQuyen) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return VeTrangSauDangNhap(returnUrl, taiKhoanHienTai.VaiTro);
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
        return VeTrangSauDangNhap(model.ReturnUrl, taiKhoan.VaiTro);
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

    private IActionResult VeTrangSauDangNhap(string? returnUrl, VaiTro? vaiTro)
    {
        if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
        return vaiTro switch
        {
            VaiTro.KhachHang => RedirectToAction("TrangChu", "KhachHang"),
            VaiTro.Admin or VaiTro.NhanVien => RedirectToAction("Index", "QuanTri"),
            _ => RedirectToAction(nameof(ThongTin))
        };
    }
}

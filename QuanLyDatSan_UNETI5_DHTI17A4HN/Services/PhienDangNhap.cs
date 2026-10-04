// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ quản lý phiên đăng nhập.
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public class PhienDangNhap(ApplicationDbContext db) : CookieAuthenticationEvents
{
    public static ClaimsPrincipal TaoPrincipal(TaiKhoan taiKhoan, string maPhien) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, taiKhoan.MaTaiKhoan.ToString()),
        new Claim(ClaimTypes.Name, taiKhoan.HoTen),
        new Claim(ClaimTypes.Role, taiKhoan.VaiTro.ToString()),
        new Claim("MaPhien", maPhien)
    ], CookieAuthenticationDefaults.AuthenticationScheme));

    public static void LuuSession(ISession session, TaiKhoan taiKhoan)
    {
        session.SetInt32("MaTaiKhoan", taiKhoan.MaTaiKhoan);
        session.SetString("HoTen", taiKhoan.HoTen);
        session.SetString("VaiTro", taiKhoan.VaiTro.ToString());
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync(context.HttpContext.RequestAborted);
        var maHopLe = int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var ma);
        var maPhien = session.GetString("MaPhien");
        var taiKhoan = maHopLe && session.GetInt32("MaTaiKhoan") == ma
            && !string.IsNullOrEmpty(maPhien) && context.Principal?.FindFirstValue("MaPhien") == maPhien
            ? await db.TaiKhoans.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == ma, context.HttpContext.RequestAborted)
            : null;
        if (taiKhoan is null || taiKhoan.TrangThai != TrangThaiTaiKhoan.HoatDong
            || !Enum.IsDefined(taiKhoan.VaiTro)
            || context.Principal?.FindFirstValue(ClaimTypes.Role) != taiKhoan.VaiTro.ToString())
        {
            context.RejectPrincipal();
            session.Clear();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        LuuSession(session, taiKhoan);
        if (context.Principal?.Identity?.Name != taiKhoan.HoTen)
        {
            context.ReplacePrincipal(TaoPrincipal(taiKhoan, maPhien!));
            context.ShouldRenew = true;
        }
    }
}

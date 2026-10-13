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

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class KhachHangController(ApplicationDbContext db, IPasswordHasher<TaiKhoan> hasher, IDongHo dongHo,
    ITaiKhoanHienTai taiKhoanHienTai) : Controller
{
    [HttpGet]
    public async Task<IActionResult> TrangChu(CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        var khachHang = await db.KhachHangs.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTaiKhoan)
            .Select(x => new { x.HoTen, x.DiemTichLuy, x.TrangThai })
            .SingleOrDefaultAsync(cancellationToken);
        if (khachHang is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách hàng của tài khoản này.";
            return RedirectToAction("ThongTin", "TaiKhoan");
        }

        var bayGio = dongHo.BayGio;
        var donSapToi = db.DatSans.AsNoTracking()
            .Where(x => x.KhachHang.MaTaiKhoan == maTaiKhoan && x.GioKetThuc > bayGio
                && (x.TrangThai == TrangThaiDatSan.ChoXuLy || x.TrangThai == TrangThaiDatSan.DangXuLy));

        var model = new TrangChuKhachViewModel
        {
            HoTen = khachHang.HoTen,
            DiemTichLuy = khachHang.DiemTichLuy,
            HoSoBiKhoa = khachHang.TrangThai != TrangThaiKhachHang.HoatDong,
            SoDonChoXacNhan = await donSapToi.CountAsync(x => x.TrangThai == TrangThaiDatSan.ChoXuLy, cancellationToken),
            SoDonDaXacNhanSapToi = await donSapToi.CountAsync(x => x.TrangThai == TrangThaiDatSan.DangXuLy, cancellationToken),
            DonSapToi = await donSapToi
                .OrderBy(x => x.GioBatDau).ThenBy(x => x.MaDatSan).Take(3)
                .Select(x => new DonDatDongViewModel
                {
                    MaDatSan = x.MaDatSan,
                    TenSan = x.SanTheThao.TenSan,
                    TenLoai = x.SanTheThao.LoaiSan.TenLoai,
                    GioBatDau = x.GioBatDau,
                    GioKetThuc = x.GioKetThuc,
                    DonGia = x.DonGia,
                    TrangThai = x.TrangThai
                })
                .ToListAsync(cancellationToken)
        };
        return View(model);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult DangKy()
    {
        if (User.Identity?.IsAuthenticated == true) return VeTrangChuKhiDaDangNhap();
        return View(new DangKyKhachViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyKhachViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return VeTrangChuKhiDaDangNhap();
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

        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction("DangNhap", "TaiKhoan");
    }

    private IActionResult VeTrangChuKhiDaDangNhap() => User.IsInRole(nameof(VaiTro.KhachHang))
        ? RedirectToAction(nameof(TrangChu))
        : RedirectToAction("Index", "Home");

    private ViewResult TenDangNhapDaDung(DangKyKhachViewModel model)
    {
        ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
        return View(model);
    }
}

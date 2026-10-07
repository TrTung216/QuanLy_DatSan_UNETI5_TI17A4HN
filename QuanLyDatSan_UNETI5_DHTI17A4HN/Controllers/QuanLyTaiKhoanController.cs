// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ quản lý tài khoản và phân quyền.
using System.Data;
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

[Authorize(Roles = nameof(VaiTro.Admin))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanLyTaiKhoanController(ApplicationDbContext db, IPasswordHasher<TaiKhoan> hasher,
    ITaiKhoanHienTai hienTai, PhienBanQuyenTaiKhoan phienBanQuyen) : Controller
{
    private const int SoDongMoiTrang = 10;

    [HttpGet]
    public async Task<IActionResult> DanhSach(string? tuKhoa, VaiTro? vaiTro,
        TrangThaiTaiKhoan? trangThai, int trang = 1, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || (vaiTro.HasValue && !Enum.IsDefined(vaiTro.Value))
            || (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))) return BadRequest();
        tuKhoa = tuKhoa?.Trim();
        if (tuKhoa?.Length > 100) return BadRequest();
        var query = db.TaiKhoans.AsNoTracking();
        if (!string.IsNullOrEmpty(tuKhoa))
            query = query.Where(x => x.TenDangNhap.Contains(tuKhoa) || x.HoTen.Contains(tuKhoa) || x.Email.Contains(tuKhoa));
        if (vaiTro.HasValue) query = query.Where(x => x.VaiTro == vaiTro.Value);
        if (trangThai.HasValue) query = query.Where(x => x.TrangThai == trangThai.Value);
        var tongSo = await query.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);
        return View(new TaiKhoanDanhSachViewModel
        {
            DanhSach = await query.OrderBy(x => x.TenDangNhap).ThenBy(x => x.MaTaiKhoan)
                .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)
                .Select(x => new TaiKhoanDongViewModel
                {
                    MaTaiKhoan = x.MaTaiKhoan, TenDangNhap = x.TenDangNhap, HoTen = x.HoTen,
                    Email = x.Email, VaiTro = x.VaiTro, TrangThai = x.TrangThai
                }).ToListAsync(cancellationToken),
            TuKhoa = tuKhoa, VaiTro = vaiTro, TrangThai = trangThai,
            Trang = trang, TongTrang = tongTrang, TongSo = tongSo
        });
    }

    [HttpGet]
    public IActionResult Tao() => View(new TaiKhoanTaoViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaiKhoanTaoViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var ten = model.TenDangNhap.Trim();
        return await LuuAsync(model, async _ =>
        {
            // So sánh trực tiếp cột dùng Vietnamese_100_CI_AS, giống đăng nhập và unique index.
            if (await db.TaiKhoans.AnyAsync(x => x.TenDangNhap == ten, cancellationToken))
            {
                ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
                return View(model);
            }
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = ten, HoTen = model.HoTen, Email = model.Email,
                VaiTro = model.VaiTro!.Value, TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, model.MatKhau);
            db.TaiKhoans.Add(taiKhoan);
            return null;
        }, "Đã tạo tài khoản.", cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken cancellationToken)
    {
        var taiKhoan = await db.TaiKhoans.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == id, cancellationToken);
        if (taiKhoan is null) return NotFound();
        ViewData["MaTaiKhoan"] = id;
        ViewData["TenDangNhap"] = taiKhoan.TenDangNhap;
        ViewData["LaTaiKhoanHienTai"] = id == hienTai.MaTaiKhoan;
        return View(new TaiKhoanSuaViewModel
        {
            HoTen = taiKhoan.HoTen, Email = taiKhoan.Email, VaiTro = taiKhoan.VaiTro, TrangThai = taiKhoan.TrangThai
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(int id, TaiKhoanSuaViewModel model, CancellationToken cancellationToken)
    {
        var doiQuyenHoacTrangThai = false;
        // Nạp lại tên từ server cả khi validation thất bại, không nhận tên/mật khẩu từ form sửa.
        var ten = await db.TaiKhoans.AsNoTracking().Where(x => x.MaTaiKhoan == id)
            .Select(x => x.TenDangNhap).SingleOrDefaultAsync(cancellationToken);
        if (ten is null) return NotFound();
        ViewData["MaTaiKhoan"] = id;
        ViewData["TenDangNhap"] = ten;
        ViewData["LaTaiKhoanHienTai"] = id == hienTai.MaTaiKhoan;
        if (!ModelState.IsValid) return View(model);

        return await LuuAsync(model, async adminIds =>
        {
            var taiKhoan = await db.TaiKhoans.SingleOrDefaultAsync(x => x.MaTaiKhoan == id, cancellationToken);
            if (taiKhoan is null) return NotFound();
            var conLaAdminHoatDong = model.VaiTro == VaiTro.Admin && model.TrangThai == TrangThaiTaiKhoan.HoatDong;
            if (!conLaAdminHoatDong && adminIds.Contains(id) && adminIds.Count == 1)
                ModelState.AddModelError(string.Empty, "Không thể khóa hoặc hạ quyền Admin hoạt động cuối cùng.");
            if (id == hienTai.MaTaiKhoan && !conLaAdminHoatDong)
                ModelState.AddModelError(string.Empty, "Bạn không thể tự khóa hoặc hạ quyền tài khoản đang đăng nhập.");
            if (!ModelState.IsValid) return View(model);

            doiQuyenHoacTrangThai = taiKhoan.VaiTro != model.VaiTro || taiKhoan.TrangThai != model.TrangThai;
            taiKhoan.HoTen = model.HoTen;
            taiKhoan.Email = model.Email;
            taiKhoan.VaiTro = model.VaiTro!.Value;
            taiKhoan.TrangThai = model.TrangThai!.Value;
            return null;
        }, "Đã cập nhật tài khoản.", cancellationToken, () =>
        {
            if (doiQuyenHoacTrangThai) phienBanQuyen.VoHieuHoa(id);
        });
    }

    private async Task<IActionResult> LuuAsync(object model, Func<List<int>, Task<IActionResult?>> capNhat,
        string thongBao, CancellationToken cancellationToken, Action? sauKhiLuu = null)
    {
        try
        {
            // Giữ tập Admin hoạt động ổn định đến commit, chống hai Admin cùng hạ quyền/khóa nhau.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var adminIds = await db.TaiKhoans.Where(x => x.VaiTro == VaiTro.Admin && x.TrangThai == TrangThaiTaiKhoan.HoatDong)
                .Select(x => x.MaTaiKhoan).ToListAsync(cancellationToken);
            // Quyền có thể vừa bị đổi sau middleware: kiểm tra lại bên trong transaction ghi.
            if (!hienTai.MaTaiKhoan.HasValue || !adminIds.Contains(hienTai.MaTaiKhoan.Value)) return Forbid();
            var loi = await capNhat(adminIds);
            if (loi is not null) return loi;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            sauKhiLuu?.Invoke();
            TempData["Success"] = thongBao;
            return RedirectToAction(nameof(DanhSach));
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(TaiKhoanTaoViewModel.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
        }
        catch (Exception ex) when (ex is SqlException { Number: 1205 }
            || ex is DbUpdateException { InnerException: SqlException { Number: 1205 } })
        {
            ModelState.AddModelError(string.Empty, "Có cập nhật đồng thời. Vui lòng tải lại trang và thử lại.");
        }
        return View(model);
    }
}

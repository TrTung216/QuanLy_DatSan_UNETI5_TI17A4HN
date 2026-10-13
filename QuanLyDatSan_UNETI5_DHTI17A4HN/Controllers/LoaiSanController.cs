// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ CRUD loại sân, validation và bảo vệ dữ liệu tham chiếu.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LoaiSanController(ApplicationDbContext db) : Controller
{
    private const int SoDongMoiTrang = 10;

    [HttpGet]
    public async Task<IActionResult> DanhSach(string? tuKhoa, TrangThaiLoaiSan? trangThai,
        string sapXep = "ten_tang", int trang = 1, CancellationToken cancellationToken = default)
    {
        tuKhoa = tuKhoa?.Trim();
        if (!ModelState.IsValid || tuKhoa?.Length > 100
            || (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))
            || sapXep is not ("ten_tang" or "ten_giam")) return BadRequest();
        var query = db.LoaiSans.AsNoTracking();
        if (!string.IsNullOrEmpty(tuKhoa)) query = query.Where(x => x.TenLoai.Contains(tuKhoa));
        if (trangThai.HasValue) query = query.Where(x => x.TrangThai == trangThai.Value);
        var tongSo = await query.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);
        var sapXepQuery = sapXep == "ten_giam" ? query.OrderByDescending(x => x.TenLoai) : query.OrderBy(x => x.TenLoai);
        return View(new LoaiSanDanhSachViewModel
        {
            DanhSach = await Chieu(sapXepQuery.ThenBy(x => x.MaLoaiSan)
                .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)).ToListAsync(cancellationToken),
            TuKhoa = tuKhoa, TrangThai = trangThai, SapXep = sapXep,
            Trang = trang, TongTrang = tongTrang, TongSo = tongSo
        });
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        var model = await DocChiTietAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public IActionResult Tao() => View(new LoaiSanFormViewModel { TrangThai = TrangThaiLoaiSan.HoatDong });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(LoaiSanFormViewModel model, CancellationToken cancellationToken)
    {
        await KiemTraTenAsync(model, null, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        var loaiSan = new LoaiSan();
        GanThongTin(loaiSan, model);
        db.LoaiSans.Add(loaiSan);
        if (!await LuuAsync(cancellationToken)) return View(model);
        TempData["Success"] = "Đã thêm loại sân.";
        return RedirectToAction(nameof(ChiTiet), new { id = loaiSan.MaLoaiSan });
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken cancellationToken)
    {
        var loaiSan = await db.LoaiSans.AsNoTracking().SingleOrDefaultAsync(x => x.MaLoaiSan == id, cancellationToken);
        if (loaiSan is null) return NotFound();
        ViewData["MaLoaiSan"] = id;
        return View(new LoaiSanFormViewModel
        {
            TenLoai = loaiSan.TenLoai, MoTa = loaiSan.MoTa, SoNguoiToiDa = loaiSan.SoNguoiToiDa,
            DonGiaTheoGio = loaiSan.DonGiaTheoGio, TrangThai = loaiSan.TrangThai
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(int id, LoaiSanFormViewModel model, CancellationToken cancellationToken)
    {
        var loaiSan = await db.LoaiSans.SingleOrDefaultAsync(x => x.MaLoaiSan == id, cancellationToken);
        if (loaiSan is null) return NotFound();
        ViewData["MaLoaiSan"] = id;
        await KiemTraTenAsync(model, id, cancellationToken);
        if (!ModelState.IsValid) return View(model);
        // Chỉ sửa danh mục. Không cập nhật giá, trạng thái hoặc lịch sử của các sân liên quan.
        GanThongTin(loaiSan, model);
        if (!await LuuAsync(cancellationToken)) return View(model);
        TempData["Success"] = "Đã cập nhật loại sân.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Xoa(int id, CancellationToken cancellationToken)
    {
        var model = await DocChiTietAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ActionName(nameof(Xoa)), ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanXoa(int id, CancellationToken cancellationToken)
    {
        var loaiSan = await db.LoaiSans.SingleOrDefaultAsync(x => x.MaLoaiSan == id, cancellationToken);
        if (loaiSan is null) return NotFound();
        if (await db.SanTheThaos.AnyAsync(x => x.MaLoaiSan == id, cancellationToken)) return ChanXoa(id);
        db.LoaiSans.Remove(loaiSan);
        try
        {
            // FK NoAction của SQL Server vẫn bảo vệ nếu sân mới tham chiếu sau bước AnyAsync.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            return ChanXoa(id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        TempData["Success"] = "Đã xóa loại sân chưa được tham chiếu.";
        return RedirectToAction(nameof(DanhSach));
    }

    private IActionResult ChanXoa(int id)
    {
        TempData["Error"] = "Không thể xóa loại sân đang được tham chiếu. Bạn có thể chuyển sang Ngừng hoạt động để giữ dữ liệu liên quan.";
        return RedirectToAction(nameof(Xoa), new { id });
    }

    private async Task KiemTraTenAsync(LoaiSanFormViewModel model, int? id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return;
        var ten = model.TenLoai.Trim();
        // Dùng collation Vietnamese_100_CI_AS và unique index đã có; không tự đổi quy tắc so sánh tên.
        if (await db.LoaiSans.AnyAsync(x => x.TenLoai == ten && (!id.HasValue || x.MaLoaiSan != id.Value), cancellationToken))
            ModelState.AddModelError(nameof(model.TenLoai), "Tên loại sân đã được sử dụng.");
    }

    private async Task<bool> LuuAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError(nameof(LoaiSanFormViewModel.TenLoai), "Tên loại sân đã được sử dụng.");
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, "Loại sân đã bị xóa trong lúc chỉnh sửa. Vui lòng trở lại danh sách.");
        }
        return false;
    }

    private static void GanThongTin(LoaiSan loaiSan, LoaiSanFormViewModel model)
    {
        loaiSan.TenLoai = model.TenLoai.Trim();
        loaiSan.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
        loaiSan.SoNguoiToiDa = model.SoNguoiToiDa!.Value;
        loaiSan.DonGiaTheoGio = model.DonGiaTheoGio!.Value;
        loaiSan.TrangThai = model.TrangThai!.Value;
    }

    private Task<LoaiSanChiTietViewModel?> DocChiTietAsync(int id, CancellationToken cancellationToken) =>
        Chieu(db.LoaiSans.AsNoTracking().Where(x => x.MaLoaiSan == id)).SingleOrDefaultAsync(cancellationToken);

    private static IQueryable<LoaiSanChiTietViewModel> Chieu(IQueryable<LoaiSan> query) => query.Select(x => new LoaiSanChiTietViewModel
    {
        MaLoaiSan = x.MaLoaiSan, TenLoai = x.TenLoai, MoTa = x.MoTa, SoNguoiToiDa = x.SoNguoiToiDa,
        DonGiaTheoGio = x.DonGiaTheoGio, TrangThai = x.TrangThai, SoSan = x.DanhSachSan.Count
    });
}

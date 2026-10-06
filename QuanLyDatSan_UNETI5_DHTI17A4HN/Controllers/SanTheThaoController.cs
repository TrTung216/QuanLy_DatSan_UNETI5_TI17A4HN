// Họ và tên: Phan Giang Tâm
// Mã sinh viên: 23103100196
// Nội dung: Xem danh sách, chi tiết và thêm sân.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SanTheThaoController(ApplicationDbContext db) : Controller
{
    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ThemSanViewModel
        {
            GioMoCua = new TimeOnly(6, 0),
            GioDongCua = new TimeOnly(22, 0)
        };
        await NapLoaiSan(model, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThemSanViewModel model, CancellationToken cancellationToken)
    {
        // Kiểm tra lại loại sân trên server, kể cả khi người dùng sửa dữ liệu gửi lên.
        var loaiSanHopLe = model.MaLoaiSan.HasValue && await db.LoaiSans.AsNoTracking()
            .AnyAsync(x => x.MaLoaiSan == model.MaLoaiSan.Value
                && x.TrangThai == TrangThaiLoaiSan.HoatDong, cancellationToken);
        if (!loaiSanHopLe)
            ModelState.AddModelError(nameof(model.MaLoaiSan), "Vui lòng chọn loại sân đang hoạt động.");

        if (!ModelState.IsValid)
        {
            await NapLoaiSan(model, cancellationToken);
            return View(model);
        }

        var san = new SanTheThao
        {
            TenSan = model.TenSan.Trim(),
            MaLoaiSan = model.MaLoaiSan!.Value,
            DiaChi = model.DiaChi.Trim(),
            TienIch = model.TienIch?.Trim(),
            DonGia = model.DonGia!.Value,
            TrangThai = model.TrangThai!.Value,
            GioMoCua = model.GioMoCua!.Value,
            GioDongCua = model.GioDongCua!.Value,
            NgayBaoTri = model.NgayBaoTri,
            GhiChu = model.GhiChu?.Trim()
        };
        db.SanTheThaos.Add(san);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        {
            db.Entry(san).State = EntityState.Detached;
            ModelState.AddModelError(string.Empty, "Dữ liệu không còn hợp lệ. Vui lòng kiểm tra loại sân, giá và giờ hoạt động rồi thử lại.");
            await NapLoaiSan(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Đã thêm sân thành công.";
        return RedirectToAction(nameof(Details), new { id = san.MaSan });
    }

    private async Task NapLoaiSan(ThemSanViewModel model, CancellationToken cancellationToken)
    {
        model.CacLoaiSan = await db.LoaiSans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiLoaiSan.HoatDong)
            .OrderBy(x => x.TenLoai)
            .Select(x => new LuaChonLoaiSan(x.MaLoaiSan, x.TenLoai, x.DonGiaTheoGio))
            .ToListAsync(cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Index(int trang = 1, CancellationToken cancellationToken = default)
    {
        const int soDongMoiTrang = 10;
        var query = LaySanDuocXem();
        var tongSoSan = await query.CountAsync(cancellationToken);
        var tongSoTrang = Math.Max(1, (int)Math.Ceiling(tongSoSan / (double)soDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongSoTrang);

        var danhSach = await query.Include(x => x.LoaiSan)
            .OrderBy(x => x.TenSan).ThenBy(x => x.MaSan)
            .Skip((trang - 1) * soDongMoiTrang).Take(soDongMoiTrang)
            .ToListAsync(cancellationToken);

        return View(new DanhSachSanViewModel
        {
            DanhSachSan = danhSach,
            TrangHienTai = trang,
            TongSoTrang = tongSoTrang,
            TongSoSan = tongSoSan
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var san = await LaySanDuocXem().Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == id, cancellationToken);
        if (san is null) return NotFound();
        return View(san);
    }

    private IQueryable<SanTheThao> LaySanDuocXem()
    {
        var query = db.SanTheThaos.AsNoTracking();
        var laQuanLy = User.Identity?.IsAuthenticated == true
            && (User.IsInRole(nameof(VaiTro.Admin)) || User.IsInRole(nameof(VaiTro.NhanVien)));

        // Khách chỉ xem sân và loại sân đang hoạt động.
        if (!laQuanLy)
            query = query.Where(x => x.TrangThai == TrangThaiSan.HoatDong
                && x.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong);
        return query;
    }
}

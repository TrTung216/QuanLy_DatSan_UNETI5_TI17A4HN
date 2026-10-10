// Họ và tên: Phan Giang Tâm
// Mã sinh viên: 23103100196
// Nội dung: Xem danh sách, chi tiết, thêm, sửa và xóa sân.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

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
            DonGiaCaoDiem = model.DonGiaCaoDiem!.Value,
            DonGiaCuoiTuan = model.DonGiaCuoiTuan!.Value,
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

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var san = await db.SanTheThaos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.MaSan == id, cancellationToken);
        if (san is null) return NotFound();

        var model = new SuaSanViewModel
        {
            MaSan = san.MaSan, TenSan = san.TenSan, MaLoaiSan = san.MaLoaiSan,
            DiaChi = san.DiaChi, TienIch = san.TienIch, DonGia = san.DonGia,
            DonGiaCaoDiem = san.DonGiaCaoDiem ?? san.DonGia,
            DonGiaCuoiTuan = san.DonGiaCuoiTuan ?? san.DonGia,
            TrangThai = san.TrangThai, GioMoCua = san.GioMoCua, GioDongCua = san.GioDongCua,
            NgayBaoTri = san.NgayBaoTri, GhiChu = san.GhiChu
        };
        await NapLoaiSan(model, cancellationToken, san.MaLoaiSan);
        return View(model);
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SuaSanViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.MaSan) return BadRequest();
        var san = await db.SanTheThaos.SingleOrDefaultAsync(x => x.MaSan == id, cancellationToken);
        if (san is null) return NotFound();
        var maLoaiSanCu = san.MaLoaiSan;

        // Giữ được loại sân hiện tại; khi đổi loại chỉ chọn loại đang hoạt động.
        var loaiSanHopLe = model.MaLoaiSan.HasValue && await db.LoaiSans.AsNoTracking()
            .AnyAsync(x => x.MaLoaiSan == model.MaLoaiSan.Value
                && (x.TrangThai == TrangThaiLoaiSan.HoatDong || x.MaLoaiSan == maLoaiSanCu), cancellationToken);
        if (!loaiSanHopLe)
            ModelState.AddModelError(nameof(model.MaLoaiSan), "Loại sân không hợp lệ hoặc đã ngừng hoạt động.");

        if (!ModelState.IsValid)
        {
            await NapLoaiSan(model, cancellationToken, maLoaiSanCu);
            return View(model);
        }

        // Chỉ gán các trường cho phép sửa, không cập nhật bản ghi từ toàn bộ dữ liệu form.
        san.TenSan = model.TenSan.Trim();
        san.MaLoaiSan = model.MaLoaiSan!.Value;
        san.DiaChi = model.DiaChi.Trim();
        san.TienIch = model.TienIch?.Trim();
        san.DonGia = model.DonGia!.Value;
        san.DonGiaCaoDiem = model.DonGiaCaoDiem!.Value;
        san.DonGiaCuoiTuan = model.DonGiaCuoiTuan!.Value;
        san.TrangThai = model.TrangThai!.Value;
        san.GioMoCua = model.GioMoCua!.Value;
        san.GioDongCua = model.GioDongCua!.Value;
        san.NgayBaoTri = model.NgayBaoTri;
        san.GhiChu = model.GhiChu?.Trim();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        {
            db.Entry(san).State = EntityState.Detached;
            ModelState.AddModelError(string.Empty, "Không lưu được vì dữ liệu liên quan đã thay đổi. Vui lòng kiểm tra lại thông tin sân.");
            await NapLoaiSan(model, cancellationToken, maLoaiSanCu);
            return View(model);
        }

        TempData["Success"] = "Đã cập nhật sân thành công.";
        return RedirectToAction(nameof(Details), new { id = san.MaSan });
    }

    private async Task NapLoaiSan(ThemSanViewModel model, CancellationToken cancellationToken, int? maLoaiSanHienTai = null)
    {
        var cacLoai = await db.LoaiSans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiLoaiSan.HoatDong || x.MaLoaiSan == maLoaiSanHienTai)
            .OrderBy(x => x.TenLoai)
            .Select(x => new LuaChonLoaiSan(x.MaLoaiSan,
                x.TrangThai == TrangThaiLoaiSan.HoatDong ? x.TenLoai : x.TenLoai + " (ngừng hoạt động)", x.DonGiaTheoGio))
            .ToListAsync(cancellationToken);
        model.CacLoaiSan = cacLoai.Select(x =>
        {
            var gia = GiaThueSan.GiaGoiY(x.TenLoai, x.DonGiaTheoGio);
            return x with { DonGiaTheoGio = gia.Thuong, DonGiaCaoDiem = gia.CaoDiem, DonGiaCuoiTuan = gia.CuoiTuan };
        }).ToList();
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
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken,
        DateOnly? ngay = null, TimeOnly? batDau = null, TimeOnly? ketThuc = null)
    {
        var san = await LaySanDuocXem().Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == id, cancellationToken);
        if (san is null) return NotFound();
        ViewData["NgayTinhGia"] = ngay?.ToString("yyyy-MM-dd");
        ViewData["GioBatDau"] = batDau?.ToString("HH:mm");
        ViewData["GioKetThuc"] = ketThuc?.ToString("HH:mm");
        if (ngay.HasValue || batDau.HasValue || ketThuc.HasValue || !ModelState.IsValid)
        {
            if (!ngay.HasValue || !batDau.HasValue || !ketThuc.HasValue)
                ModelState.AddModelError(string.Empty, "Nhập đủ ngày thuê, giờ bắt đầu và giờ kết thúc để tính giá.");
            if (ModelState.IsValid)
            {
                try { ViewData["GiaDuKien"] = GiaThueSan.TinhTien(san, ngay!.Value, batDau!.Value, ketThuc!.Value); }
                catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); }
            }
        }
        return View(san);
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var san = await db.SanTheThaos.AsNoTracking().Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == id, cancellationToken);
        if (san is null) return NotFound();
        return View(san);
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        // Tải lại sân theo mã, không nhận toàn bộ thông tin sân từ form.
        var san = await db.SanTheThaos.Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == id, cancellationToken);
        if (san is null) return NotFound();

        db.SanTheThaos.Remove(san);
        try
        {
            // Quan hệ đặt sân phải chặn xóa bản ghi cha, không cascade lịch sử.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        {
            db.Entry(san).State = EntityState.Unchanged;
            ModelState.AddModelError(string.Empty, "Không thể xóa sân vì đã có dữ liệu liên quan. Hãy giữ sân và chuyển sang ngừng hoạt động.");
            return View("Delete", san);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(san).State = EntityState.Detached;
            TempData["Warning"] = "Sân đã được xóa bởi một thao tác khác. Danh sách đã được cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Đã xóa sân thành công.";
        return RedirectToAction(nameof(Index));
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

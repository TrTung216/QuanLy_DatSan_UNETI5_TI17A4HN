// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: quản lý danh mục dịch vụ (chỉ Admin).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin))]
public class DichVuController(ApplicationDbContext db) : Controller
{
    private const int SoDongMoiTrang = 10;

    [HttpGet]
    public async Task<IActionResult> DanhSach(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep, int trang = 1)
    {
        var truyVan = db.DichVus.AsNoTracking().AsQueryable();

        tuKhoa = tuKhoa?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
            truyVan = truyVan.Where(x => x.TenDichVu.Contains(tuKhoa));
        if (trangThai.HasValue)
            truyVan = truyVan.Where(x => x.TrangThai == trangThai.Value);

        IOrderedQueryable<DichVu> sapXepTruyVan = sapXep switch
        {
            "ten_giam" => truyVan.OrderByDescending(x => x.TenDichVu),
            "gia_tang" => truyVan.OrderBy(x => x.DonGia),
            "gia_giam" => truyVan.OrderByDescending(x => x.DonGia),
            _ => truyVan.OrderBy(x => x.TenDichVu)
        };

        var tongSoDong = await sapXepTruyVan.CountAsync();
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSoDong / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);

        return View(new DichVuDanhSachViewModel
        {
            DanhSach = await sapXepTruyVan.ThenBy(x => x.MaDichVu)
                .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang).ToListAsync(),
            TuKhoa = tuKhoa,
            TrangThai = trangThai,
            SapXep = sapXep,
            Trang = trang,
            TongTrang = tongTrang
        });
    }

    [HttpGet]
    public IActionResult Tao() => View(new DichVuTaoViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(DichVuTaoViewModel model)
    {
        KiemTraDonGia(model);
        if (!ModelState.IsValid) return View(model);

        db.DichVus.Add(new DichVu
        {
            TenDichVu = model.TenDichVu,
            DonViTinh = model.DonViTinh,
            DonGia = model.DonGia,
            MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa,
            TrangThai = TrangThaiDichVu.HoatDong // do server quyết định, không nhận từ form
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Đã thêm dịch vụ.";
        return RedirectToAction(nameof(DanhSach));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id)
    {
        var dichVu = await db.DichVus.AsNoTracking().SingleOrDefaultAsync(x => x.MaDichVu == id);
        if (dichVu is null) return NotFound();

        return View(new DichVuSuaViewModel
        {
            MaDichVu = dichVu.MaDichVu,
            TenDichVu = dichVu.TenDichVu,
            DonViTinh = dichVu.DonViTinh,
            DonGia = dichVu.DonGia,
            MoTa = dichVu.MoTa
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(DichVuSuaViewModel model)
    {
        KiemTraDonGia(model);
        if (!ModelState.IsValid) return View(model);

        var dichVu = await db.DichVus.SingleOrDefaultAsync(x => x.MaDichVu == model.MaDichVu);
        if (dichVu is null) return NotFound();

        // Chỉ ảnh hưởng đơn mới: ChiTietDatSan sẽ lưu bản chụp DonGia lúc thêm.
        dichVu.TenDichVu = model.TenDichVu;
        dichVu.DonViTinh = model.DonViTinh;
        dichVu.DonGia = model.DonGia;
        dichVu.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa;
        await db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật dịch vụ.";
        return RedirectToAction(nameof(DanhSach));
    }

    // Không xóa cứng: chỉ chuyển HoatDong <-> NgungHoatDong.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var dichVu = await db.DichVus.SingleOrDefaultAsync(x => x.MaDichVu == id);
        if (dichVu is null) return NotFound();

        dichVu.TrangThai = dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? TrangThaiDichVu.NgungHoatDong
            : TrangThaiDichVu.HoatDong;
        await db.SaveChangesAsync();

        TempData["Success"] = dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? "Đã kích hoạt dịch vụ." : "Đã ngừng hoạt động dịch vụ.";
        return RedirectToAction(nameof(DanhSach));
    }

    // Quy ước: giá nhập theo đồng nguyên.
    private void KiemTraDonGia(DichVuTaoViewModel model)
    {
        if (model.DonGia != decimal.Truncate(model.DonGia))
            ModelState.AddModelError(nameof(model.DonGia), "Đơn giá phải là số đồng nguyên.");
    }
}
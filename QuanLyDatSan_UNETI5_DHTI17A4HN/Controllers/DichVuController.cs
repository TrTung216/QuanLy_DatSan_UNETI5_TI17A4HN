// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: quản lý danh mục dịch vụ (chỉ Admin).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin))]
public class DichVuController(IDichVuService service) : Controller
{

    [HttpGet]
    public async Task<IActionResult> DanhSach(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep, int trang = 1)
    {
        var ketQua = await service.LayDanhSachAsync(tuKhoa, trangThai, sapXep, trang, HttpContext.RequestAborted);

        return View(new DichVuDanhSachViewModel
        {
            DanhSach = ketQua.DanhSach,
            TuKhoa = ketQua.TuKhoa,
            TrangThai = trangThai,
            SapXep = sapXep,
            Trang = ketQua.Trang,
            TongTrang = ketQua.TongTrang
        });
    }

    [HttpGet]
    public IActionResult Tao() => View(new DichVuTaoViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(DichVuTaoViewModel model)
    {
        KiemTraDonGia(model);
        if (!ModelState.IsValid) return View(model);

        var ketQua = await service.TaoAsync(model.TenDichVu, model.DonViTinh, model.DonGia, model.MoTa, HttpContext.RequestAborted);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(ketQua.TruongLoi ?? string.Empty, ketQua.Loi!);
            return View(model);
        }
        TempData["Success"] = ketQua.ThongBao;
        return RedirectToAction(nameof(DanhSach));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id)
    {
        var dichVu = await service.LayChiTietAsync(id, HttpContext.RequestAborted);
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

        var ketQua = await service.SuaAsync(model.MaDichVu, model.TenDichVu, model.DonViTinh, model.DonGia, model.MoTa, HttpContext.RequestAborted);
        if (ketQua.KhongTimThay) return NotFound();
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(ketQua.TruongLoi ?? string.Empty, ketQua.Loi!);
            return View(model);
        }
        TempData["Success"] = ketQua.ThongBao;
        return RedirectToAction(nameof(DanhSach));
    }

    // Không xóa cứng: chỉ chuyển HoatDong <-> NgungHoatDong.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiTrangThai(int id)
    {
        var ketQua = await service.DoiTrangThaiAsync(id, HttpContext.RequestAborted);
        if (ketQua.KhongTimThay) return NotFound();
        TempData[ketQua.ThanhCong ? "Success" : "Error"] = ketQua.ThongBao ?? ketQua.Loi;
        return RedirectToAction(nameof(DanhSach));
    }

    // Quy ước: giá nhập theo đồng nguyên.
    private void KiemTraDonGia(DichVuTaoViewModel model)
    {
        if (model.DonGia != decimal.Truncate(model.DonGia))
            ModelState.AddModelError(nameof(model.DonGia), "Đơn giá phải là số đồng nguyên.");
    }
}
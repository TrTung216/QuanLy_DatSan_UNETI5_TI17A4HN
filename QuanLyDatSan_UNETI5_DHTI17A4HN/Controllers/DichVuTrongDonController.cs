// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: nhân viên/Admin thêm, sửa, hủy dịch vụ trong đơn. Controller mỏng, gọi IDichVuTrongDonService.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
public class DichVuTrongDonController(IDichVuTrongDonService dichVuTrongDonService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int maDatSan, CancellationToken cancellationToken)
    {
        try
        {
            var chiTiet = await dichVuTrongDonService.LayTheoDonAsync(maDatSan, cancellationToken);
            if (chiTiet is null) return NotFound();
            return View(chiTiet);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Them(ThemDichVuTrongDonViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BaoLoiForm(model.MaDatSan);
        try
        {
            var ketQua = await dichVuTrongDonService.ThemAsync(model.MaDatSan, model.MaDichVu, model.SoLuong,
                model.PhienBan, cancellationToken);
            return KetQuaVe(model.MaDatSan, ketQua);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaSoLuong(SuaSoLuongDichVuViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BaoLoiForm(model.MaDatSan);
        try
        {
            var ketQua = await dichVuTrongDonService.SuaSoLuongAsync(model.MaDatSan, model.MaChiTiet, model.SoLuong,
                model.PhienBan, cancellationToken);
            return KetQuaVe(model.MaDatSan, ketQua);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int maDatSan, int maChiTiet, string phienBan,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phienBan)) return BaoLoiForm(maDatSan);
        try
        {
            var ketQua = await dichVuTrongDonService.HuyAsync(maDatSan, maChiTiet, phienBan, cancellationToken);
            return KetQuaVe(maDatSan, ketQua);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private IActionResult BaoLoiForm(int maDatSan)
    {
        TempData["Error"] = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage
            ?? "Dữ liệu không hợp lệ.";
        return RedirectToAction(nameof(Index), new { maDatSan });
    }

    private IActionResult KetQuaVe(int maDatSan, KetQuaNghiepVu ketQua)
    {
        if (ketQua.KhongTimThay) return NotFound();
        if (ketQua.ThanhCong) TempData["Success"] = ketQua.ThongBao;
        else TempData["Error"] = ketQua.Loi;
        return RedirectToAction(nameof(Index), new { maDatSan });
    }
}
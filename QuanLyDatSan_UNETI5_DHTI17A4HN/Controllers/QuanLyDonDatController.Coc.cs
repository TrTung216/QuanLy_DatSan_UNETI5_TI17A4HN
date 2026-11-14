// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Nối giao diện quản lý với service đối soát.
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.QuanLy;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

public partial class QuanLyDonDatController
{
    [HttpGet]
    public Task<IActionResult> DoiSoatCoc(int id, CancellationToken cancellationToken) =>
        HienThiDoiSoatAsync(id, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanCoc(XacNhanCocViewModel model, CancellationToken cancellationToken)
    {
        if (!KiemTraPhienBan(model.PhienBan, out var phienBan))
            return await HienThiDoiSoatAsync(model.MaDon, cancellationToken);
        var result = await datSanService.XacNhanCocVaDatSanAsync(
            new(model.MaDon, model.MaGiaoDich, phienBan, model.DaKiemTraGiaoDich), cancellationToken);
        if (!result.ThanhCong)
        {
            if (result.Loi == MaLoiCoc.KhongDuQuyen) return Forbid();
            if (result.Loi == MaLoiCoc.KhongTonTai) return NotFound();
            ModelState.AddModelError(string.Empty, result.LoiDatSan.HasValue
                ? ThongBaoLoiDatSanViewModel.AnhXa(new LoiDatSan(result.LoiDatSan.Value)).ThongBao
                : ThongBaoDoiSoatCoc.Loi(result.Loi!.Value));
            return await HienThiDoiSoatAsync(model.MaDon, cancellationToken);
        }
        TempData["Success"] = result.DuLieu!.DaXuLyTruocDo
            ? "Giao dịch này đã được đối soát; không ghi cọc lần nữa."
            : "Đã đối soát cọc và xác nhận đặt sân.";
        return RedirectToAction(nameof(DoiSoatCoc), new { id = model.MaDon });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoGiaoDichMoPhong(TaoGiaoDichMoPhongViewModel model, CancellationToken cancellationToken)
    {
        // Kiểm tra lại quyền và sự tồn tại của đơn trước khi ghi nguồn mô phỏng.
        var don = await datSanService.LayDoiSoatCocAsync(model.MaDon, cancellationToken);
        if (!don.ThanhCong)
        {
            if (don.Loi == MaLoiCoc.KhongDuQuyen) return Forbid();
            if (don.Loi == MaLoiCoc.KhongTonTai) return NotFound();
            ModelState.AddModelError(string.Empty, ThongBaoDoiSoatCoc.Loi(don.Loi!.Value));
        }
        if (!ModelState.IsValid) return await HienThiDoiSoatAsync(model.MaDon, cancellationToken);
        var result = await datSanService.TaoGiaoDichMoPhongAsync(
            new(model.MaGiaoDichNgoai, model.NoiDung, model.SoTien!.Value, model.NgayNhan!.Value), cancellationToken);
        if (!result.ThanhCong)
        {
            if (result.Loi == MaLoiCoc.KhongDuQuyen) return Forbid();
            ModelState.AddModelError(string.Empty, ThongBaoDoiSoatCoc.Loi(result.Loi!.Value));
            return await HienThiDoiSoatAsync(model.MaDon, cancellationToken);
        }
        TempData["Success"] = "Đã ghi giao dịch mô phỏng. Đơn chưa được xác nhận; cần đối soát riêng. Giao dịch chỉ xuất hiện trong danh sách khi nội dung khớp đơn.";
        return RedirectToAction(nameof(DoiSoatCoc), new { id = model.MaDon });
    }

    private async Task<IActionResult> HienThiDoiSoatAsync(int id, CancellationToken ct)
    {
        if (id <= 0) return NotFound();
        var result = await datSanService.LayDoiSoatCocAsync(id, ct);
        if (!result.ThanhCong)
        {
            if (result.Loi == MaLoiCoc.KhongDuQuyen) return Forbid();
            if (result.Loi == MaLoiCoc.KhongTonTai) return NotFound();
            ModelState.AddModelError(string.Empty, ThongBaoDoiSoatCoc.Loi(result.Loi!.Value));
            return await HienThiLoiAsync(id, null, null, null, ct);
        }
        return View(nameof(DoiSoatCoc), new DoiSoatCocViewModel
        {
            DuLieu = result.DuLieu!, PhienBan = Convert.ToBase64String(result.DuLieu!.PhienBan),
            CoTheMoPhong = result.DuLieu.CoTheMoPhong
        });
    }
}

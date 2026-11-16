// M1: Trần Trọng Tùng; MSSV 23103100202. Tích hợp luồng khách thêm dịch vụ M3/M5 theo yêu cầu.
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.KhachHang;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

public partial class DonDatController
{
    [HttpGet]
    public async Task<IActionResult> DichVu(int id, CancellationToken cancellationToken)
    {
        var model = await LayDichVuViewModelAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemDichVu([Bind(Prefix = "Form")] ThemDichVuTrongDonViewModel form,
        CancellationToken cancellationToken)
    {
        // Luôn kiểm tra chủ đơn, kể cả request có dữ liệu form sai.
        var model = await LayDichVuViewModelAsync(form.MaDatSan, cancellationToken);
        if (model is null) return NotFound();
        if (ModelState.IsValid)
        {
            var ketQua = await dichVuTrongDonService.KhachHangThemAsync(form.MaDatSan, form.MaDichVu,
                form.SoLuong, form.PhienBan, cancellationToken);
            if (ketQua.KhongTimThay) return NotFound();
            if (ketQua.ThanhCong)
            {
                TempData["Success"] = ketQua.ThongBao;
                return RedirectToAction(nameof(DichVu), new { id = form.MaDatSan });
            }
            ModelState.AddModelError(string.Empty, ketQua.Loi ?? "Không thể thêm dịch vụ.");
            model = await LayDichVuViewModelAsync(form.MaDatSan, cancellationToken);
            if (model is null) return NotFound();
        }
        model.Form.MaDichVu = form.MaDichVu;
        model.Form.SoLuong = form.SoLuong;
        // Giữ lựa chọn nhưng dùng phiên bản mới khi khách đọc lỗi rồi gửi lại.
        ModelState.Remove("Form.PhienBan");
        return View(nameof(DichVu), model);
    }

    private async Task<DichVuDonKhachHangViewModel?> LayDichVuViewModelAsync(int id, CancellationToken cancellationToken)
    {
        var duLieu = await dichVuTrongDonService.LayChoKhachHangAsync(id, cancellationToken);
        if (duLieu is null) return null;
        return new DichVuDonKhachHangViewModel
        {
            Form = new() { MaDatSan = id, PhienBan = duLieu.PhienBan },
            ChoPhepThem = duLieu.ChoPhepSua,
            TongTienDichVu = duLieu.TongTienDichVu,
            LuaChon = duLieu.DichVuCoTheThem.Select(x => new LuaChonDichVuKhachHang(
                x.MaDichVu, x.TenDichVu, x.DonViTinh, x.DonGia)).ToArray(),
            Dong = duLieu.Dong.Select(x => new DichVuCaNhanViewModel(x.TenDichVu, x.DonViTinh,
                x.SoLuong, x.DonGia, x.ThanhTien, x.DaHuy, x.NgayHuy)).ToArray()
        };
    }
}

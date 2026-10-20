// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-02/03 - tra cứu đơn và POST xác nhận, từ chối, hủy cho Admin/NhanVien.
// Ánh xạ DTO/ViewModel, kiểm tra dữ liệu và RowVersion; giữ lý do khi lỗi, nạp lại trạng thái và chuyển hướng nội bộ.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanLyDonDatController(IDatSanService datSanService, IDongHo dongHo) : Controller
{
    [HttpGet]
    public async Task<IActionResult> DanhSach([FromQuery] LocDonQuanLyViewModel loc, CancellationToken cancellationToken)
    {
        var model = new DanhSachDonQuanLyViewModel
        {
            TuKhoa = loc.TuKhoa?.Trim(), MaLoaiSan = loc.MaLoaiSan, TrangThai = loc.TrangThai,
            TuNgay = loc.TuNgay, DenNgay = loc.DenNgay, SapXep = loc.SapXep, Trang = loc.Trang,
            LoaiSan = (await datSanService.LayLoaiSanLocDonAsync(cancellationToken))
                .Select(x => new LoaiSanLocViewModel(x.MaLoaiSan, x.TenLoai)).ToList()
        };
        if (!ModelState.IsValid) return View(model);

        var result = await datSanService.LayDanhSachQuanLyAsync(new LocDonQuanLy(model.TuKhoa,
            model.MaLoaiSan, model.TrangThai, model.TuNgay, model.DenNgay, model.SapXep, model.Trang), cancellationToken);
        model.Dong = result.Dong.Select(x => new DonQuanLyDongViewModel(x.MaDatSan, x.TenKhachHang,
            x.TenSan, x.TenLoai, x.NgayDat, x.GioBatDau, x.GioKetThuc, x.TrangThai, x.TienCoc)).ToList();
        model.Trang = result.Trang;
        model.TongTrang = result.TongTrang;
        model.TongSo = result.TongSo;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, string? quayLai, CancellationToken cancellationToken)
    {
        var model = await LayChiTietViewModelAsync(id, quayLai, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    private async Task<ChiTietDonQuanLyViewModel?> LayChiTietViewModelAsync(int id, string? quayLai,
        CancellationToken cancellationToken)
    {
        if (id <= 0) return null;
        var result = await datSanService.LayChiTietQuanLyAsync(id, cancellationToken);
        if (result is null) return null;
        var don = result.Don;
        var laDonCho = don.TrangThai == TrangThaiDatSan.ChoXuLy;
        var laDonXacNhan = don.TrangThai == TrangThaiDatSan.DangXuLy;
        var chuaBatDau = don.GioBatDau > dongHo.BayGio;
        return new ChiTietDonQuanLyViewModel
        {
            MaDatSan = don.MaDatSan, MaKhachHang = result.MaKhachHang,
            TenKhachHang = result.TenKhachHang, SoDienThoai = result.SoDienThoai, Email = result.Email,
            TenSan = don.TenSan, TenLoai = don.TenLoai, DiaChi = don.DiaChi,
            NgayDat = don.NgayDat, GioBatDau = don.GioBatDau, GioKetThuc = don.GioKetThuc,
            DonGia = don.DonGia, TienCoc = don.TienCoc, TrangThai = don.TrangThai,
            NgayXacNhan = don.NgayXacNhan, NgayHuy = don.NgayHuy, NgayHoanThanh = don.NgayHoanThanh,
            LyDoHuy = don.LyDoHuy, TienSan = don.TienSan, TienDichVu = don.TienDichVu,
            TongTien = don.TongTien, TienSanDuKien = don.TienSanDuKien,
            PhienBan = Convert.ToBase64String(don.PhienBan),
            CoTheXacNhan = laDonCho && chuaBatDau,
            CoTheTuChoi = laDonCho && don.TienCoc == 0m,
            CoTheNhanVienHuy = chuaBatDau && don.TienCoc == 0m && (laDonCho || laDonXacNhan),
            QuayLai = Url.IsLocalUrl(quayLai) ? quayLai! : Url.Action(nameof(DanhSach))!
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(XacNhanDonViewModel model, CancellationToken cancellationToken)
    {
        if (!KiemTraPhienBan(model.PhienBan, out var phienBan))
            return await HienThiLoiAsync(model.MaDon, model.QuayLai, null, null, cancellationToken);

        var result = await datSanService.XacNhanDonAsync(new XuLyDonYeuCau(model.MaDon, phienBan), cancellationToken);
        return await XuLyKetQuaAsync(model.MaDon, model.QuayLai, result, null, null, cancellationToken);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(HuyDonQuanLyViewModel model, CancellationToken cancellationToken)
    {
        if (!KiemTraPhienBan(model.PhienBan, out var phienBan))
            return await HienThiLoiAsync(model.MaDon, model.QuayLai, model.LyDo, null, cancellationToken);

        var result = await datSanService.TuChoiDonAsync(
            new HuyDonYeuCau(model.MaDon, model.LyDo, phienBan), cancellationToken);
        return await XuLyKetQuaAsync(model.MaDon, model.QuayLai, result, model.LyDo, null, cancellationToken);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(HuyDonQuanLyViewModel model, CancellationToken cancellationToken)
    {
        if (!KiemTraPhienBan(model.PhienBan, out var phienBan))
            return await HienThiLoiAsync(model.MaDon, model.QuayLai, null, model.LyDo, cancellationToken);

        var result = await datSanService.NhanVienHuyDonAsync(
            new HuyDonYeuCau(model.MaDon, model.LyDo, phienBan), cancellationToken);
        return await XuLyKetQuaAsync(model.MaDon, model.QuayLai, result, null, model.LyDo, cancellationToken);
    }

    private async Task<IActionResult> XuLyKetQuaAsync(int maDon, string? quayLai, KetQuaXuLyDon result,
        string? lyDoTuChoi, string? lyDoHuy, CancellationToken cancellationToken)
    {
        if (!result.ThanhCong)
        {
            ModelState.AddModelError(string.Empty,
                ThongBaoLoiDatSanViewModel.AnhXa(new LoiDatSan(result.Loi!.Value)).ThongBao);
            return await HienThiLoiAsync(maDon, quayLai, lyDoTuChoi, lyDoHuy, cancellationToken);
        }

        TempData["Success"] = "Đã cập nhật đơn đặt sân.";
        var anToan = Url.IsLocalUrl(quayLai) ? quayLai : Url.Action(nameof(DanhSach));
        return RedirectToAction(nameof(ChiTiet), new { id = maDon, quayLai = anToan });
    }

    private async Task<IActionResult> HienThiLoiAsync(int maDon, string? quayLai,
        string? lyDoTuChoi, string? lyDoHuy, CancellationToken cancellationToken)
    {
        var model = await LayChiTietViewModelAsync(maDon, quayLai, cancellationToken);
        if (model is null) return NotFound();
        model.LyDoTuChoiNhap = lyDoTuChoi;
        model.LyDoHuyNhap = lyDoHuy;
        // View dùng phiên bản vừa đọc từ database; dữ liệu nhập chỉ giữ lại để người dùng sửa.
        return View(nameof(ChiTiet), model);
    }

    private bool KiemTraPhienBan(string? encoded, out byte[] phienBan)
    {
        if (!TryLayPhienBan(encoded, out phienBan))
            ModelState.AddModelError(nameof(XacNhanDonViewModel.PhienBan),
                "Phiên bản đơn không hợp lệ. Hãy tải lại chi tiết đơn.");
        return ModelState.IsValid;
    }

    private static bool TryLayPhienBan(string? encoded, out byte[] phienBan)
    {
        phienBan = [];
        if (string.IsNullOrWhiteSpace(encoded)) return false;
        try
        {
            phienBan = Convert.FromBase64String(encoded);
            return phienBan.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

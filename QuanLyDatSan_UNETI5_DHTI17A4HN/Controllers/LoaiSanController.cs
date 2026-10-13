// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ Controller loại sân gọi service, giữ route và phản hồi.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class LoaiSanController(ILoaiSanService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? tuKhoa, TrangThaiLoaiSan? trangThai,
        string sapXep = "ten_tang", int trang = 1, CancellationToken cancellationToken = default)
    {
        tuKhoa = tuKhoa?.Trim();
        if (!ModelState.IsValid || tuKhoa?.Length > 100
            || (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))
            || sapXep is not ("ten_tang" or "ten_giam")) return BadRequest();
        var ketQua = await service.LayDanhSachAsync(tuKhoa, trangThai, sapXep, trang, cancellationToken);
        return View(new LoaiSanDanhSachViewModel
        {
            DanhSach = ketQua.DanhSach.Select(ThanhViewModel).ToList(),
            TuKhoa = tuKhoa, TrangThai = trangThai, SapXep = sapXep,
            Trang = ketQua.Trang, TongTrang = ketQua.TongTrang, TongSo = ketQua.TongSo
        });
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        var chiTiet = await service.LayChiTietAsync(id, cancellationToken);
        return chiTiet is null ? NotFound() : View(ThanhViewModel(chiTiet));
    }

    [HttpGet]
    public IActionResult Tao() => View(new LoaiSanFormViewModel { TrangThai = TrangThaiLoaiSan.HoatDong });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(LoaiSanFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var ketQua = await service.TaoAsync(ThanhYeuCau(model), cancellationToken);
        if (ketQua.MaLoi != MaLoiLoaiSan.ThanhCong)
        {
            ThemLoi(ketQua.MaLoi);
            return View(model);
        }
        TempData["Success"] = "Đã thêm loại sân.";
        return RedirectToAction(nameof(ChiTiet), new { id = ketQua.MaLoaiSan });
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken cancellationToken)
    {
        var loaiSan = await service.LayChiTietAsync(id, cancellationToken);
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
        // Giữ thứ tự: ID không tồn tại trả 404, kể cả khi form chưa hợp lệ.
        if (await service.LayChiTietAsync(id, cancellationToken) is null) return NotFound();
        ViewData["MaLoaiSan"] = id;
        if (!ModelState.IsValid) return View(model);
        var ketQua = await service.SuaAsync(id, ThanhYeuCau(model), cancellationToken);
        if (ketQua.MaLoi == MaLoiLoaiSan.KhongTonTai) return NotFound();
        if (ketQua.MaLoi != MaLoiLoaiSan.ThanhCong)
        {
            ThemLoi(ketQua.MaLoi);
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật loại sân.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Xoa(int id, CancellationToken cancellationToken)
    {
        var chiTiet = await service.LayChiTietAsync(id, cancellationToken);
        return chiTiet is null ? NotFound() : View(ThanhViewModel(chiTiet));
    }

    [HttpPost, ActionName(nameof(Xoa)), ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhanXoa(int id, CancellationToken cancellationToken)
    {
        var ketQua = await service.XoaAsync(id, cancellationToken);
        switch (ketQua.MaLoi)
        {
            case MaLoiLoaiSan.KhongTonTai: return NotFound();
            case MaLoiLoaiSan.DangDuocThamChieu:
                TempData["Error"] = "Không thể xóa loại sân đang được tham chiếu. Bạn có thể chuyển sang Ngừng hoạt động để giữ dữ liệu liên quan.";
                return RedirectToAction(nameof(Xoa), new { id });
            case MaLoiLoaiSan.ThanhCong:
                TempData["Success"] = "Đã xóa loại sân chưa được tham chiếu.";
                return RedirectToAction(nameof(DanhSach));
            default: throw new InvalidOperationException("Kết quả xóa loại sân chưa được ánh xạ.");
        }
    }

    private void ThemLoi(MaLoiLoaiSan loi)
    {
        var (truong, thongBao) = loi switch
        {
            MaLoiLoaiSan.TrungTen => (nameof(LoaiSanFormViewModel.TenLoai), "Tên loại sân đã được sử dụng."),
            MaLoiLoaiSan.XungDotDuLieu => (string.Empty, "Loại sân đã bị xóa trong lúc chỉnh sửa. Vui lòng trở lại danh sách."),
            MaLoiLoaiSan.DuLieuKhongHopLe => (string.Empty, "Thông tin loại sân không hợp lệ."),
            _ => throw new InvalidOperationException("Mã lỗi loại sân chưa được ánh xạ.")
        };
        ModelState.AddModelError(truong, thongBao);
    }

    private static GhiLoaiSan ThanhYeuCau(LoaiSanFormViewModel model) => new(
        model.TenLoai, model.MoTa, model.SoNguoiToiDa!.Value, model.DonGiaTheoGio!.Value, model.TrangThai!.Value);

    private static LoaiSanChiTietViewModel ThanhViewModel(LoaiSanDoc x) => new()
    {
        MaLoaiSan = x.MaLoaiSan, TenLoai = x.TenLoai, MoTa = x.MoTa, SoNguoiToiDa = x.SoNguoiToiDa,
        DonGiaTheoGio = x.DonGiaTheoGio, TrangThai = x.TrangThai, SoSan = x.SoSan
    };
}

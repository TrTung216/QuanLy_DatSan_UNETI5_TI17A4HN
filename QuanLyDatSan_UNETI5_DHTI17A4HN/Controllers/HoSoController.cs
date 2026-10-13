// M3: Nguyễn Văn Quý; MSSV: 23103100181. xem và sửa hồ sơ cá nhân của khách hàng đang đăng nhập.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class HoSoController(IKhachHangService khachHangService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var hoSo = await khachHangService.LayHoSoAsync(cancellationToken);
        if (hoSo is null) return KhongCoHoSo();
        return View(TaoViewModel(hoSo));
    }

    [HttpGet]
    public async Task<IActionResult> Sua(CancellationToken cancellationToken)
    {
        var hoSo = await khachHangService.LayHoSoAsync(cancellationToken);
        if (hoSo is null) return KhongCoHoSo();
        if (hoSo.TrangThai != TrangThaiKhachHang.HoatDong) return HoSoBiKhoa();
        return View(TaoViewModel(hoSo));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(HoSoViewModel model, CancellationToken cancellationToken)
    {
        var hoSo = await khachHangService.LayHoSoAsync(cancellationToken);
        if (hoSo is null) return KhongCoHoSo();
        if (hoSo.TrangThai != TrangThaiKhachHang.HoatDong) return HoSoBiKhoa();

        if (!ModelState.IsValid)
        {
            GanTruongChiDoc(model, hoSo);
            return View(model);
        }

        var dto = new CapNhatHoSoDto(model.HoTen, model.Email, model.SoDienThoai, model.NgaySinh,
            model.GioiTinh, model.DiaChi);
        var ketQua = await khachHangService.CapNhatHoSoAsync(dto, cancellationToken);

        if (ketQua.ThanhCong)
        {
            TempData["Success"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction(nameof(Index));
        }

        switch (ketQua.Loi)
        {
            case MaLoiHoSo.KhongTonTai:
                return KhongCoHoSo();
            case MaLoiHoSo.HoSoBiKhoa:
                return HoSoBiKhoa();
            case MaLoiHoSo.NgaySinhKhongHopLe:
                ModelState.AddModelError(nameof(model.NgaySinh), "Ngày sinh không hợp lệ.");
                GanTruongChiDoc(model, hoSo);
                return View(model);
            default:
                // Mã lỗi chưa được ánh xạ là lỗi lập trình, để xử lý lỗi chung ghi nhận.
                throw new InvalidOperationException($"Chưa ánh xạ mã lỗi hồ sơ: {ketQua.Loi}.");
        }
    }

    private static HoSoViewModel TaoViewModel(HoSoKhachDto hoSo) => new()
    {
        TenDangNhap = hoSo.TenDangNhap,
        HoTen = hoSo.HoTen,
        Email = hoSo.Email,
        SoDienThoai = hoSo.SoDienThoai,
        NgaySinh = hoSo.NgaySinh,
        GioiTinh = hoSo.GioiTinh,
        DiaChi = hoSo.DiaChi,
        NgayDangKy = hoSo.NgayDangKy,
        DiemTichLuy = hoSo.DiemTichLuy
    };

    // Các trường BindNever không đi theo form nên phải nạp lại khi hiển thị lại form lỗi.
    private static void GanTruongChiDoc(HoSoViewModel model, HoSoKhachDto hoSo)
    {
        model.TenDangNhap = hoSo.TenDangNhap;
        model.NgayDangKy = hoSo.NgayDangKy;
        model.DiemTichLuy = hoSo.DiemTichLuy;
    }

    private RedirectToActionResult KhongCoHoSo()
    {
        TempData["Error"] = "Không tìm thấy hồ sơ khách hàng của tài khoản này.";
        return RedirectToAction("ThongTin", "TaiKhoan");
    }

    private RedirectToActionResult HoSoBiKhoa()
    {
        TempData["Error"] = "Hồ sơ đang bị khóa, không thể chỉnh sửa.";
        return RedirectToAction(nameof(Index));
    }
}

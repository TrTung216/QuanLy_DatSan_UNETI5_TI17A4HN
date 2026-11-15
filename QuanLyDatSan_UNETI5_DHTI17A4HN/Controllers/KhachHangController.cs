// M3: Nguyễn Văn Quý; MSSV: 23103100181. đăng ký tài khoản khách hàng kèm hồ sơ trong cùng giao dịch.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Bổ sung danh mục khám phá sân qua service trang chủ dùng chung.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.KhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.KhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.TrangChu;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class KhachHangController(IDangKyKhachService service, ITaiKhoanHienTai taiKhoanHienTai,
    ITrangChuService trangChuService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> TrangChu(CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        var khachHang = await service.LayTrangChuAsync(cancellationToken);
        if (khachHang is null)
        {
            TempData["Error"] = "Không tìm thấy hồ sơ khách hàng của tài khoản này.";
            return RedirectToAction("ThongTin", "TaiKhoan");
        }

        var cacLoai = await trangChuService.LaySanTheoLoaiAsync(cancellationToken);
        var model = new TrangChuKhachViewModel
        {
            CacLoaiSan = cacLoai.Select(loai => new NhomSanTrangChuViewModel(
                loai.MaLoaiSan, loai.TenLoai, loai.San.Select(san => new TheSanTrangChuViewModel(
                    san.MaSan, san.TenSan, san.DiaChi, san.DonGia, san.GioMoCua, san.GioDongCua, san.CoAnh)).ToList(), loai.CoAnh)).ToList(),
            HoTen = khachHang.HoTen,
            DiemTichLuy = khachHang.DiemTichLuy,
            HoSoBiKhoa = khachHang.HoSoBiKhoa,
            SoDonChoXacNhan = khachHang.SoDonChoXacNhan,
            SoDonDaXacNhanSapToi = khachHang.SoDonDaXacNhanSapToi,
            DonSapToi = khachHang.DonSapToi.Select(x => new DonDatDongViewModel
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.TenSan,
                TenLoai = x.TenLoai,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai
            }).ToList()
        };
        return View(model);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult DangKy()
    {
        if (User.Identity?.IsAuthenticated == true) return VeTrangChuKhiDaDangNhap();
        return View(new DangKyKhachViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangKy(DangKyKhachViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return VeTrangChuKhiDaDangNhap();
        if (!ModelState.IsValid) return View(model);

        var ketQua = await service.DangKyAsync(new(model.TenDangNhap, model.MatKhau, model.HoTen, model.Email,
            model.SoDienThoai, model.NgaySinh, model.GioiTinh, model.DiaChi), cancellationToken);
        if (ketQua == MaLoiDangKyKhach.TrungTen) return TenDangNhapDaDung(model);
        if (ketQua == MaLoiDangKyKhach.DuLieuKhongHopLe)
        {
            ModelState.AddModelError(string.Empty, "Thông tin đăng ký không hợp lệ.");
            return View(model);
        }
        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction("DangNhap", "TaiKhoan");
    }

    private IActionResult VeTrangChuKhiDaDangNhap() => User.IsInRole(nameof(VaiTro.KhachHang))
        ? RedirectToAction(nameof(TrangChu))
        : RedirectToAction("Index", "Home");

    private ViewResult TenDangNhapDaDung(DangKyKhachViewModel model)
    {
        ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã được sử dụng.");
        return View(model);
    }
}

// M3: Nguyễn Văn Quý; MSSV: 23103100181. khách hàng tạo đơn đặt sân, xem lịch đặt cá nhân và chi tiết đơn của chính mình.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DonDatController(ApplicationDbContext db, ITaiKhoanHienTai taiKhoanHienTai,
    IDatSanService datSanService) : Controller
{
    private const int SoDongMoiTrang = 10;

    [HttpGet]
    public async Task<IActionResult> Index(TrangThaiDatSan? trangThai, int trang = 1, CancellationToken cancellationToken = default)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;

        var truyVan = db.DatSans.AsNoTracking().Where(x => x.KhachHang.MaTaiKhoan == maTaiKhoan);
        if (trangThai.HasValue) truyVan = truyVan.Where(x => x.TrangThai == trangThai.Value);

        var tongSo = await truyVan.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);

        var dong = await truyVan
            .OrderByDescending(x => x.GioBatDau).ThenByDescending(x => x.MaDatSan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)
            .Select(x => new DonDatDongViewModel
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.SanTheThao.TenSan,
                TenLoai = x.SanTheThao.LoaiSan.TenLoai,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai
            })
            .ToListAsync(cancellationToken);

        return View(new DanhSachDonViewModel
        {
            Dong = dong, TrangThai = trangThai, Trang = trang, TongTrang = tongTrang, TongSo = tongSo
        });
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        var don = await db.DatSans.AsNoTracking()
            .Where(x => x.MaDatSan == id && x.KhachHang.MaTaiKhoan == maTaiKhoan)
            .Select(x => new ChiTietDonViewModel
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.SanTheThao.TenSan,
                TenLoai = x.SanTheThao.LoaiSan.TenLoai,
                DiaChi = x.SanTheThao.DiaChi,
                NgayDat = x.NgayDat,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TienCoc = x.TienCoc,
                TrangThai = x.TrangThai,
                NgayXacNhan = x.NgayXacNhan,
                NgayHuy = x.NgayHuy,
                NgayHoanThanh = x.NgayHoanThanh,
                LyDoHuy = x.LyDoHuy,
                TienSan = x.TienSan,
                TienDichVu = x.TienDichVu,
                TongTien = x.TongTien
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (don is null) return NotFound();

        don.TienSanDuKien = DatSanService.TinhTienSanDuKien(don.DonGia, don.GioBatDau, don.GioKetThuc);
        return View(don);
    }

    [HttpGet]
    public async Task<IActionResult> Tao(int? maSan, CancellationToken cancellationToken)
    {
        var model = new TaoDonViewModel { MaSan = maSan };
        await NapDanhSachSanAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaoDonViewModel model, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();
        if (!ModelState.IsValid)
        {
            await NapDanhSachSanAsync(model, cancellationToken);
            return View(model);
        }

        var ketQua = await datSanService.TaoDonAsync(maTaiKhoan, model.MaSan!.Value, model.NgaySuDung!.Value,
            model.GioBatDau!.Value, model.GioKetThuc!.Value, cancellationToken);
        if (!ketQua.ThanhCong)
        {
            ModelState.AddModelError(ketQua.TruongLoi ?? string.Empty, ketQua.Loi!);
            await NapDanhSachSanAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Đặt sân thành công. Đơn đang chờ nhân viên xác nhận.";
        return RedirectToAction(nameof(ChiTiet), new { id = ketQua.MaDatSan });
    }

    private async Task NapDanhSachSanAsync(TaoDonViewModel model, CancellationToken cancellationToken)
    {
        model.DanhSachSan = await db.SanTheThaos.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiSan.HoatDong && x.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong)
            .OrderBy(x => x.LoaiSan.TenLoai).ThenBy(x => x.TenSan)
            .Select(x => new SanChonViewModel(x.MaSan, x.TenSan, x.LoaiSan.TenLoai, x.DonGia, x.GioMoCua, x.GioDongCua))
            .ToListAsync(cancellationToken);
    }
}

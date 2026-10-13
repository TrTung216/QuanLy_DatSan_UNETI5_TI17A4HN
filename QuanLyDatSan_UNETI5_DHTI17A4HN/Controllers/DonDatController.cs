// M3: Nguyễn Văn Quý; MSSV: 23103100181. khách hàng tạo đơn đặt sân, xem lịch đặt cá nhân và chi tiết đơn của chính mình.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung M4: chuyển truy vấn sang IDatSanService; ánh xạ DTO sang ViewModel và mã lỗi sang thông báo cho luồng đặt sân của khách hàng.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DonDatController(ITaiKhoanHienTai taiKhoanHienTai,
    IDatSanService datSanService) : Controller
{

    [HttpGet]
    public async Task<IActionResult> Index(TrangThaiDatSan? trangThai, int trang = 1, CancellationToken cancellationToken = default)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();
        var duLieu = await datSanService.LayDanhSachCaNhanAsync(maTaiKhoan, trangThai, trang, cancellationToken);
        return View(new DanhSachDonViewModel
        {
            Dong = duLieu.Dong.Select(x => new DonDatDongViewModel
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.TenSan,
                TenLoai = x.TenLoai,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai,
            }).ToList(),
            TrangThai = duLieu.TrangThai, Trang = duLieu.Trang,
            TongTrang = duLieu.TongTrang, TongSo = duLieu.TongSo
        });
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        var don = await datSanService.LayChiTietCaNhanAsync(maTaiKhoan, id, cancellationToken);
        if (don is null) return NotFound();
        return View(new ChiTietDonViewModel
        {
            MaDatSan = don.MaDatSan,
            TenSan = don.TenSan,
            TenLoai = don.TenLoai,
            DiaChi = don.DiaChi,
            NgayDat = don.NgayDat,
            GioBatDau = don.GioBatDau,
            GioKetThuc = don.GioKetThuc,
            DonGia = don.DonGia,
            TienCoc = don.TienCoc,
            TrangThai = don.TrangThai,
            NgayXacNhan = don.NgayXacNhan,
            NgayHuy = don.NgayHuy,
            NgayHoanThanh = don.NgayHoanThanh,
            LyDoHuy = don.LyDoHuy,
            TienSan = don.TienSan,
            TienDichVu = don.TienDichVu,
            TongTien = don.TongTien,
            TienSanDuKien = don.TienSanDuKien,
        });
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

        var ketQua = await datSanService.TaoDonAsync(new TaoDonYeuCau(maTaiKhoan, model.MaSan!.Value, model.NgaySuDung!.Value,
            model.GioBatDau!.Value, model.GioKetThuc!.Value), cancellationToken);
        if (!ketQua.ThanhCong)
        {
            var loi = ThongBaoLoiDatSan.AnhXa(ketQua.Loi!);
            ModelState.AddModelError(loi.Truong, loi.ThongBao);
            await NapDanhSachSanAsync(model, cancellationToken);
            return View(model);
        }

        TempData["ThanhCong"] = "Đặt sân thành công. Đơn đang chờ nhân viên xác nhận.";
        return RedirectToAction(nameof(ChiTiet), new { id = ketQua.MaDatSan });
    }

    private async Task NapDanhSachSanAsync(TaoDonViewModel model, CancellationToken cancellationToken)
    {
        var luaChon = await datSanService.LayLuaChonSanAsync(cancellationToken);
        model.DanhSachSan = luaChon.Select(x => new SanChonViewModel(x.MaSan, x.TenSan, x.TenLoai,
            x.DonGia, x.GioMoCua, x.GioDongCua)).ToList();
    }
}

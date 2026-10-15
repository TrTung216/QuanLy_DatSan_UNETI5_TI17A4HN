// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: Phân quyền Admin/NhanVien, nhận bộ lọc, gọi service và ánh xạ DTO sang ViewModel để tra cứu đơn.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanLyDonDatController(IDatSanService datSanService) : Controller
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
        if (id <= 0) return NotFound();
        var result = await datSanService.LayChiTietQuanLyAsync(id, cancellationToken);
        if (result is null) return NotFound();
        var don = result.Don;
        return View(new ChiTietDonQuanLyViewModel
        {
            MaDatSan = don.MaDatSan, MaKhachHang = result.MaKhachHang,
            TenKhachHang = result.TenKhachHang, SoDienThoai = result.SoDienThoai, Email = result.Email,
            TenSan = don.TenSan, TenLoai = don.TenLoai, DiaChi = don.DiaChi,
            NgayDat = don.NgayDat, GioBatDau = don.GioBatDau, GioKetThuc = don.GioKetThuc,
            DonGia = don.DonGia, TienCoc = don.TienCoc, TrangThai = don.TrangThai,
            NgayXacNhan = don.NgayXacNhan, NgayHuy = don.NgayHuy, NgayHoanThanh = don.NgayHoanThanh,
            LyDoHuy = don.LyDoHuy, TienSan = don.TienSan, TienDichVu = don.TienDichVu,
            TongTien = don.TongTien, TienSanDuKien = don.TienSanDuKien,
            QuayLai = Url.IsLocalUrl(quayLai) ? quayLai! : Url.Action(nameof(DanhSach))!
        });
    }
}

// M3: Nguyễn Văn Quý; MSSV: 23103100181. trang điều hành cho Admin và Nhân viên với số liệu thật từ database.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanTriController(ApplicationDbContext db, IDongHo dongHo) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var bayGio = dongHo.BayGio;
        var model = new TrangDieuHanhViewModel
        {
            SoDonChoXacNhan = await db.DatSans.CountAsync(
                x => x.TrangThai == TrangThaiDatSan.ChoXuLy && x.GioKetThuc > bayGio, cancellationToken),
            SoKhachHangHoatDong = await db.KhachHangs.CountAsync(
                x => x.TrangThai == TrangThaiKhachHang.HoatDong, cancellationToken),
            SoSanHoatDong = await db.SanTheThaos.CountAsync(
                x => x.TrangThai == TrangThaiSan.HoatDong, cancellationToken)
        };
        if (User.IsInRole(nameof(VaiTro.Admin)))
        {
            model.SoDichVuHoatDong = await db.DichVus.CountAsync(
                x => x.TrangThai == TrangThaiDichVu.HoatDong, cancellationToken);
        }
        return View(model);
    }
}

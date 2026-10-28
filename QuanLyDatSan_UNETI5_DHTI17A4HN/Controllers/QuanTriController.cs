// M3: Nguyễn Văn Quý; MSSV: 23103100181. trang điều hành cho Admin và Nhân viên với số liệu thật từ database.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanTriController(IDieuHanhService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var duLieu = await service.LaySoLieuAsync(cancellationToken);
        var model = new TrangDieuHanhViewModel
        {
            SoDonChoXacNhan = duLieu.SoDonChoXacNhan,
            SoKhachHangHoatDong = duLieu.SoKhachHangHoatDong,
            SoSanHoatDong = duLieu.SoSanHoatDong
        };
        return View(model);
    }
}

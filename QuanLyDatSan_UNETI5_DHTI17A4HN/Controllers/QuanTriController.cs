// M3: Nguyễn Văn Quý; MSSV: 23103100181. trang điều hành cho Admin và Nhân viên với số liệu thật từ database.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DieuHanh;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanTriController(IDieuHanhService service, IThongKeService thongKeService, IDongHo dongHo,
    ILogger<QuanTriController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var duLieu = await service.LaySoLieuAsync(cancellationToken);

        var homNay = dongHo.HomNay;
        var boLoc = new BoLocThongKe(new DateOnly(homNay.Year, homNay.Month, 1), homNay, CheDoThongKe.Ngay);
        KetQuaThongKe? thongKe = null;
        // M1: Trần Trọng Tùng; MSSV 23103100202. Giữ quyền thống kê chỉ Admin như ThongKeController.
        if (User.IsInRole(nameof(VaiTro.Admin)))
        {
            try
            {
                thongKe = await thongKeService.LayAsync(boLoc, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Không tải được thống kê tháng trên trang điều hành.");
            }
        }

        var model = new TrangDieuHanhViewModel
        {
            SoDonChoXacNhan = duLieu.SoDonChoXacNhan,
            SoKhachHangHoatDong = duLieu.SoKhachHangHoatDong,
            SoSanHoatDong = duLieu.SoSanHoatDong,
            ThongKe = thongKe
        };
        return View(model);
    }
}

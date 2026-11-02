// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: trang thống kê (chỉ Admin). Controller mỏng, gọi IThongKeService.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin))]
public class ThongKeController(IThongKeService thongKeService, IDongHo dongHo) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? tuNgay, DateOnly? denNgay, CheDoThongKe? cheDo,
        CancellationToken cancellationToken)
    {
        // Mặc định: từ đầu tháng hiện tại đến hôm nay, theo ngày.
        var homNay = dongHo.HomNay;
        var macDinh = new BoLocThongKe(new DateOnly(homNay.Year, homNay.Month, 1), homNay, CheDoThongKe.Ngay);
        var boLoc = new BoLocThongKe(tuNgay ?? macDinh.TuNgay, denNgay ?? macDinh.DenNgay, cheDo ?? macDinh.CheDo);

        var loi = thongKeService.KiemTraBoLoc(boLoc);
        if (loi is not null) boLoc = macDinh;

        var model = new ThongKeViewModel
        {
            BoLoc = boLoc,
            KetQua = await thongKeService.LayAsync(boLoc, cancellationToken),
            ThongBaoLoi = loi
        };
        return View(model);
    }
} 
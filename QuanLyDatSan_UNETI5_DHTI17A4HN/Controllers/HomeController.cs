// M1: Trần Trọng Tùng; MSSV: 23103100202. Nạp danh mục sân công khai qua service cho trang chủ.
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using System.Diagnostics;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers
{
    public class HomeController(ITrangChuService trangChuService) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var cacLoai = await trangChuService.LaySanTheoLoaiAsync(cancellationToken);
            return View(new TrangChuViewModel(cacLoai.Select(loai => new NhomSanTrangChuViewModel(
                loai.MaLoaiSan, loai.TenLoai, loai.San.Select(san => new TheSanTrangChuViewModel(
                    san.MaSan, san.TenSan, san.DiaChi, san.DonGia, san.GioMoCua, san.GioDongCua)).ToList())).ToList()));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

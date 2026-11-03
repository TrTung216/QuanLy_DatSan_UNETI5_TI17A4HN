// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: dữ liệu trang thống kê.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class ThongKeViewModel
{
    public BoLocThongKe BoLoc { get; set; } = null!;
    public KetQuaThongKe KetQua { get; set; } = null!;
    public string? ThongBaoLoi { get; set; }
}
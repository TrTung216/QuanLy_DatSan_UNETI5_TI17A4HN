// M3: Nguyễn Văn Quý; MSSV: 23103100181. dữ liệu trang chủ của khách hàng sau khi đăng nhập.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class TrangChuKhachViewModel
{
    public string HoTen { get; set; } = string.Empty;
    public int DiemTichLuy { get; set; }
    public bool HoSoBiKhoa { get; set; }
    public int SoDonChoXacNhan { get; set; }
    public int SoDonDaXacNhanSapToi { get; set; }
    public List<DonDatDongViewModel> DonSapToi { get; set; } = [];
}

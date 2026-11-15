using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.KhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.TrangChu;
// M3: Nguyễn Văn Quý; MSSV: 23103100181. dữ liệu trang chủ của khách hàng sau khi đăng nhập.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Dùng chung dữ liệu thẻ sân với trang chủ công khai.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.KhachHang;

public class TrangChuKhachViewModel
{
    public IReadOnlyList<NhomSanTrangChuViewModel> CacLoaiSan { get; set; } = [];
    public string HoTen { get; set; } = string.Empty;
    public int DiemTichLuy { get; set; }
    public bool HoSoBiKhoa { get; set; }
    public int SoDonChoXacNhan { get; set; }
    public int SoDonDaXacNhanSapToi { get; set; }
    public List<DonDatDongViewModel> DonSapToi { get; set; } = [];
}

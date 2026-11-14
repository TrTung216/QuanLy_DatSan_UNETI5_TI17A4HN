// M1: Trần Trọng Tùng; MSSV: 23103100202. Dữ liệu hiển thị sân theo loại trên trang chủ.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.TrangChu;

public record TrangChuViewModel(IReadOnlyList<NhomSanTrangChuViewModel> CacLoaiSan);
public record NhomSanTrangChuViewModel(int MaLoaiSan, string TenLoai, IReadOnlyList<TheSanTrangChuViewModel> San, bool CoAnh = false);
public record TheSanTrangChuViewModel(int MaSan, string TenSan, string DiaChi, decimal DonGia,
    TimeOnly GioMoCua, TimeOnly GioDongCua, bool CoAnh = false);

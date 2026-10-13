// M3: Nguyễn Văn Quý; MSSV: 23103100181. dữ liệu hiển thị lịch đặt cá nhân và chi tiết đơn của khách hàng.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DonDatDongViewModel
{
    public int MaDatSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public TrangThaiDatSan TrangThai { get; set; }
}

public class DanhSachDonViewModel
{
    public List<DonDatDongViewModel> Dong { get; set; } = [];
    public TrangThaiDatSan? TrangThai { get; set; }
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
    public int TongSo { get; set; }
}

public class ChiTietDonViewModel
{
    public int MaDatSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public DateTime NgayDat { get; set; }
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public decimal TienCoc { get; set; }
    public TrangThaiDatSan TrangThai { get; set; }
    public DateTime? NgayXacNhan { get; set; }
    public DateTime? NgayHuy { get; set; }
    public DateTime? NgayHoanThanh { get; set; }
    public string? LyDoHuy { get; set; }
    public decimal? TienSan { get; set; }
    public decimal? TienDichVu { get; set; }
    public decimal? TongTien { get; set; }
    public decimal TienSanDuKien { get; set; }
}

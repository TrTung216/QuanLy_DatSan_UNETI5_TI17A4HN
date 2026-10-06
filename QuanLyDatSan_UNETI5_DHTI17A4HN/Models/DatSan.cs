// M3: Nguyễn Văn Quý; MSSV: 23103100181. đơn đặt sân (M3 tạo đơn, M4 xử lý trạng thái); chưa có ChiTietDatSan của M5.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class DatSan
{
    public int MaDatSan { get; set; }
    public int MaKhachHang { get; set; }
    public KhachHang KhachHang { get; set; } = null!;
    public int MaSan { get; set; }
    public SanTheThao SanTheThao { get; set; } = null!;
    public DateTime NgayDat { get; set; }
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public decimal TienCoc { get; set; }
    public TrangThaiDatSan TrangThai { get; set; } = TrangThaiDatSan.ChoXuLy;
    public DateTime? NgayXacNhan { get; set; }
    public DateTime? NgayHoanThanh { get; set; }
    public DateTime? NgayHuy { get; set; }
    public string? LyDoHuy { get; set; }
    public decimal? TienSan { get; set; }
    public decimal? TienDichVu { get; set; }
    public decimal? TongTien { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

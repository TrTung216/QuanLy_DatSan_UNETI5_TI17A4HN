// M3: Nguyễn Văn Quý; MSSV: 23103100181. hồ sơ khách hàng gắn một-một với tài khoản.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class KhachHang
{
    public int MaKhachHang { get; set; }
    public int MaTaiKhoan { get; set; }
    public TaiKhoan TaiKhoan { get; set; } = null!;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly? NgaySinh { get; set; }
    public GioiTinh? GioiTinh { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public DateTime NgayDangKy { get; set; }
    public int DiemTichLuy { get; set; }
    public TrangThaiKhachHang TrangThai { get; set; } = TrangThaiKhachHang.HoatDong;
    public string? GhiChu { get; set; }
    public List<DatSan> DanhSachDatSan { get; set; } = [];
}

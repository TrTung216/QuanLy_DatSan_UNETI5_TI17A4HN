// Đây là phần của Quý, thiếu gì tự bổ sung....
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class DatSan
{
    public int MaDon { get; set; }
    public int MaSan { get; set; }
    public SanTheThao San { get; set; } = null!;
    // Liên kết tài khoản M1; không tạo thêm bảng người dùng.
    public int MaKhachHang { get; set; }
    public TaiKhoan KhachHang { get; set; } = null!;
    // Giờ địa phương Việt Nam, cùng quy ước giờ mở/đóng cửa của M2.
    public DateTime BatDau { get; set; }
    public DateTime KetThuc { get; set; }
    public TrangThaiDonDatSan TrangThai { get; set; } = TrangThaiDonDatSan.ChoXacNhan;
    public string? GhiChu { get; set; }
    // Các mốc kiểm toán dùng UTC.
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
    public byte[] PhienBan { get; set; } = [];
}

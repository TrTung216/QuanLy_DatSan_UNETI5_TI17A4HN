// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ soạn mã.
// Nội dung: dữ liệu tài khoản; MatKhau chỉ chứa giá trị băm.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. thêm navigation KhachHang.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class TaiKhoan
{
    public int MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public VaiTro VaiTro { get; set; } = VaiTro.KhachHang;
    public TrangThaiTaiKhoan TrangThai { get; set; } = TrangThaiTaiKhoan.HoatDong;
    public KhachHang? KhachHang { get; set; }
}

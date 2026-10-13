// M3: Nguyễn Văn Quý; MSSV: 23103100181. hợp đồng service hồ sơ cá nhân của khách hàng đang đăng nhập.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

// Dữ liệu đọc ra cho màn hình hồ sơ; không chứa mật khẩu hay mã nội bộ.
public record HoSoKhachDto(
    string TenDangNhap, string HoTen, string Email, string SoDienThoai, DateOnly? NgaySinh,
    GioiTinh GioiTinh, string? DiaChi, DateTime NgayDangKy, int DiemTichLuy, TrangThaiKhachHang TrangThai);

// Chỉ các trường khách được phép sửa. Mã khách, trạng thái, điểm tích lũy do server quyết định.
public record CapNhatHoSoDto(
    string HoTen, string Email, string SoDienThoai, DateOnly? NgaySinh, GioiTinh GioiTinh, string? DiaChi);

public enum MaLoiHoSo
{
    KhongTonTai,
    HoSoBiKhoa,
    NgaySinhKhongHopLe
}

// Thành công thì không có mã lỗi; thất bại luôn có đúng một mã lỗi.
public record KetQuaCapNhatHoSo
{
    public bool ThanhCong => Loi is null;
    public MaLoiHoSo? Loi { get; private init; }

    public static KetQuaCapNhatHoSo ThanhCongMoi() => new();
    public static KetQuaCapNhatHoSo ThatBai(MaLoiHoSo loi) => new() { Loi = loi };
}

public interface IKhachHangService
{
    // Hồ sơ của tài khoản đang đăng nhập; null nếu chưa đăng nhập hoặc không có hồ sơ.
    Task<HoSoKhachDto?> LayHoSoAsync(CancellationToken cancellationToken);

    // Cập nhật hồ sơ của tài khoản đang đăng nhập; đồng bộ HoTen/Email sang TaiKhoan trong cùng lần lưu.
    Task<KetQuaCapNhatHoSo> CapNhatHoSoAsync(CapNhatHoSoDto dto, CancellationToken cancellationToken);
}

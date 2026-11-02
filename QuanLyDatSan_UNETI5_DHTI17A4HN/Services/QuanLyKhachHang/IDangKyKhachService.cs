// Codex: tách nghiệp vụ đăng ký/trang chủ từ KhachHangController của M3.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

public sealed record DangKyKhach(string TenDangNhap, string MatKhau, string HoTen, string Email,
    string SoDienThoai, DateOnly? NgaySinh, GioiTinh? GioiTinh, string? DiaChi);
public enum MaLoiDangKyKhach { ThanhCong, TrungTen, DuLieuKhongHopLe }
public sealed record TrangChuKhachDto(string HoTen, int DiemTichLuy, bool HoSoBiKhoa,
    int SoDonChoXacNhan, int SoDonDaXacNhanSapToi, IReadOnlyList<DonDatDongDto> DonSapToi);
public interface IDangKyKhachService
{
    Task<MaLoiDangKyKhach> DangKyAsync(DangKyKhach yeuCau, CancellationToken cancellationToken);
    Task<TrangChuKhachDto?> LayTrangChuAsync(CancellationToken cancellationToken);
}
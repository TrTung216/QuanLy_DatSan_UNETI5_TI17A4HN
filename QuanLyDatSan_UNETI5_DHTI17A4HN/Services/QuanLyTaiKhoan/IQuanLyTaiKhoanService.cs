// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ DTO và mã lỗi quản lý tài khoản, không phụ thuộc MVC.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;

// Dùng class để ToString mặc định không in mật khẩu như positional record.
public sealed class TaoTaiKhoan
{
    public required string TenDangNhap { get; init; }
    public required string MatKhau { get; init; }
    public required string HoTen { get; init; }
    public required string Email { get; init; }
    public VaiTro VaiTro { get; init; }
    // Chỉ dùng khi tạo hồ sơ cho tài khoản khách; không nhận dữ liệu hệ thống của hồ sơ.
    public string? SoDienThoai { get; init; }
}
public record SuaTaiKhoan(string HoTen, string Email, VaiTro VaiTro, TrangThaiTaiKhoan TrangThai,
    string? SoDienThoai = null);
public record TaiKhoanDoc(int MaTaiKhoan, string TenDangNhap, string HoTen, string Email, VaiTro VaiTro,
    TrangThaiTaiKhoan TrangThai, bool CoHoSoKhachHang = false);
public record TrangTaiKhoan(IReadOnlyList<TaiKhoanDoc> DanhSach, int Trang, int TongTrang, int TongSo);
public enum MaLoiTaiKhoan { KhongTonTai, KhongDuQuyen, TrungTen, AdminCuoiCung, TuKhoaHoacHaQuyen, XungDotDuLieu, DuLieuKhongHopLe, SoDienThoaiHoSoKhongHopLe }
public record KetQuaQuanLyTaiKhoan(IReadOnlyList<MaLoiTaiKhoan> Loi)
{
    public bool ThanhCong => Loi.Count == 0;
}

// Người gọi: QuanLyTaiKhoanController (Admin). DTO đọc không có mật khẩu/hash.
// Mã người thao tác lấy từ ITaiKhoanHienTai trong service; VaiTro trong DTO là vai trò của tài khoản được quản lý.
public interface IQuanLyTaiKhoanService
{
    Task<TrangTaiKhoan> LayDanhSachAsync(string? tuKhoa, VaiTro? vaiTro, TrangThaiTaiKhoan? trangThai, int trang, CancellationToken cancellationToken);
    Task<TaiKhoanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken);
    Task<KetQuaQuanLyTaiKhoan> TaoAsync(TaoTaiKhoan yeuCau, CancellationToken cancellationToken);
    Task<KetQuaQuanLyTaiKhoan> SuaAsync(int id, SuaTaiKhoan yeuCau, CancellationToken cancellationToken);
}

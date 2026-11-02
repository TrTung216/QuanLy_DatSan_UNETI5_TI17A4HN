// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách truy cập dữ liệu, giữ hợp đồng nghiệp vụ.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;

public interface ITaiKhoanRepository
{
    Task<TrangTaiKhoan> LayDanhSachAsync(string? tuKhoa, VaiTro? vaiTro, TrangThaiTaiKhoan? trangThai, int trang, CancellationToken cancellationToken);
    Task<TaiKhoanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken);
    Task<TaiKhoan?> LayDeCapNhatAsync(int id, CancellationToken ct);
    Task<TaiKhoan?> LayDeXacThucAsync(string ten, CancellationToken ct);
    Task<bool> TrungTenAsync(string ten, CancellationToken ct);
    Task<List<int>> LayMaTheoQuyenAsync(VaiTro vaiTro, TrangThaiTaiKhoan trangThai, CancellationToken ct);
    void Them(TaiKhoan taiKhoan);
}

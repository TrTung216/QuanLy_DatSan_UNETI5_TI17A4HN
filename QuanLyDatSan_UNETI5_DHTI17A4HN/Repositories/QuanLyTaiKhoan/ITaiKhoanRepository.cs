// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;

public interface ITaiKhoanRepository
{
    Task<TrangTaiKhoan> LayDanhSachAsync(string? tuKhoa, VaiTro? vaiTro, TrangThaiTaiKhoan? trangThai, int trang, CancellationToken cancellationToken);
    Task<TaiKhoanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken);
    Task<TaiKhoan?> LayDeCapNhatAsync(int id, CancellationToken cancellationToken);
    Task<TaiKhoan?> LayTheoTenDeCapNhatAsync(string ten, CancellationToken cancellationToken);
    Task<bool> TrungTenAsync(string ten, CancellationToken cancellationToken);
    Task<List<int>> LayAdminHoatDongAsync(CancellationToken cancellationToken);
    void Them(TaiKhoan taiKhoan);
}

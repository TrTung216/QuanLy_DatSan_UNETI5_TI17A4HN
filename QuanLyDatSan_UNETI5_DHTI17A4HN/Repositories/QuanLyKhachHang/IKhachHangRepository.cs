// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;

public interface IKhachHangRepository
{
    Task<HoSoKhachDto?> LayHoSoAsync(int maTaiKhoan, CancellationToken cancellationToken);
    Task<KhachHang?> LayDeCapNhatAsync(int maTaiKhoan, CancellationToken cancellationToken);
}

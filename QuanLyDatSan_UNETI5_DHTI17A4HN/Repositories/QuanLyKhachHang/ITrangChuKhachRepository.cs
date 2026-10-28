// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;

public interface ITrangChuKhachRepository
{
    Task<TrangChuKhachDto?> LayTrangChuAsync(int maTaiKhoan, DateTime bayGio, CancellationToken cancellationToken);
}

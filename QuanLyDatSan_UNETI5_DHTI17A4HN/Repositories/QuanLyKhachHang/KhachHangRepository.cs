// Codex: tách truy vấn từ service KhachHang, giữ nguyên bộ lọc và tracking.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;

public class KhachHangRepository(ApplicationDbContext db) : IKhachHangRepository
{
    public async Task<HoSoKhachDto?> LayHoSoAsync(int maTaiKhoan, CancellationToken cancellationToken)
    {
        return await db.KhachHangs.AsNoTracking()
            .Where(x => x.MaTaiKhoan == maTaiKhoan)
            .Select(x => new HoSoKhachDto(
                x.TaiKhoan.TenDangNhap, x.HoTen, x.Email, x.SoDienThoai, x.NgaySinh,
                x.GioiTinh ?? GioiTinh.KhongKhaiBao, x.DiaChi, x.NgayDangKy, x.DiemTichLuy, x.TrangThai))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<KhachHang?> LayDeCapNhatAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
        db.KhachHangs.Include(x => x.TaiKhoan).SingleOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan, cancellationToken);
}

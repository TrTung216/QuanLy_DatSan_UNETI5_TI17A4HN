// Codex: tách truy vấn từ service TrangChuKhach, giữ nguyên bộ lọc và tracking.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;

public class TrangChuKhachRepository(ApplicationDbContext db) : ITrangChuKhachRepository
{
    public async Task<TrangChuKhachDto?> LayTrangChuAsync(int maTaiKhoan, DateTime bayGio, CancellationToken cancellationToken)
    {
        var khach = await db.KhachHangs.AsNoTracking().Where(x => x.MaTaiKhoan == maTaiKhoan)
            .Select(x => new { x.HoTen, x.DiemTichLuy, x.TrangThai }).SingleOrDefaultAsync(cancellationToken);
        if (khach is null) return null;
        var don = db.DatSans.AsNoTracking().Where(x => x.KhachHang.MaTaiKhoan == maTaiKhoan && x.GioKetThuc > bayGio
            && (x.TrangThai == TrangThaiDatSan.ChoXuLy || x.TrangThai == TrangThaiDatSan.DangXuLy));
        return new(khach.HoTen, khach.DiemTichLuy, khach.TrangThai != TrangThaiKhachHang.HoatDong,
            await don.CountAsync(x => x.TrangThai == TrangThaiDatSan.ChoXuLy, cancellationToken),
            await don.CountAsync(x => x.TrangThai == TrangThaiDatSan.DangXuLy, cancellationToken),
            await don.OrderBy(x => x.GioBatDau).ThenBy(x => x.MaDatSan).Take(3).Select(x => new DonDatDongDto
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.SanTheThao.TenSan,
                TenLoai = x.SanTheThao.LoaiSan.TenLoai,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai
            }).ToListAsync(cancellationToken));
    }
}

// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: truy vấn và tổng hợp số liệu thống kê từ DatSan và ChiTietDatSan.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyThongKe;

public class ThongKeRepository(ApplicationDbContext db) : IThongKeRepository
{
    private const int SoSanToiDa = 10;
    private const int SoDichVuToiDa = 5;

    public async Task<TongQuanThongKe> LayTongQuanAsync(
        DateTime tu, DateTime den, CancellationToken cancellationToken)
    {
        var donHoanThanh = DonHoanThanh(tu, den);
        var soDonHoanThanh = await donHoanThanh.CountAsync(cancellationToken);
        var tienSan = await donHoanThanh.SumAsync(x => x.TienSan ?? 0m, cancellationToken);
        var tienDichVu = await donHoanThanh.SumAsync(x => x.TienDichVu ?? 0m, cancellationToken);
        var tongDoanhThu = await donHoanThanh.SumAsync(x => x.TongTien ?? 0m, cancellationToken);

        return new TongQuanThongKe(soDonHoanThanh, tienSan, tienDichVu, tongDoanhThu);
    }

    public async Task<IReadOnlyList<SoDonTheoTrangThai>> LaySoDonTheoTrangThaiAsync(
        DateTime tu, DateTime den, CancellationToken cancellationToken) =>
        await db.DatSans.AsNoTracking()
            .Where(x => x.NgayDat >= tu && x.NgayDat < den)
            .GroupBy(x => x.TrangThai)
            .Select(g => new SoDonTheoTrangThai(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DuLieuDoanhThuNgay>> LayDoanhThuTheoNgayAsync(
        DateTime tu, DateTime den, CancellationToken cancellationToken)
    {
        var dong = await DonHoanThanh(tu, den)
            .GroupBy(x => x.NgayHoanThanh!.Value.Date)
            .Select(g => new
            {
                Ngay = g.Key,
                SoDon = g.Count(),
                TienSan = g.Sum(x => x.TienSan ?? 0m),
                TienDichVu = g.Sum(x => x.TienDichVu ?? 0m),
                TongTien = g.Sum(x => x.TongTien ?? 0m)
            })
            .ToListAsync(cancellationToken);

        return dong.Select(x => new DuLieuDoanhThuNgay(DateOnly.FromDateTime(x.Ngay),
            x.SoDon, x.TienSan, x.TienDichVu, x.TongTien)).ToList();
    }

    public async Task<IReadOnlyList<DuLieuDoanhThuThang>> LayDoanhThuTheoThangAsync(
        DateTime tu, DateTime den, CancellationToken cancellationToken)
    {
        var dong = await DonHoanThanh(tu, den)
            .GroupBy(x => new { x.NgayHoanThanh!.Value.Year, x.NgayHoanThanh!.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                SoDon = g.Count(),
                TienSan = g.Sum(x => x.TienSan ?? 0m),
                TienDichVu = g.Sum(x => x.TienDichVu ?? 0m),
                TongTien = g.Sum(x => x.TongTien ?? 0m)
            })
            .ToListAsync(cancellationToken);

        return dong.Select(x => new DuLieuDoanhThuThang(x.Year, x.Month,
            x.SoDon, x.TienSan, x.TienDichVu, x.TongTien)).ToList();
    }

    public async Task<IReadOnlyList<DoanhThuTheoSan>> LayDoanhThuTheoSanAsync(
        DateTime tu, DateTime den, CancellationToken cancellationToken)
    {
        var dong = await DonHoanThanh(tu, den)
            .GroupBy(x => new { x.MaSan, x.SanTheThao.TenSan })
            .OrderByDescending(g => g.Sum(x => x.TongTien ?? 0m))
            .Select(g => new
            {
                g.Key.MaSan,
                g.Key.TenSan,
                SoDon = g.Count(),
                TongTien = g.Sum(x => x.TongTien ?? 0m)
            })
            .Take(SoSanToiDa)
            .ToListAsync(cancellationToken);

        return dong.Select(x => new DoanhThuTheoSan(x.MaSan, x.TenSan, x.SoDon, x.TongTien)).ToList();
    }

    public async Task<IReadOnlyList<DichVuBanChay>> LayTopDichVuAsync(
        DateTime tu, DateTime den, CancellationToken cancellationToken)
    {
        var dong = await db.ChiTietDatSans.AsNoTracking()
            .Where(c => c.DatSan.TrangThai == TrangThaiDatSan.HoanThanh
                        && c.DatSan.NgayHoanThanh >= tu && c.DatSan.NgayHoanThanh < den)
            .GroupBy(c => new { c.MaDichVu, c.DichVu.TenDichVu })
            .OrderByDescending(g => g.Sum(c => c.SoLuong))
            .ThenByDescending(g => g.Sum(c => c.SoLuong * c.DonGia))
            .Select(g => new
            {
                g.Key.MaDichVu,
                g.Key.TenDichVu,
                SoLuong = g.Sum(c => c.SoLuong),
                DoanhThu = g.Sum(c => c.SoLuong * c.DonGia)
            })
            .Take(SoDichVuToiDa)
            .ToListAsync(cancellationToken);

        return dong.Select(x => new DichVuBanChay(x.MaDichVu, x.TenDichVu, x.SoLuong, x.DoanhThu)).ToList();
    }

    private IQueryable<DatSan> DonHoanThanh(DateTime tu, DateTime den) =>
        db.DatSans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiDatSan.HoanThanh
                        && x.NgayHoanThanh >= tu && x.NgayHoanThanh < den);
}

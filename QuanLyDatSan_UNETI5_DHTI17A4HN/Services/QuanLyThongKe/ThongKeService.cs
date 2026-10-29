// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: thống kê. Doanh thu chỉ tính đơn HoanThanh theo NgayHoanThanh; số đơn theo trạng thái tính theo NgayDat.
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;

public class ThongKeService(ApplicationDbContext db) : IThongKeService
{
    private const int SoNgayToiDaTheoNgay = 62;
    private const int SoNgayToiDa = 366;
    private const int SoSanToiDa = 10;
    private const int SoDichVuToiDa = 5;

    public string? KiemTraBoLoc(BoLocThongKe boLoc)
    {
        if (boLoc.TuNgay > boLoc.DenNgay) return "Từ ngày không được sau đến ngày.";
        var soNgay = boLoc.DenNgay.DayNumber - boLoc.TuNgay.DayNumber + 1;
        if (boLoc.CheDo == CheDoThongKe.Ngay && soNgay > SoNgayToiDaTheoNgay)
            return $"Thống kê theo ngày tối đa {SoNgayToiDaTheoNgay} ngày.";
        if (soNgay > SoNgayToiDa) return $"Khoảng thống kê tối đa {SoNgayToiDa} ngày.";
        return null;
    }

    public async Task<KetQuaThongKe> LayAsync(BoLocThongKe boLoc, CancellationToken cancellationToken)
    {
        var tu = boLoc.TuNgay.ToDateTime(TimeOnly.MinValue);
        var den = boLoc.DenNgay.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var donHoanThanh = db.DatSans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiDatSan.HoanThanh
                        && x.NgayHoanThanh >= tu && x.NgayHoanThanh < den);

        var soDonHoanThanh = await donHoanThanh.CountAsync(cancellationToken);
        var tienSan = await donHoanThanh.SumAsync(x => x.TienSan ?? 0m, cancellationToken);
        var tienDichVu = await donHoanThanh.SumAsync(x => x.TienDichVu ?? 0m, cancellationToken);
        var tongDoanhThu = await donHoanThanh.SumAsync(x => x.TongTien ?? 0m, cancellationToken);

        // Số đơn theo trạng thái: tính theo ngày tạo đơn (NgayDat).
        var dem = await db.DatSans.AsNoTracking()
            .Where(x => x.NgayDat >= tu && x.NgayDat < den)
            .GroupBy(x => x.TrangThai)
            .Select(g => new { TrangThai = g.Key, SoDon = g.Count() })
            .ToListAsync(cancellationToken);
        var soDonTrangThai = Enum.GetValues<TrangThaiDatSan>()
            .Select(t => new SoDonTheoTrangThai(t, dem.FirstOrDefault(x => x.TrangThai == t)?.SoDon ?? 0))
            .ToList();

        var theoKy = boLoc.CheDo == CheDoThongKe.Ngay
            ? await LayTheoNgayAsync(donHoanThanh, boLoc, cancellationToken)
            : await LayTheoThangAsync(donHoanThanh, boLoc, cancellationToken);

        var san = await donHoanThanh
            .GroupBy(x => new { x.MaSan, x.SanTheThao.TenSan })
            .OrderByDescending(g => g.Sum(x => x.TongTien ?? 0m))
            .Select(g => new { g.Key.MaSan, g.Key.TenSan, SoDon = g.Count(), TongTien = g.Sum(x => x.TongTien ?? 0m) })
            .Take(SoSanToiDa)
            .ToListAsync(cancellationToken);
        var theoSan = san.Select(x => new DoanhThuTheoSan(x.MaSan, x.TenSan, x.SoDon, x.TongTien)).ToList();

        var dichVu = await db.ChiTietDatSans.AsNoTracking()
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
        var topDichVu = dichVu.Select(x => new DichVuBanChay(x.MaDichVu, x.TenDichVu, x.SoLuong, x.DoanhThu)).ToList();

        return new KetQuaThongKe(boLoc, soDonHoanThanh, tienSan, tienDichVu, tongDoanhThu,
            soDonTrangThai, theoKy, theoSan, topDichVu);
    }

    // Ngày không có đơn vẫn hiện dòng 0 để biểu đồ liền mạch.
    private static async Task<List<DoanhThuTheoKy>> LayTheoNgayAsync(IQueryable<Models.DatSan> donHoanThanh,
        BoLocThongKe boLoc, CancellationToken cancellationToken)
    {
        var dong = await donHoanThanh
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
        var theoNgay = dong.ToDictionary(x => DateOnly.FromDateTime(x.Ngay));

        var ketQua = new List<DoanhThuTheoKy>();
        for (var ngay = boLoc.TuNgay; ngay <= boLoc.DenNgay; ngay = ngay.AddDays(1))
        {
            theoNgay.TryGetValue(ngay, out var d);
            ketQua.Add(new DoanhThuTheoKy(ngay.ToString("dd/MM", CultureInfo.InvariantCulture),
                d?.SoDon ?? 0, d?.TienSan ?? 0m, d?.TienDichVu ?? 0m, d?.TongTien ?? 0m));
        }
        return ketQua;
    }

    private static async Task<List<DoanhThuTheoKy>> LayTheoThangAsync(IQueryable<Models.DatSan> donHoanThanh,
        BoLocThongKe boLoc, CancellationToken cancellationToken)
    {
        var dong = await donHoanThanh
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
        var theoThang = dong.ToDictionary(x => (x.Year, x.Month));

        var ketQua = new List<DoanhThuTheoKy>();
        for (var thang = new DateOnly(boLoc.TuNgay.Year, boLoc.TuNgay.Month, 1); thang <= boLoc.DenNgay;
             thang = thang.AddMonths(1))
        {
            theoThang.TryGetValue((thang.Year, thang.Month), out var d);
            ketQua.Add(new DoanhThuTheoKy(thang.ToString("MM/yyyy", CultureInfo.InvariantCulture),
                d?.SoDon ?? 0, d?.TienSan ?? 0m, d?.TienDichVu ?? 0m, d?.TongTien ?? 0m));
        }
        return ketQua;
    }
} 
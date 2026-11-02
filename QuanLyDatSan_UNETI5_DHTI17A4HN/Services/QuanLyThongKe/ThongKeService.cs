// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: thống kê. Doanh thu chỉ tính đơn HoanThanh theo NgayHoanThanh; số đơn theo trạng thái tính theo NgayDat.
using System.Globalization;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyThongKe;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;

public class ThongKeService(IThongKeRepository repository) : IThongKeService
{
    private const int SoNgayToiDaTheoNgay = 62;
    private const int SoNgayToiDa = 366;

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

        var tongQuan = await repository.LayTongQuanAsync(tu, den, cancellationToken);
        var dem = await repository.LaySoDonTheoTrangThaiAsync(tu, den, cancellationToken);
        var soDonTrangThai = Enum.GetValues<TrangThaiDatSan>()
            .Select(t => new SoDonTheoTrangThai(t, dem.FirstOrDefault(x => x.TrangThai == t)?.SoDon ?? 0))
            .ToList();

        var theoKy = boLoc.CheDo == CheDoThongKe.Ngay
            ? await LayTheoNgayAsync(boLoc, cancellationToken)
            : await LayTheoThangAsync(boLoc, cancellationToken);

        var theoSan = (await repository.LayDoanhThuTheoSanAsync(tu, den, cancellationToken)).ToList();
        var topDichVu = (await repository.LayTopDichVuAsync(tu, den, cancellationToken)).ToList();

        return new KetQuaThongKe(boLoc, tongQuan.SoDonHoanThanh, tongQuan.TienSan, tongQuan.TienDichVu, tongQuan.TongDoanhThu,
            soDonTrangThai, theoKy, theoSan, topDichVu);
    }

    // Ngày không có đơn vẫn hiện dòng 0 để biểu đồ liền mạch.
    private async Task<List<DoanhThuTheoKy>> LayTheoNgayAsync(BoLocThongKe boLoc,
        CancellationToken cancellationToken)
    {
        var tu = boLoc.TuNgay.ToDateTime(TimeOnly.MinValue);
        var den = boLoc.DenNgay.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var dong = await repository.LayDoanhThuTheoNgayAsync(tu, den, cancellationToken);
        var theoNgay = dong.ToDictionary(x => x.Ngay);

        var ketQua = new List<DoanhThuTheoKy>();
        for (var ngay = boLoc.TuNgay; ngay <= boLoc.DenNgay; ngay = ngay.AddDays(1))
        {
            theoNgay.TryGetValue(ngay, out var d);
            ketQua.Add(new DoanhThuTheoKy(ngay.ToString("dd/MM", CultureInfo.InvariantCulture),
                d?.SoDon ?? 0, d?.TienSan ?? 0m, d?.TienDichVu ?? 0m, d?.TongTien ?? 0m));
        }
        return ketQua;
    }

    private async Task<List<DoanhThuTheoKy>> LayTheoThangAsync(BoLocThongKe boLoc,
        CancellationToken cancellationToken)
    {
        var tu = boLoc.TuNgay.ToDateTime(TimeOnly.MinValue);
        var den = boLoc.DenNgay.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var dong = await repository.LayDoanhThuTheoThangAsync(tu, den, cancellationToken);
        var theoThang = dong.ToDictionary(x => (x.Nam, x.Thang));

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
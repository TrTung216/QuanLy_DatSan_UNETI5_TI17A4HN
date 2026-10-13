// Phan Giang Tâm - 23103100196. M2: Bảng giá và tính tiền theo khung giờ.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public static class GiaThueSan
{
    public static (decimal Thuong, decimal CaoDiem, decimal CuoiTuan) GiaGoiY(string tenLoai, decimal giaCu)
    {
        if (tenLoai.Contains("Bóng đá", StringComparison.OrdinalIgnoreCase)) return (200000m, 350000m, 450000m);
        if (tenLoai.Contains("Cầu lông", StringComparison.OrdinalIgnoreCase)) return (80000m, 150000m, 200000m);
        if (tenLoai.Contains("Bóng rổ", StringComparison.OrdinalIgnoreCase)) return (200000m, 300000m, 350000m);
        if (tenLoai.Contains("Quần vợt", StringComparison.OrdinalIgnoreCase)) return (120000m, 180000m, 200000m);
        return (giaCu, giaCu, giaCu);
    }

    public static decimal TinhTien(SanTheThao san, DateOnly ngay, TimeOnly batDau, TimeOnly ketThuc)
    {
        if (ketThuc <= batDau || batDau < san.GioMoCua || ketThuc > san.GioDongCua)
            throw new ArgumentException("Giờ thuê phải trong giờ phục vụ và kết thúc sau giờ bắt đầu trong cùng ngày.");

        var soGio = (decimal)(ketThuc.ToTimeSpan() - batDau.ToTimeSpan()).Ticks / TimeSpan.TicksPerHour;
        if (ngay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return decimal.Round(soGio * (san.DonGiaCuoiTuan ?? san.DonGia), 0, MidpointRounding.AwayFromZero);

        // Chỉ đoạn nằm trong 18h–21h dùng giá cao điểm.
        var dauCaoDiem = Math.Max(batDau.Ticks, new TimeOnly(18, 0).Ticks);
        var cuoiCaoDiem = Math.Min(ketThuc.Ticks, new TimeOnly(21, 0).Ticks);
        var gioCaoDiem = (decimal)Math.Max(0, cuoiCaoDiem - dauCaoDiem) / TimeSpan.TicksPerHour;
        return decimal.Round((soGio - gioCaoDiem) * san.DonGia
            + gioCaoDiem * (san.DonGiaCaoDiem ?? san.DonGia), 0, MidpointRounding.AwayFromZero);
    }
}

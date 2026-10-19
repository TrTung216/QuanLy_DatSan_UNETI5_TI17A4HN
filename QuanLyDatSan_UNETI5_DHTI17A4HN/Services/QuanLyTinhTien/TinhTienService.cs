// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: tính tiền đơn đặt sân theo mục 5 quy ước (số phút / 60 x đơn giá, làm tròn đến đồng).
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

public class TinhTienService : ITinhTienService
{
    public decimal TinhTienSan(decimal donGia, DateTime batDau, DateTime ketThuc)
    {
        if (donGia < 0) throw new ArgumentOutOfRangeException(nameof(donGia), "Đơn giá không được âm.");
        if (ketThuc <= batDau) throw new ArgumentException("Giờ kết thúc phải sau giờ bắt đầu.", nameof(ketThuc));

        decimal soPhut = (ketThuc - batDau).Ticks / TimeSpan.TicksPerMinute;
        return Math.Round(soPhut * donGia / 60m, 0, MidpointRounding.AwayFromZero);
    }

    public decimal TinhTienDichVu(IEnumerable<DongDichVu> cacDong)
    {
        decimal tong = 0m;
        foreach (var dong in cacDong)
        {
            if (dong.SoLuong <= 0) throw new ArgumentOutOfRangeException(nameof(cacDong), "Số lượng phải lớn hơn 0.");
            if (dong.DonGia < 0) throw new ArgumentOutOfRangeException(nameof(cacDong), "Đơn giá không được âm.");
            tong += dong.SoLuong * dong.DonGia;
        }
        return tong;
    }

    public decimal TinhTongTien(decimal tienSan, decimal tienDichVu) => tienSan + tienDichVu;
}
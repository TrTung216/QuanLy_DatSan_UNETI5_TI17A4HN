// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: tính tiền đơn đặt sân theo mục 5 quy ước (số phút / 60 x đơn giá, làm tròn đến đồng).
// M1: Trần Trọng Tùng; MSSV: 23103100202. Gom công thức cọc/còn phải thu; từ chối dữ liệu tiền sai.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. Công thức cọc 50% chuyển từ ChinhSachCocToiThieu, giữ cách làm tròn.
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

    public decimal TinhTienCocYeuCau(decimal tienSan)
    {
        KiemTraTien(tienSan, nameof(tienSan));
        return Math.Round(tienSan * 0.5m, 0, MidpointRounding.AwayFromZero);
    }

    public decimal TinhConPhaiThu(decimal tongTien, decimal tienCoc)
    {
        KiemTraTien(tongTien, nameof(tongTien));
        KiemTraTien(tienCoc, nameof(tienCoc));
        if (tienCoc > tongTien)
            throw new ArgumentException("Tiền cọc không được vượt tổng tiền.", nameof(tienCoc));
        return tongTien - tienCoc;
    }

    private static void KiemTraTien(decimal tien, string thamSo)
    {
        if (tien < 0m || tien > 9999999999999999.99m)
            throw new ArgumentOutOfRangeException(thamSo, "Tiền phải không âm và trong giới hạn lưu trữ.");
    }
}

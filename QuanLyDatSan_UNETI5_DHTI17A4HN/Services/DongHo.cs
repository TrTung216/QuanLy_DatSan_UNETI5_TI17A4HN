// M3: Nguyễn Văn Quý; MSSV: [MSSV]. Nội dung: đồng hồ chung theo giờ Việt Nam cho các module.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public interface IDongHo
{
    DateTime BayGio { get; }
    DateOnly HomNay { get; }
}

public class DongHoHeThong : IDongHo
{
    private static readonly TimeZoneInfo MuiGio = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public DateTime BayGio => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MuiGio);
    public DateOnly HomNay => DateOnly.FromDateTime(BayGio);
}

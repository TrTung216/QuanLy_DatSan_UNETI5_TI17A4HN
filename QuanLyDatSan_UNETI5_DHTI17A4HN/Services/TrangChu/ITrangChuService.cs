// M1: Trần Trọng Tùng; MSSV: 23103100202. Hợp đồng đọc danh mục sân công khai trên trang chủ.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;

public interface ITrangChuService
{
    Task<IReadOnlyList<NhomSanTrangChu>> LaySanTheoLoaiAsync(CancellationToken cancellationToken);
}

public record SanTrangChu(int MaSan, string TenSan, string DiaChi, decimal DonGia,
    TimeOnly GioMoCua, TimeOnly GioDongCua);
public record NhomSanTrangChu(int MaLoaiSan, string TenLoai, IReadOnlyList<SanTrangChu> San);

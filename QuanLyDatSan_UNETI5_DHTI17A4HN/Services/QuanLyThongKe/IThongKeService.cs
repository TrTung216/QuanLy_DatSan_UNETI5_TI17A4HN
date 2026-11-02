// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng thống kê doanh thu, số đơn và dịch vụ bán chạy (chỉ đọc dữ liệu).
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;

public enum CheDoThongKe { Ngay = 0, Thang = 1 }

public record BoLocThongKe(DateOnly TuNgay, DateOnly DenNgay, CheDoThongKe CheDo);
public record SoDonTheoTrangThai(TrangThaiDatSan TrangThai, int SoDon);
public record DoanhThuTheoKy(string Nhan, int SoDon, decimal TienSan, decimal TienDichVu, decimal TongTien);
public record DoanhThuTheoSan(int MaSan, string TenSan, int SoDon, decimal TongTien);
public record DichVuBanChay(int MaDichVu, string TenDichVu, int SoLuong, decimal DoanhThu);

public record KetQuaThongKe(
    BoLocThongKe BoLoc,
    int SoDonHoanThanh,
    decimal TienSan,
    decimal TienDichVu,
    decimal TongDoanhThu,
    List<SoDonTheoTrangThai> SoDonTrangThai,
    List<DoanhThuTheoKy> TheoKy,
    List<DoanhThuTheoSan> TheoSan,
    List<DichVuBanChay> TopDichVu);

public interface IThongKeService
{
    // Trả về thông báo lỗi tiếng Việt, hoặc null nếu bộ lọc hợp lệ.
    string? KiemTraBoLoc(BoLocThongKe boLoc);

    Task<KetQuaThongKe> LayAsync(BoLocThongKe boLoc, CancellationToken cancellationToken);
} 
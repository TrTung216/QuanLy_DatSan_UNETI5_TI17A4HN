// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng truy vấn số liệu thống kê.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyThongKe;

public interface IThongKeRepository
{
    Task<TongQuanThongKe> LayTongQuanAsync(DateTime tu, DateTime den, CancellationToken cancellationToken);
    Task<IReadOnlyList<SoDonTheoTrangThai>> LaySoDonTheoTrangThaiAsync(DateTime tu, DateTime den, CancellationToken cancellationToken);
    Task<IReadOnlyList<DuLieuDoanhThuNgay>> LayDoanhThuTheoNgayAsync(DateTime tu, DateTime den, CancellationToken cancellationToken);
    Task<IReadOnlyList<DuLieuDoanhThuThang>> LayDoanhThuTheoThangAsync(DateTime tu, DateTime den, CancellationToken cancellationToken);
    Task<IReadOnlyList<DoanhThuTheoSan>> LayDoanhThuTheoSanAsync(DateTime tu, DateTime den, CancellationToken cancellationToken);
    Task<IReadOnlyList<DichVuBanChay>> LayTopDichVuAsync(DateTime tu, DateTime den, CancellationToken cancellationToken);
}

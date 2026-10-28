// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.DieuHanh;

public interface IDieuHanhRepository
{
    // Số liệu bảng điều hành: đơn chờ chưa hết giờ sử dụng, khách hoạt động và sân hoạt động.
    // bayGio do service lấy từ đồng hồ ứng dụng để các phép đếm dùng cùng một mốc thời gian.
    Task<SoLieuDieuHanh> LaySoLieuAsync(DateTime bayGio, CancellationToken cancellationToken);
}

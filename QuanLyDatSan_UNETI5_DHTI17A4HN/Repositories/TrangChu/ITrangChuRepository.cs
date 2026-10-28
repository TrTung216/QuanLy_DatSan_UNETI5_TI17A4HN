// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.TrangChu;

public interface ITrangChuRepository
{
    Task<IReadOnlyList<NhomSanTrangChu>> LaySanTheoLoaiAsync(CancellationToken cancellationToken);
}

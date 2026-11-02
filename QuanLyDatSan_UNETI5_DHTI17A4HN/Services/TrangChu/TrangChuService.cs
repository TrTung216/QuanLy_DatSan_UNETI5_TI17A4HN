// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách truy cập dữ liệu, giữ hợp đồng nghiệp vụ.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.TrangChu;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;
public class TrangChuService(ITrangChuRepository repository) : ITrangChuService
{
    public Task<IReadOnlyList<NhomSanTrangChu>> LaySanTheoLoaiAsync(CancellationToken cancellationToken) =>
        repository.LaySanTheoLoaiAsync(cancellationToken);
}

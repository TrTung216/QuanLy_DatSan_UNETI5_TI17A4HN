// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Đọc sân theo loại cho trang chủ, không thay nghiệp vụ M2.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.TrangChu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;

public class TrangChuService(ITrangChuRepository repository) : ITrangChuService
{
    public Task<IReadOnlyList<NhomSanTrangChu>> LaySanTheoLoaiAsync(CancellationToken cancellationToken) =>
        repository.LaySanTheoLoaiAsync(cancellationToken);
}

// M1: Trần Trọng Tùng - 23103100202. Đọc quyền công khai và tham chiếu ảnh của hai bảng.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;

public interface IAnhRepository
{
    Task<ThongTinAnh?> LayAsync(NhomAnh nhom, int id, CancellationToken ct);
    Task<bool> CoThamChieuAsync(string khoa, CancellationToken ct);
}

// Họ và tên: Phan Giang Tâm
// Mã sinh viên: 23103100196
// Nội dung: Dữ liệu danh sách sân và phân trang.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DanhSachSanViewModel
{
    public IReadOnlyList<SanTheThao> DanhSachSan { get; init; } = Array.Empty<SanTheThao>();
    public int TrangHienTai { get; init; } = 1;
    public int TongSoTrang { get; init; } = 1;
    public int TongSoSan { get; init; }
}

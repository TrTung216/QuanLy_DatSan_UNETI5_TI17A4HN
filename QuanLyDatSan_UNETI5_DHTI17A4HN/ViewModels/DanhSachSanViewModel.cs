// Họ và tên: Phan Giang Tâm
// Mã sinh viên: 23103100196
// Nội dung: Dữ liệu danh sách sân và phân trang.

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DanhSachSanViewModel
{
    public IReadOnlyList<SanChiTietViewModel> DanhSachSan { get; init; } = Array.Empty<SanChiTietViewModel>();
    public int TrangHienTai { get; init; } = 1;
    public int TongSoTrang { get; init; } = 1;
    public int TongSoSan { get; init; }
}

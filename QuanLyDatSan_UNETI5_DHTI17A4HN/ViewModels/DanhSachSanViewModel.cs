// Họ và tên: Phan Giang Tâm
// Mã sinh viên: 23103100196
// Nội dung: Dữ liệu danh sách, điều kiện tra cứu và phân trang sân.

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DanhSachSanViewModel : TraCuuSanViewModel
{
    public IReadOnlyList<SanChiTietViewModel> DanhSachSan { get; init; } = Array.Empty<SanChiTietViewModel>();
    public int TrangHienTai { get; init; } = 1;
    public int TongSoTrang { get; init; } = 1;
    public int TongSoSan { get; init; }
    public IReadOnlyList<LoaiSanTraCuuViewModel> CacLoaiSan { get; init; } = Array.Empty<LoaiSanTraCuuViewModel>();
}

public record LoaiSanTraCuuViewModel(int MaLoaiSan, string TenLoai);

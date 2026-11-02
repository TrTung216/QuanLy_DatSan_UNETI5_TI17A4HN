// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Dữ liệu khả dụng và chi tiết đơn để service kiểm tra nghiệp vụ, tính tiền.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;

public sealed class DuLieuKhaDungSan
{
    public int MaSan { get; init; }
    public decimal DonGia { get; init; }
    public bool HoatDong { get; init; }
    public TimeOnly GioMoCua { get; init; }
    public TimeOnly GioDongCua { get; init; }
    public DateOnly? NgayBaoTri { get; init; }
    public bool TrungLich { get; init; }
}

public sealed record DuLieuChiTietDonQuanLy(ChiTietDonQuanLyDto ChiTiet, List<DongDichVu> DichVu);

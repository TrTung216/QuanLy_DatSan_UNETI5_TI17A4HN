// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: dữ liệu hiển thị lịch đặt cá nhân gồm các dòng đơn, bộ lọc trạng thái và thông tin phân trang.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DanhSachDonViewModel
{
    public IReadOnlyList<DonDatDongViewModel> Dong { get; init; } = [];
    public TrangThaiDatSan? TrangThai { get; init; }
    public int Trang { get; init; } = 1;
    public bool ConTrangSau { get; init; }
    public int TongTrang { get; init; } = 1;
    public int TongSo { get; init; }
}

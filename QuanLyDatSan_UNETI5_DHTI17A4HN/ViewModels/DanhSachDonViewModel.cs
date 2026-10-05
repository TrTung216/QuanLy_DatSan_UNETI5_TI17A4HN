// Họ và tên: Phạm Thành Nghĩa
// Mã sinh viên: 23103100308
// Nội dung thực hiện: M4 - danh sách đơn đã đặt
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DanhSachDonViewModel
{
    public IReadOnlyList<DonDatSan> DanhSach { get; init; } = [];
    public int Trang { get; init; } = 1;
    public bool ConTrangSau { get; init; }
}

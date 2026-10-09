// Họ và tên: Phạm Thành Nghĩa
// Mã sinh viên: 23103100308
// Nội dung thực hiện: M4 - danh sách đơn đã đặt
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DanhSachDonViewModel
{
    public IReadOnlyList<DatSan> DanhSach { get; init; } = [];
    public IReadOnlyList<DonDatDongViewModel> Dong { get; init; } = [];
    public TrangThaiDatSan? TrangThai { get; init; }
    public int Trang { get; init; } = 1;
    public bool ConTrangSau { get; init; }
    public int TongTrang { get; init; } = 1;
    public int TongSo { get; init; }
}

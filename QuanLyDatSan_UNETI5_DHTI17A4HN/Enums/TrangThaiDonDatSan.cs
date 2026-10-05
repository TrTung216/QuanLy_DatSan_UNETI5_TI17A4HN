// Họ và tên: Phạm Thành Nghĩa
// Mã sinh viên: 23103100308
// Nội dung thực hiện: M4 - trạng thái đơn đặt sân
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

public enum TrangThaiDonDatSan
{
    [Display(Name = "Chờ xác nhận")] ChoXacNhan = 0,
    [Display(Name = "Đã xác nhận")] DaXacNhan = 1,
    [Display(Name = "Đang sử dụng")] DangSuDung = 2,
    [Display(Name = "Hoàn thành")] HoanThanh = 3,
    [Display(Name = "Đã hủy")] DaHuy = 4
}

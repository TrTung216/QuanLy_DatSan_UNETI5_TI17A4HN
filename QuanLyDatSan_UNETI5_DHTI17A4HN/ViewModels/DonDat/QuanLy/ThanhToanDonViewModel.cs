// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-04 - đầu vào ghi tổng cọc và xác nhận thu đủ; không nhận giá chốt từ trình duyệt.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Không kế thừa lựa chọn cọc của form xác nhận.
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.QuanLy;

public sealed class GhiCocViewModel : XuLyDonViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tổng cọc đã nhận.")]
    [Range(typeof(decimal), "1", "9999999999999999", ErrorMessage = "Tổng cọc phải lớn hơn 0 và trong giới hạn lưu trữ.")]
    public decimal? TongCoc { get; set; }
}

public sealed class HoanThanhDonViewModel : XuLyDonViewModel
{
    public bool DaThuDu { get; set; }
}

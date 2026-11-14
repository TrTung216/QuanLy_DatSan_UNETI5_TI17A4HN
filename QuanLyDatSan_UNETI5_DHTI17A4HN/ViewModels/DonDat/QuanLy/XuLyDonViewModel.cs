// M1: Trần Trọng Tùng; MSSV: 23103100202. Đầu vào chung cho các thao tác đơn của quản lý.
// Tách các trường/validation chung từ ViewModel M4; lựa chọn cọc chỉ thuộc thao tác xác nhận.
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.QuanLy;

public abstract class XuLyDonViewModel
{
    [Range(1, int.MaxValue)]
    public int MaDon { get; set; }

    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string PhienBan { get; set; } = string.Empty;

    public string? QuayLai { get; set; }
}

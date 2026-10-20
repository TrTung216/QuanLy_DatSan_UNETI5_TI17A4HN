// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-03 - dữ liệu POST xác nhận đơn gồm mã đơn, RowVersion dạng Base64 và đường dẫn quay lại.
// Controller kiểm tra phiên bản 8 byte và chỉ chấp nhận đường dẫn nội bộ.

using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class XacNhanDonViewModel
{
    [Range(1, int.MaxValue)]
    public int MaDon { get; set; }

    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string PhienBan { get; set; } = string.Empty;

    public string? QuayLai { get; set; }
}

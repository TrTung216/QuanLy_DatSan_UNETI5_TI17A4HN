// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-03 - dữ liệu POST xác nhận đơn gồm mã đơn, RowVersion dạng Base64 và đường dẫn quay lại.
// Controller kiểm tra phiên bản 8 byte và chỉ chấp nhận đường dẫn nội bộ.

// M3: Nguyễn Văn Quý; MSSV: 23103100181. Bổ sung lựa chọn cọc khi xác nhận đơn (đã nhận đủ / chưa nhận đủ).
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public enum TinhTrangCocKhiXacNhan { DaNhanDu, ChuaNhanDu }

public class XacNhanDonViewModel
{
    [Range(1, int.MaxValue)]
    public int MaDon { get; set; }

    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string PhienBan { get; set; } = string.Empty;

    public string? QuayLai { get; set; }

    // null: không gửi lựa chọn cọc (giữ hành vi xác nhận cũ); form quản lý luôn gửi giá trị này.
    public TinhTrangCocKhiXacNhan? TinhTrangCoc { get; set; }
}

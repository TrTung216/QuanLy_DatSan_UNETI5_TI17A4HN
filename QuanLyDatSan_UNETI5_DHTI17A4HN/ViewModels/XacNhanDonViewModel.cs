// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: khai báo dữ liệu và validation cho form xác nhận đơn: mã đơn, phiên bản, ghi chú và thông tin hiển thị.
// Các trường hiển thị dùng BindNever để không nhận giá trị từ form gửi lên.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class XacNhanDonViewModel
{
    [Range(1, int.MaxValue)]
    public int MaDon { get; set; }

    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string PhienBan { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    [Display(Name = "Ghi chú xác nhận (không bắt buộc)")]
    public string? GhiChu { get; set; }

    // Chỉ lấy từ database, không tin dữ liệu hiển thị gửi từ trình duyệt.
    [BindNever] public string TenSan { get; set; } = string.Empty;
    [BindNever] public string TenKhachHang { get; set; } = string.Empty;
    [BindNever] public DateTime BatDau { get; set; }
    [BindNever] public DateTime KetThuc { get; set; }
    [BindNever] public TrangThaiDatSan TrangThai { get; set; }
}

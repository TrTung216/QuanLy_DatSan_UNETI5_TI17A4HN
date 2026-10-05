// M3: Nguyễn Văn Quý; MSSV: [MSSV]. Nội dung: form đăng ký tài khoản khách hàng.
using System.ComponentModel.DataAnnotations;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DangKyKhachViewModel : ThongTinKhachViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Mật khẩu từ 8 đến 128 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string XacNhanMatKhau { get; set; } = string.Empty;
}

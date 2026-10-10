// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ ViewModel quản lý tài khoản.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class TaiKhoanThongTinViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
    [EnumDataType(typeof(VaiTro), ErrorMessage = "Vai trò không hợp lệ.")]
    [Display(Name = "Vai trò")]
    public VaiTro? VaiTro { get; set; }
}

public class TaiKhoanTaoViewModel : TaiKhoanThongTinViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự.")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Mật khẩu phải có từ 12 đến 128 ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu ban đầu")]
    public string MatKhau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
    [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string XacNhanMatKhau { get; set; } = string.Empty;
}

public class TaiKhoanSuaViewModel : TaiKhoanThongTinViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [EnumDataType(typeof(TrangThaiTaiKhoan), ErrorMessage = "Trạng thái không hợp lệ.")]
    [Display(Name = "Trạng thái")]
    public TrangThaiTaiKhoan? TrangThai { get; set; }
}

// Không đưa entity hoặc giá trị băm mật khẩu ra danh sách/View.
public class TaiKhoanDongViewModel
{
    public int MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public VaiTro VaiTro { get; set; }
    public TrangThaiTaiKhoan TrangThai { get; set; }
}

public class TaiKhoanDanhSachViewModel
{
    public List<TaiKhoanDongViewModel> DanhSach { get; set; } = [];
    public string? TuKhoa { get; set; }
    public VaiTro? VaiTro { get; set; }
    public TrangThaiTaiKhoan? TrangThai { get; set; }
    public int Trang { get; set; }
    public int TongTrang { get; set; }
    public int TongSo { get; set; }
}

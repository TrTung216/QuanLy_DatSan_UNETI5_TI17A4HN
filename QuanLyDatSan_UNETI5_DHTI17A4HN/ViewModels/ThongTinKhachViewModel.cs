// M3: Nguyễn Văn Quý; MSSV: [MSSV]. Nội dung: các trường hồ sơ khách hàng được phép nhận từ form.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class ThongTinKhachViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    [Display(Name = "Họ tên")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [StringLength(254, ErrorMessage = "Email tối đa 254 ký tự.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ, ví dụ 0912345678.")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = string.Empty;

    [Display(Name = "Ngày sinh")]
    public DateOnly? NgaySinh { get; set; }

    [EnumDataType(typeof(GioiTinh), ErrorMessage = "Giới tính không hợp lệ.")]
    [Display(Name = "Giới tính")]
    public GioiTinh GioiTinh { get; set; } = GioiTinh.KhongKhaiBao;

    [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var dongHo = (IDongHo)validationContext.GetService(typeof(IDongHo))!;
        if (NgaySinh is { } ngaySinh && (ngaySinh > dongHo.HomNay || ngaySinh.Year < 1900))
        {
            yield return new ValidationResult("Ngày sinh không hợp lệ.", [nameof(NgaySinh)]);
        }
    }
}

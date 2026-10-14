// Phan Giang Tâm - 23103100196. M2: Dữ liệu và validation form tra cứu sân.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class TraCuuSanViewModel : IValidatableObject
{
    [StringLength(100, ErrorMessage = "Từ khóa không được quá 100 ký tự.")]
    [Display(Name = "Từ khóa")]
    public string? TuKhoa { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Loại sân không hợp lệ.")]
    [Display(Name = "Loại sân")]
    public int? MaLoaiSan { get; set; }

    [EnumDataType(typeof(TrangThaiSan), ErrorMessage = "Trạng thái không hợp lệ.")]
    [Display(Name = "Trạng thái")]
    public TrangThaiSan? TrangThai { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá từ phải không âm và nằm trong giới hạn cho phép.")]
    [Display(Name = "Giá từ (đồng/giờ)")]
    public decimal? GiaTu { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá đến phải không âm và nằm trong giới hạn cho phép.")]
    [Display(Name = "Giá đến (đồng/giờ)")]
    public decimal? GiaDen { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn cách sắp xếp.")]
    [RegularExpression("^(ten_tang|ten_giam|gia_tang|gia_giam|gio_tang|gio_giam)$", ErrorMessage = "Cách sắp xếp không hợp lệ.")]
    [Display(Name = "Sắp xếp")]
    public string SapXep { get; set; } = "ten_tang";

    public int Trang { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GiaTu.HasValue && GiaTu != decimal.Truncate(GiaTu.Value))
            yield return new("Giá từ phải là số nguyên theo đồng.", [nameof(GiaTu)]);
        if (GiaDen.HasValue && GiaDen != decimal.Truncate(GiaDen.Value))
            yield return new("Giá đến phải là số nguyên theo đồng.", [nameof(GiaDen)]);
        if (GiaTu.HasValue && GiaDen.HasValue && GiaTu > GiaDen)
            yield return new("Giá đến phải lớn hơn hoặc bằng giá từ.", [nameof(GiaDen)]);
    }
}

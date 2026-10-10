// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ form và dữ liệu hiển thị quản lý loại sân.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class LoaiSanFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập tên loại sân.")]
    [StringLength(100, ErrorMessage = "Tên loại sân tối đa 100 ký tự.")]
    [Display(Name = "Tên loại sân")]
    public string TenLoai { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số người tối đa.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số người tối đa phải là số nguyên lớn hơn 0.")]
    [Display(Name = "Số người tối đa")]
    public int? SoNguoiToiDa { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn giá theo giờ.")]
    [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Đơn giá phải từ 0 đến 9.999.999.999.999.999 đồng.")]
    [Display(Name = "Đơn giá mặc định (₫/giờ)")]
    public decimal? DonGiaTheoGio { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    [EnumDataType(typeof(TrangThaiLoaiSan), ErrorMessage = "Trạng thái không hợp lệ.")]
    [Display(Name = "Trạng thái")]
    public TrangThaiLoaiSan? TrangThai { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DonGiaTheoGio is { } gia && gia != decimal.Truncate(gia))
            yield return new ValidationResult("Đơn giá phải là số đồng nguyên.", [nameof(DonGiaTheoGio)]);
    }
}

public class LoaiSanChiTietViewModel
{
    public int MaLoaiSan { get; set; }
    public string TenLoai { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int SoNguoiToiDa { get; set; }
    public decimal DonGiaTheoGio { get; set; }
    public TrangThaiLoaiSan TrangThai { get; set; }
    public int SoSan { get; set; }
}

public class LoaiSanDanhSachViewModel
{
    public List<LoaiSanChiTietViewModel> DanhSach { get; set; } = [];
    public string? TuKhoa { get; set; }
    public TrangThaiLoaiSan? TrangThai { get; set; }
    public string SapXep { get; set; } = "ten_tang";
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
    public int TongSo { get; set; }
}

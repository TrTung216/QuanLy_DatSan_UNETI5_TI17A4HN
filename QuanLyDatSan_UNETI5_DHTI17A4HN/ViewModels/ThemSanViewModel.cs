// Họ tên: Phan Giang Tâm; MSSV: 23103100196.
// Nội dung: Dữ liệu nhập và kiểm tra form thêm sân.
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class ThemSanViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập tên sân.")]
    [StringLength(100, ErrorMessage = "Tên sân không được quá 100 ký tự.")]
    [Display(Name = "Tên sân")]
    public string TenSan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn loại sân.")]
    [Range(1, int.MaxValue, ErrorMessage = "Loại sân không hợp lệ.")]
    [Display(Name = "Loại sân")]
    public int? MaLoaiSan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(255, ErrorMessage = "Địa chỉ không được quá 255 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Tiện ích không được quá 1000 ký tự.")]
    [Display(Name = "Tiện ích")]
    public string? TienIch { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn giá.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Đơn giá phải không âm và nằm trong giới hạn cho phép.")]
    [Display(Name = "Đơn giá theo giờ (đồng)")]
    public decimal? DonGia { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn trạng thái sân.")]
    [EnumDataType(typeof(TrangThaiSan), ErrorMessage = "Trạng thái sân không hợp lệ.")]
    [Display(Name = "Trạng thái")]
    public TrangThaiSan? TrangThai { get; set; } = TrangThaiSan.HoatDong;

    [Required(ErrorMessage = "Vui lòng nhập giờ mở cửa.")]
    [Display(Name = "Giờ mở cửa")]
    public TimeOnly? GioMoCua { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giờ đóng cửa.")]
    [Display(Name = "Giờ đóng cửa")]
    public TimeOnly? GioDongCua { get; set; }

    [Display(Name = "Ngày bảo trì")]
    public DateOnly? NgayBaoTri { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được quá 1000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [ValidateNever]
    public IReadOnlyList<LuaChonLoaiSan> CacLoaiSan { get; set; } = Array.Empty<LuaChonLoaiSan>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GioMoCua.HasValue && GioDongCua.HasValue && GioDongCua <= GioMoCua)
            yield return new ValidationResult("Giờ đóng cửa phải sau giờ mở cửa trong cùng ngày.", [nameof(GioDongCua)]);

        if (DonGia.HasValue && decimal.Truncate(DonGia.Value) != DonGia.Value)
            yield return new ValidationResult("Đơn giá phải là số nguyên theo đồng, không nhập phần thập phân.", [nameof(DonGia)]);
    }
}

public record LuaChonLoaiSan(int MaLoaiSan, string TenLoai, decimal DonGiaTheoGio);

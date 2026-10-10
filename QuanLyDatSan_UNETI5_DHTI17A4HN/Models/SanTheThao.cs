// Họ và tên: Phan Giang Tâm
// Mã sinh viên: 23103100196
// Nội dung thực hiện: M2 - dữ liệu sân thể thao, ràng buộc cơ bản và liên kết loại sân.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class SanTheThao
{
    [Key]
    public int MaSan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sân.")]
    [StringLength(100, ErrorMessage = "Tên sân không được quá 100 ký tự.")]
    [Display(Name = "Tên sân")]
    public string TenSan { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn loại sân.")]
    [Display(Name = "Loại sân")]
    public int MaLoaiSan { get; set; }

    [ForeignKey(nameof(MaLoaiSan))]
    public LoaiSan LoaiSan { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
    [StringLength(255, ErrorMessage = "Địa chỉ không được quá 255 ký tự.")]
    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Tiện ích không được quá 1000 ký tự.")]
    [Display(Name = "Tiện ích")]
    public string? TienIch { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Đơn giá phải không âm và nằm trong giới hạn cho phép.")]
    [Display(Name = "Đơn giá theo giờ")]
    public decimal DonGia { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? DonGiaCaoDiem { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? DonGiaCuoiTuan { get; set; }

    [EnumDataType(typeof(TrangThaiSan), ErrorMessage = "Trạng thái sân không hợp lệ.")]
    [Display(Name = "Trạng thái")]
    public TrangThaiSan TrangThai { get; set; } = TrangThaiSan.HoatDong;

    [Column(TypeName = "time(0)")]
    [Display(Name = "Giờ mở cửa")]
    public TimeOnly GioMoCua { get; set; }

    [Column(TypeName = "time(0)")]
    [Display(Name = "Giờ đóng cửa")]
    public TimeOnly GioDongCua { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được quá 1000 ký tự.")]
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    [Column(TypeName = "date")]
    [Display(Name = "Ngày bảo trì")]
    public DateOnly? NgayBaoTri { get; set; }
}

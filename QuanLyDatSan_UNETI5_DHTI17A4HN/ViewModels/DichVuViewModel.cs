// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: ViewModel thêm, sửa và danh sách dịch vụ.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DichVuTaoViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
    [StringLength(100, ErrorMessage = "Tên dịch vụ tối đa 100 ký tự.")]
    [Display(Name = "Tên dịch vụ")]
    public string TenDichVu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
    [StringLength(30, ErrorMessage = "Đơn vị tính tối đa 30 ký tự.")]
    [Display(Name = "Đơn vị tính")]
    public string DonViTinh { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập đơn giá.")]
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "Đơn giá phải từ 0 đến 1.000.000.000.")]
    [Display(Name = "Đơn giá (₫)")]
    public decimal DonGia { get; set; }

    [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }
}

public class DichVuSuaViewModel : DichVuTaoViewModel
{
    public int MaDichVu { get; set; }
}

public class DichVuDanhSachViewModel
{
    public List<DichVu> DanhSach { get; set; } = new();
    public string? TuKhoa { get; set; }
    public TrangThaiDichVu? TrangThai { get; set; }
    public string? SapXep { get; set; }
    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; } = 1;
}
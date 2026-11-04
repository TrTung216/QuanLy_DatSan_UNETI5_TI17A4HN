// M1: Trần Trọng Tùng - 23103100202. IFormFile chỉ ở biên MVC.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public sealed class CapNhatAnhViewModel
{
    [Required, EnumDataType(typeof(ThaoTacAnh))]
    public ThaoTacAnh? ThaoTac { get; set; }
    [StringLength(255)]
    public string? AnhCu { get; set; }
    public IFormFile? Tep { get; set; }
}

// Không đưa khóa file vào URL hiển thị; endpoint tra DB theo ID và kiểm quyền ở server.
public sealed record AnhDaiDienViewModel(NhomAnh Nhom, int Id, string Ten, bool CoAnh,
    bool ChiTiet = false, bool TaiCham = true);

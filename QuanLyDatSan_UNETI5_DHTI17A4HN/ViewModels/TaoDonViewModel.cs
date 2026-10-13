// M3: Nguyễn Văn Quý; MSSV: 23103100181. các trường được phép nhận khi khách tạo đơn đặt sân (không nhận giá, cọc, trạng thái).
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public record SanChonViewModel(int MaSan, string TenSan, string TenLoai, decimal DonGia, TimeOnly GioMoCua, TimeOnly GioDongCua);

public class TaoDonViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn sân.")]
    [Display(Name = "Sân")]
    public int? MaSan { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày sử dụng.")]
    [Display(Name = "Ngày sử dụng")]
    public DateOnly? NgaySuDung { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu.")]
    [Display(Name = "Giờ bắt đầu")]
    public TimeOnly? GioBatDau { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc.")]
    [Display(Name = "Giờ kết thúc")]
    public TimeOnly? GioKetThuc { get; set; }

    [BindNever]
    public List<SanChonViewModel> DanhSachSan { get; set; } = [];
}

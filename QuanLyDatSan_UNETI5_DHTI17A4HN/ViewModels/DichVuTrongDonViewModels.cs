// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: dữ liệu form thêm / sửa số lượng dịch vụ trong đơn.
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class ThemDichVuTrongDonViewModel
{
    public int MaDatSan { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn dịch vụ.")]
    [Display(Name = "Dịch vụ")]
    public int MaDichVu { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    [Display(Name = "Số lượng")]
    public int SoLuong { get; set; } = 1;
}

public class SuaSoLuongDichVuViewModel
{
    public int MaDatSan { get; set; }
    public int MaChiTiet { get; set; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    [Display(Name = "Số lượng")]
    public int SoLuong { get; set; }
}
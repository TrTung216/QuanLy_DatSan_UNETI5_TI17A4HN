// Họ tên: Phan Giang Tâm; MSSV: 23103100196.
// Nội dung: Dữ liệu form sửa sân, dùng lại kiểm tra dữ liệu của form thêm.
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class SuaSanViewModel : ThemSanViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã sân không hợp lệ.")]
    public int MaSan { get; set; }
}

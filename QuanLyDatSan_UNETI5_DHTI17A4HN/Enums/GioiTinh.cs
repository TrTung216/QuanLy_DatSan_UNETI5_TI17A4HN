// M3: Nguyễn Văn Quý; MSSV: [MSSV]. Nội dung: giới tính khách hàng.
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

public enum GioiTinh
{
    [Display(Name = "Không khai báo")]
    KhongKhaiBao = 0,
    [Display(Name = "Nam")]
    Nam = 1,
    [Display(Name = "Nữ")]
    Nu = 2,
    [Display(Name = "Khác")]
    Khac = 3
}

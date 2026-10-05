// M3: Nguyễn Văn Quý; MSSV: [MSSV]. Nội dung: form xem và sửa hồ sơ cá nhân của khách hàng.
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class HoSoViewModel : ThongTinKhachViewModel
{
    [BindNever]
    public string TenDangNhap { get; set; } = string.Empty;

    [BindNever]
    public DateTime NgayDangKy { get; set; }

    [BindNever]
    public int DiemTichLuy { get; set; }
}

// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Lưu giao dịch cọc mô phỏng và trạng thái đối soát.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public sealed class GiaoDichCoc
{
    public int MaGiaoDich { get; set; }
    public string Nguon { get; set; } = "MoPhong";
    public string TaiKhoanNhan { get; set; } = "";
    public string MaGiaoDichNgoai { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public decimal SoTien { get; set; }
    public DateTime NgayNhan { get; set; }
    public DateTime NgayGhi { get; set; }
    public int MaNguoiGhi { get; set; }
    public TrangThaiGiaoDichCoc TrangThai { get; set; }
    public MaLoiCoc? LoiNgoaiLe { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

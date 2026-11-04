// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Lưu biên nhận và thông tin xác nhận cọc của đơn.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public sealed class BienNhanCoc
{
    public int MaBienNhan { get; set; }
    public int MaDatSan { get; set; }
    public int MaGiaoDich { get; set; }
    public decimal SoTien { get; set; }
    public int MaNguoiXacNhan { get; set; }
    public DateTime NgayXacNhan { get; set; }
    public DateTime NgayNhan { get; set; }
    public string Nguon { get; set; } = "";
    public string TaiKhoanNhan { get; set; } = "";
    public string MaGiaoDichNgoai { get; set; } = "";
    public string NoiDung { get; set; } = "";
}

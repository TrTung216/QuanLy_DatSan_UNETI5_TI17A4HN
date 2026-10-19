// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: dịch vụ đi kèm trong đơn đặt sân; DonGia là bản chụp giá lúc thêm.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class ChiTietDatSan
{
    public int MaChiTiet { get; set; }
    public int MaDatSan { get; set; }
    public DatSan DatSan { get; set; } = null!;
    public int MaDichVu { get; set; }
    public DichVu DichVu { get; set; } = null!;
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
}
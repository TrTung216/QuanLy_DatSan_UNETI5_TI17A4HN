// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: Entity danh mục dịch vụ đi kèm đơn đặt sân.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class DichVu
{
    public int MaDichVu { get; set; }
    public string TenDichVu { get; set; } = string.Empty;
    public string DonViTinh { get; set; } = string.Empty;
    public decimal DonGia { get; set; }
    public string? MoTa { get; set; }
    public TrangThaiDichVu TrangThai { get; set; } = TrangThaiDichVu.HoatDong;
    // Navigation ChiTietDatSans sẽ thêm khi có Entity ChiTietDatSan (bước 3).
}
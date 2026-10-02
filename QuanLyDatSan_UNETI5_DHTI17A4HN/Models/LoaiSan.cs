// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ soạn mã.
// Nội dung: danh mục loại sân và giá mặc định theo giờ.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

public class LoaiSan
{
    public int MaLoaiSan { get; set; }
    public string TenLoai { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int SoNguoiToiDa { get; set; }
    public decimal DonGiaTheoGio { get; set; }
    public TrangThaiLoaiSan TrangThai { get; set; } = TrangThaiLoaiSan.HoatDong;
}

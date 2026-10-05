// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ soạn mã.
// Nội dung: danh mục loại sân và giá mặc định theo giờ.
// M2: Phan Giang Tâm; MSSV: 23103100196.
// Nội dung: bổ sung liên kết một loại sân có nhiều sân thể thao.
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
    public ICollection<SanTheThao> DanhSachSan { get; set; }
    = new List<SanTheThao>();
}

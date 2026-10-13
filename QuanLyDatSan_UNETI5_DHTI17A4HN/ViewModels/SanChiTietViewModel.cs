// Phan Giang Tâm - 23103100196. M2: Dữ liệu hiển thị sân, không mang Entity sang View.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public record SanChiTietViewModel(int MaSan, string TenSan, int MaLoaiSan, string TenLoai,
    string DiaChi, string? TienIch, decimal DonGia, decimal? DonGiaCaoDiem,
    decimal? DonGiaCuoiTuan, TrangThaiSan TrangThai, TimeOnly GioMoCua,
    TimeOnly GioDongCua, DateOnly? NgayBaoTri, string? GhiChu);

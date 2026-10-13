// Phan Giang Tâm - 23103100196. M2: Dữ liệu trao đổi giữa Controller và service sân.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

// Giá nhập do người quản lý thiết lập bảng giá, không phải giá chốt đơn do khách gửi.
public record GhiSan(string TenSan, int MaLoaiSan, string DiaChi, string? TienIch,
    decimal DonGia, decimal DonGiaCaoDiem, decimal DonGiaCuoiTuan, TrangThaiSan TrangThai,
    TimeOnly GioMoCua, TimeOnly GioDongCua, DateOnly? NgayBaoTri, string? GhiChu);

public record SanDoc(int MaSan, string TenSan, int MaLoaiSan, string TenLoai,
    string DiaChi, string? TienIch, decimal DonGia, decimal? DonGiaCaoDiem,
    decimal? DonGiaCuoiTuan, TrangThaiSan TrangThai, TimeOnly GioMoCua,
    TimeOnly GioDongCua, DateOnly? NgayBaoTri, string? GhiChu);

public record TrangSanDoc(IReadOnlyList<SanDoc> DanhSach, int Trang, int TongTrang, int TongSoSan);

public record LoaiSanLuaChon(int MaLoaiSan, string TenLoai, bool DangHoatDong,
    decimal DonGiaTheoGio, decimal DonGiaCaoDiem, decimal DonGiaCuoiTuan);

// Phan Giang Tâm - 23103100196. M2: Điều kiện tìm, lọc và sắp xếp sân.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

// Giá lọc/sắp xếp là giá giờ thường T2–T6; nhập đủ ngày giờ để tra cứu sân trống qua M4.
public record TraCuuSan(
    string? TuKhoa = null,
    int? MaLoaiSan = null,
    TrangThaiSan? TrangThai = null,
    decimal? GiaTu = null,
    decimal? GiaDen = null,
    string SapXep = "ten_tang",
    int Trang = 1,
    DateOnly? NgaySuDung = null,
    TimeOnly? GioBatDau = null,
    TimeOnly? GioKetThuc = null);
public record LoaiSanLoc(int MaLoaiSan, string TenLoai);

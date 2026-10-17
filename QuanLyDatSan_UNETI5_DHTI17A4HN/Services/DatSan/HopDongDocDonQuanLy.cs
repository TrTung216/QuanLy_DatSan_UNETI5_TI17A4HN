// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-02/03 - bộ lọc, sắp xếp và DTO danh sách/chi tiết quản lý; chuyển RowVersion để xử lý đơn an toàn.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public enum SapXepDonQuanLy { NgayGiam, NgayTang, KhachTang, KhachGiam, SanTang, SanGiam }

// Khoảng ngày lọc theo ngày sử dụng (GioBatDau), bao gồm cả hai đầu.
public sealed record LocDonQuanLy(string? TuKhoa, int? MaLoaiSan, TrangThaiDatSan? TrangThai,
    DateOnly? TuNgay, DateOnly? DenNgay, SapXepDonQuanLy SapXep, int Trang);
public sealed record LoaiSanLocDto(int MaLoaiSan, string TenLoai);
public sealed record DonQuanLyDongDto(int MaDatSan, string TenKhachHang, string TenSan, string TenLoai,
    DateTime NgayDat, DateTime GioBatDau, DateTime GioKetThuc, TrangThaiDatSan TrangThai, decimal TienCoc);
public sealed record DanhSachDonQuanLyDto(IReadOnlyList<DonQuanLyDongDto> Dong, int Trang, int TongTrang, int TongSo);
public sealed record ChiTietDonQuanLyDto(ChiTietDonDto Don, int MaKhachHang,
    string TenKhachHang, string SoDienThoai, string Email);

// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Bộ lọc và DTO danh sách, chi tiết đơn dành cho quản lý.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Bổ sung tiền hiển thị do service cung cấp và mã lỗi tiền.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public enum SapXepDonQuanLy { NgayGiam, NgayTang, KhachTang, KhachGiam, SanTang, SanGiam }

public sealed record LocDonQuanLy(string? TuKhoa, int? MaLoaiSan, TrangThaiDatSan? TrangThai,
    DateOnly? TuNgay, DateOnly? DenNgay, SapXepDonQuanLy SapXep, int Trang);
public sealed record LoaiSanLocDto(int MaLoaiSan, string TenLoai);
public sealed record DonQuanLyDongDto(int MaDatSan, string TenKhachHang, string TenSan, string TenLoai,
    DateTime NgayDat, DateTime GioBatDau, DateTime GioKetThuc, TrangThaiDatSan TrangThai, decimal TienCoc);
public sealed record DanhSachDonQuanLyDto(IReadOnlyList<DonQuanLyDongDto> Dong, int Trang, int TongTrang, int TongSo);
public sealed record ChiTietDonQuanLyDto(ChiTietDonDto Don, int MaKhachHang,
    string TenKhachHang, string SoDienThoai, string Email)
{
    public int PhienBanChinhSachCoc { get; init; }
    public decimal TienCocYeuCau { get; init; }
    public decimal? TienCocToiThieu { get; init; }
    // Công nợ hiển thị: 0 sau khi hoàn thành; null khi dữ liệu tiền không hợp lệ.
    public decimal? ConPhaiThu { get; init; }
    public MaLoiCoc? LoiTien { get; init; }
}

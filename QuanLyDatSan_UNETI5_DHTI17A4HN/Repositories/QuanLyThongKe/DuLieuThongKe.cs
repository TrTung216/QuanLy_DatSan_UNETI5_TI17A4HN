// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: dữ liệu tổng hợp trung gian từ Repository thống kê.

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyThongKe;

public sealed record TongQuanThongKe(
    int SoDonHoanThanh,
    decimal TienSan,
    decimal TienDichVu,
    decimal TongDoanhThu);

public sealed record DuLieuDoanhThuNgay(
    DateOnly Ngay,
    int SoDon,
    decimal TienSan,
    decimal TienDichVu,
    decimal TongTien);

public sealed record DuLieuDoanhThuThang(
    int Nam,
    int Thang,
    int SoDon,
    decimal TienSan,
    decimal TienDichVu,
    decimal TongTien);

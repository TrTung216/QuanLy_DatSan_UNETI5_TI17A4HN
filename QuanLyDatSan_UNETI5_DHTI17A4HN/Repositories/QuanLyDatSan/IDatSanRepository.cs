// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Bổ sung hợp đồng truy cập dữ liệu giao dịch và biên nhận cọc.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Hợp đồng truy cập dữ liệu đặt sân dùng chung cho M2/M3/M4.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;

public interface IDatSanRepository
{
    Task<TaiKhoan?> LayTaiKhoanAsync(int id, CancellationToken ct);
    Task<DatSan?> DocDonCocAsync(int id, CancellationToken ct);
    Task<GiaoDichCoc?> LayGiaoDichAsync(int id, CancellationToken ct);
    Task<List<GiaoDichCoc>> LayGiaoDichCuaDonAsync(string noiDung, CancellationToken ct);
    Task<BienNhanCoc?> LayBienNhanAsync(int maDon, CancellationToken ct);
    Task<BienNhanCoc?> LayBienNhanGiaoDichAsync(int maGiaoDich, CancellationToken ct);
    void ThemGiaoDich(GiaoDichCoc giaoDich);
    void ThemBienNhan(BienNhanCoc bienNhan);
    Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai, int trang, CancellationToken cancellationToken);
    Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SanChonDto>> LayLuaChonSanAsync(CancellationToken cancellationToken);
    Task<DanhSachDonQuanLyDto> LayDanhSachQuanLyAsync(LocDonQuanLy loc, CancellationToken cancellationToken);
    Task<DuLieuChiTietDonQuanLy?> LayChiTietQuanLyAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken);
    Task<bool> KhachDuDieuKienAsync(int maTaiKhoan, CancellationToken cancellationToken);
    Task<KhachHang?> LayKhachTaoDonAsync(int maTaiKhoan, CancellationToken cancellationToken);
    Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan, CancellationToken cancellationToken);
    Task KhoaSanAsync(int maSan, CancellationToken cancellationToken);
    Task<DuLieuKhaDungSan?> LayKhaDungAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan, CancellationToken cancellationToken);
    Task<Dictionary<int, DuLieuKhaDungSan>> LayKhaDungTheoLoAsync(int[] ids, DateTime batDau, DateTime ketThuc, CancellationToken cancellationToken);
    Task<int?> LayMaSanCuaDonAsync(int maDatSan, int? maTaiKhoanChuDon, CancellationToken cancellationToken);
    Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, int? maTaiKhoanChuDon, CancellationToken cancellationToken);
    Task<List<DongDichVu>> LayDongTinhTienAsync(int maDatSan, CancellationToken cancellationToken);
    void Them(DatSan don);
}

// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng truy cập và lưu dòng dịch vụ trong đơn.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;

public interface IDichVuTrongDonRepository
{
    Task<TrangThaiDatSan?> LayTrangThaiAsync(int maDatSan, CancellationToken cancellationToken);
    Task<List<DongDichVuTrongDon>> LayDongAsync(int maDatSan, CancellationToken cancellationToken);
    Task<List<DichVuDoc>> LayDichVuHoatDongAsync(CancellationToken cancellationToken);
    Task<DichVu?> LayDichVuAsync(int maDichVu, CancellationToken cancellationToken);
    Task<bool> CoQuyenQuanLyAsync(int maTaiKhoan, CancellationToken cancellationToken);
    Task<byte[]?> LayRowVersionAsync(int maDatSan, CancellationToken cancellationToken);
    Task<int?> LayMaSanAsync(int maDatSan, CancellationToken cancellationToken);
    Task KhoaSanAsync(int maSan, CancellationToken cancellationToken);
    Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, CancellationToken cancellationToken);
    Task<List<DongDichVuTrongDon>> LayLichSuHuyAsync(int maDatSan, CancellationToken cancellationToken);
    Task GhiLichSuHuyVaXoaDongAsync(ChiTietDatSan dong, int maTaiKhoan, CancellationToken cancellationToken);
    IEnumerable<DongDichVu> LayDongTinhTien(DatSan don);
    void DanhDauCapNhatTienDichVu(DatSan don);
    void DatRowVersionGoc(DatSan don, byte[] phienBan);
}

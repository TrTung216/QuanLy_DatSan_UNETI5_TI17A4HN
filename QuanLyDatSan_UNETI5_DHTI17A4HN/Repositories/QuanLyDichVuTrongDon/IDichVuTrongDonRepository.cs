// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
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
    Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, CancellationToken cancellationToken);
    void XoaDong(ChiTietDatSan dong);
    IEnumerable<DongDichVu> LayDongTinhTien(DatSan don);
    void DanhDauCapNhatTienDichVu(DatSan don);
}

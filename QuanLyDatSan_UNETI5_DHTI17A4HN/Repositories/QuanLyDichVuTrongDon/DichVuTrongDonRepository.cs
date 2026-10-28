// Codex: tách truy vấn từ service DichVuTrongDon, giữ nguyên bộ lọc và tracking.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;

public class DichVuTrongDonRepository(ApplicationDbContext db) : IDichVuTrongDonRepository
{
    public Task<TrangThaiDatSan?> LayTrangThaiAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.DatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan).Select(x => (TrangThaiDatSan?)x.TrangThai).SingleOrDefaultAsync(cancellationToken);
    public Task<List<DongDichVuTrongDon>> LayDongAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.ChiTietDatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan).OrderBy(x => x.MaChiTiet)
            .Select(x => new DongDichVuTrongDon(x.MaChiTiet, x.MaDichVu, x.DichVu.TenDichVu, x.DichVu.DonViTinh, x.SoLuong, x.DonGia)).ToListAsync(cancellationToken);
    public Task<List<DichVuDoc>> LayDichVuHoatDongAsync(CancellationToken cancellationToken) =>
        db.DichVus.AsNoTracking().Where(x => x.TrangThai == TrangThaiDichVu.HoatDong).OrderBy(x => x.TenDichVu).ThenBy(x => x.MaDichVu)
            .Select(x => new DichVuDoc(x.MaDichVu, x.TenDichVu, x.DonViTinh, x.DonGia, x.MoTa, x.TrangThai)).ToListAsync(cancellationToken);
    public Task<DichVu?> LayDichVuAsync(int maDichVu, CancellationToken cancellationToken) =>
        db.DichVus.AsNoTracking().SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);
    public Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.DatSans.Include(x => x.ChiTietDatSans).SingleOrDefaultAsync(x => x.MaDatSan == maDatSan, cancellationToken);
    public void XoaDong(ChiTietDatSan dong) => db.ChiTietDatSans.Remove(dong);
    public IEnumerable<DongDichVu> LayDongTinhTien(DatSan don) => don.ChiTietDatSans
        .Where(x => db.Entry(x).State != EntityState.Deleted).Select(x => new DongDichVu(x.SoLuong, x.DonGia)).ToArray();
    public void DanhDauCapNhatTienDichVu(DatSan don) => db.Entry(don).Property(x => x.TienDichVu).IsModified = true;
}

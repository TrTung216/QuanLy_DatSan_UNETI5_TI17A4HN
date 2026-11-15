// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: truy vấn dịch vụ trong đơn và đánh dấu cập nhật tiền dịch vụ.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;

public sealed class LichSuHuyDichVuDoc
{
    public int MaChiTiet { get; set; }
    public int MaDichVu { get; set; }
    public string TenDichVu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public bool DaHuy { get; set; }
    public DateTime? NgayHuy { get; set; }
    public int? MaTaiKhoanHuy { get; set; }
}

public class DichVuTrongDonRepository(ApplicationDbContext db) : IDichVuTrongDonRepository
{
    public Task<TrangThaiDatSan?> LayTrangThaiAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.DatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan).Select(x => (TrangThaiDatSan?)x.TrangThai).SingleOrDefaultAsync(cancellationToken);
    public Task<List<DongDichVuTrongDon>> LayDongAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.ChiTietDatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan).OrderBy(x => x.MaChiTiet)
            .Select(x => new DongDichVuTrongDon(x.MaChiTiet, x.MaDichVu, x.DichVu.TenDichVu, x.DichVu.DonViTinh,
                x.SoLuong, x.DonGia, false, null, null)).ToListAsync(cancellationToken);
    public Task<List<DichVuDoc>> LayDichVuHoatDongAsync(CancellationToken cancellationToken) =>
        db.DichVus.AsNoTracking().Where(x => x.TrangThai == TrangThaiDichVu.HoatDong).OrderBy(x => x.TenDichVu).ThenBy(x => x.MaDichVu)
            .Select(x => new DichVuDoc(x.MaDichVu, x.TenDichVu, x.DonViTinh, x.DonGia, x.MoTa, x.TrangThai)).ToListAsync(cancellationToken);
    public Task<DichVu?> LayDichVuAsync(int maDichVu, CancellationToken cancellationToken) =>
        db.DichVus.AsNoTracking().SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);
    public Task<bool> CoQuyenQuanLyAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
        db.TaiKhoans.AsNoTracking().AnyAsync(x => x.MaTaiKhoan == maTaiKhoan
            && x.TrangThai == TrangThaiTaiKhoan.HoatDong
            && (x.VaiTro == VaiTro.Admin || x.VaiTro == VaiTro.NhanVien), cancellationToken);
    public Task<byte[]?> LayRowVersionAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.DatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan)
            .Select(x => (byte[]?)x.RowVersion).SingleOrDefaultAsync(cancellationToken);
    public Task<int?> LayMaSanAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.DatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan)
            .Select(x => (int?)x.MaSan).SingleOrDefaultAsync(cancellationToken);
    public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM SanTheThao WITH (UPDLOCK, ROWLOCK) WHERE MaSan = {maSan}", cancellationToken);
    public Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.DatSans.Include(x => x.ChiTietDatSans).ThenInclude(x => x.DichVu)
            .SingleOrDefaultAsync(x => x.MaDatSan == maDatSan, cancellationToken);
    public Task<List<DongDichVuTrongDon>> LayLichSuHuyAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.Database.SqlQuery<LichSuHuyDichVuDoc>($"""
            SELECT MaChiTietNguon AS MaChiTiet, MaDichVu, TenDichVu, DonViTinh, SoLuong, DonGia,
                   CAST(1 AS bit) AS DaHuy, NgayHuy, MaTaiKhoanHuy
            FROM dbo.LichSuHuyDichVu
            WHERE MaDatSan = {maDatSan}
            """)
            .OrderBy(x => x.MaChiTiet)
            .Select(x => new DongDichVuTrongDon(x.MaChiTiet, x.MaDichVu, x.TenDichVu, x.DonViTinh,
                x.SoLuong, x.DonGia, x.DaHuy, x.NgayHuy, x.MaTaiKhoanHuy))
            .ToListAsync(cancellationToken);
    public async Task GhiLichSuHuyVaXoaDongAsync(ChiTietDatSan dong, int maTaiKhoan,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO dbo.LichSuHuyDichVu
                (MaDatSan, MaChiTietNguon, MaDichVu, TenDichVu, DonViTinh, SoLuong, DonGia, NgayHuy, MaTaiKhoanHuy)
            VALUES
                ({dong.MaDatSan}, {dong.MaChiTiet}, {dong.MaDichVu}, {dong.DichVu.TenDichVu},
                 {dong.DichVu.DonViTinh}, {dong.SoLuong}, {dong.DonGia}, {DateTime.UtcNow}, {maTaiKhoan})
            """, cancellationToken);
        db.ChiTietDatSans.Remove(dong);
    }
    public IEnumerable<DongDichVu> LayDongTinhTien(DatSan don) => don.ChiTietDatSans
        .Where(x => db.Entry(x).State != EntityState.Deleted)
        .Select(x => new DongDichVu(x.SoLuong, x.DonGia)).ToArray();
    public void DanhDauCapNhatTienDichVu(DatSan don) => db.Entry(don).Property(x => x.TienDichVu).IsModified = true;
    public void DatRowVersionGoc(DatSan don, byte[] phienBan) =>
        db.Entry(don).Property(x => x.RowVersion).OriginalValue = phienBan;
}

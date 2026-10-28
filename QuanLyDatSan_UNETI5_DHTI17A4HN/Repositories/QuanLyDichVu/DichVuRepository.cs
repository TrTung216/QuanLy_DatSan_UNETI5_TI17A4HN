// Codex: tách truy vấn từ service DichVu, giữ nguyên bộ lọc và tracking.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVu;

public class DichVuRepository(ApplicationDbContext db) : IDichVuRepository
{
    private const int SoDongMoiTrang = 10;
    public async Task<DanhSachDichVu> LayDanhSachAsync(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep,
        int trang, CancellationToken cancellationToken)
    {
        var truyVan = db.DichVus.AsNoTracking().AsQueryable();

        tuKhoa = tuKhoa?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
            truyVan = truyVan.Where(x => x.TenDichVu.Contains(tuKhoa));
        if (trangThai.HasValue)
            truyVan = truyVan.Where(x => x.TrangThai == trangThai.Value);

        IOrderedQueryable<DichVu> sapXepTruyVan = sapXep switch
        {
            "ten_giam" => truyVan.OrderByDescending(x => x.TenDichVu),
            "gia_tang" => truyVan.OrderBy(x => x.DonGia),
            "gia_giam" => truyVan.OrderByDescending(x => x.DonGia),
            _ => truyVan.OrderBy(x => x.TenDichVu)
        };

        var tongSoDong = await sapXepTruyVan.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSoDong / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);

        var danhSach = await sapXepTruyVan.ThenBy(x => x.MaDichVu)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang).Select(x => new DichVuDoc(x.MaDichVu, x.TenDichVu, x.DonViTinh, x.DonGia, x.MoTa, x.TrangThai)).ToListAsync(cancellationToken);
        return new DanhSachDichVu(danhSach, tuKhoa, trang, tongTrang);
    }

    public Task<DichVuDoc?> LayChiTietAsync(int maDichVu, CancellationToken cancellationToken)
        => db.DichVus.AsNoTracking().Where(x => x.MaDichVu == maDichVu).Select(x => new DichVuDoc(x.MaDichVu, x.TenDichVu, x.DonViTinh, x.DonGia, x.MoTa, x.TrangThai)).SingleOrDefaultAsync(cancellationToken);

    public Task<DichVu?> LayDeCapNhatAsync(int maDichVu, CancellationToken cancellationToken) =>
        db.DichVus.SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);
    public void Them(DichVu dichVu) => db.DichVus.Add(dichVu);
}

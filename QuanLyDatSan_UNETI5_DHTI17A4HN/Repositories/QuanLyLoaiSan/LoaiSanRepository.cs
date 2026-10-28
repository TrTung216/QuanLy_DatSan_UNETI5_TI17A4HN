// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách truy cập dữ liệu, giữ hợp đồng nghiệp vụ.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;

public class LoaiSanRepository(ApplicationDbContext db) : ILoaiSanRepository
{
    private const int SoDongMoiTrang = 10;

    public async Task<TrangLoaiSan> LayDanhSachAsync(string? tuKhoa, TrangThaiLoaiSan? trangThai,
        string sapXep, int trang, CancellationToken cancellationToken)
    {
        tuKhoa = tuKhoa?.Trim();
        var query = db.LoaiSans.AsNoTracking();
        if (!string.IsNullOrEmpty(tuKhoa)) query = query.Where(x => x.TenLoai.Contains(tuKhoa));
        if (trangThai.HasValue) query = query.Where(x => x.TrangThai == trangThai.Value);
        var tongSo = await query.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);
        var sapXepQuery = sapXep == "ten_giam" ? query.OrderByDescending(x => x.TenLoai) : query.OrderBy(x => x.TenLoai);
        var danhSach = await Chieu(sapXepQuery.ThenBy(x => x.MaLoaiSan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)).ToListAsync(cancellationToken);
        return new(danhSach, trang, tongTrang, tongSo);
    }

    public Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken) =>
        Chieu(db.LoaiSans.AsNoTracking().Where(x => x.MaLoaiSan == id)).SingleOrDefaultAsync(cancellationToken);

    public Task<LoaiSan?> LayDeCapNhatAsync(int id, CancellationToken ct) => db.LoaiSans.SingleOrDefaultAsync(x => x.MaLoaiSan == id, ct);
    public Task<bool> TrungTenAsync(string ten, int? id, CancellationToken ct) => db.LoaiSans.AnyAsync(x => x.TenLoai == ten && (!id.HasValue || x.MaLoaiSan != id.Value), ct);
    public Task<bool> CoSanThamChieuAsync(int id, CancellationToken ct) => db.SanTheThaos.AnyAsync(x => x.MaLoaiSan == id, ct);
    public void Them(LoaiSan loaiSan) => db.LoaiSans.Add(loaiSan);
    public void Xoa(LoaiSan loaiSan) => db.LoaiSans.Remove(loaiSan);
    private static IQueryable<LoaiSanDoc> Chieu(IQueryable<LoaiSan> query) => query.Select(x => new LoaiSanDoc(
        x.MaLoaiSan, x.TenLoai, x.MoTa, x.SoNguoiToiDa, x.DonGiaTheoGio, x.TrangThai, x.DanhSachSan.Count));
}

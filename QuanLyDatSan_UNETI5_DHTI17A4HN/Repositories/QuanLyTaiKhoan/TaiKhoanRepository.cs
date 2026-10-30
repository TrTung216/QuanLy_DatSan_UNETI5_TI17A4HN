// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách truy cập dữ liệu, giữ hợp đồng nghiệp vụ.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;

public class TaiKhoanRepository(ApplicationDbContext db) : ITaiKhoanRepository
{
    private const int SoDongMoiTrang = 10;

    public async Task<TrangTaiKhoan> LayDanhSachAsync(string? tuKhoa, VaiTro? vaiTro,
        TrangThaiTaiKhoan? trangThai, int trang, CancellationToken cancellationToken)
    {
        tuKhoa = tuKhoa?.Trim();
        var query = db.TaiKhoans.AsNoTracking();
        if (!string.IsNullOrEmpty(tuKhoa))
            query = query.Where(x => x.TenDangNhap.Contains(tuKhoa) || x.HoTen.Contains(tuKhoa) || x.Email.Contains(tuKhoa));
        if (vaiTro.HasValue) query = query.Where(x => x.VaiTro == vaiTro.Value);
        if (trangThai.HasValue) query = query.Where(x => x.TrangThai == trangThai.Value);
        var tongSo = await query.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);
        var danhSach = await Chieu(query.OrderBy(x => x.TenDangNhap).ThenBy(x => x.MaTaiKhoan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)).ToListAsync(cancellationToken);
        return new(danhSach, trang, tongTrang, tongSo);
    }

    public Task<TaiKhoanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken) =>
        Chieu(db.TaiKhoans.AsNoTracking().Where(x => x.MaTaiKhoan == id)).SingleOrDefaultAsync(cancellationToken);

    public Task<TaiKhoan?> LayDeCapNhatAsync(int id, CancellationToken ct) => db.TaiKhoans.Include(x => x.KhachHang).SingleOrDefaultAsync(x => x.MaTaiKhoan == id, ct);
    public Task<TaiKhoan?> LayDeXacThucAsync(string ten, CancellationToken ct) => db.TaiKhoans.SingleOrDefaultAsync(x => x.TenDangNhap == ten, ct);
    public Task<bool> TrungTenAsync(string ten, CancellationToken ct) => db.TaiKhoans.AnyAsync(x => x.TenDangNhap == ten, ct);
    public Task<List<int>> LayMaTheoQuyenAsync(VaiTro vaiTro, TrangThaiTaiKhoan trangThai, CancellationToken ct) => db.TaiKhoans.Where(x => x.VaiTro == vaiTro && x.TrangThai == trangThai).Select(x => x.MaTaiKhoan).ToListAsync(ct);
    public void Them(TaiKhoan taiKhoan) => db.TaiKhoans.Add(taiKhoan);
    private static IQueryable<TaiKhoanDoc> Chieu(IQueryable<TaiKhoan> query) => query.Select(x => new TaiKhoanDoc(
        x.MaTaiKhoan, x.TenDangNhap, x.HoTen, x.Email, x.VaiTro, x.TrangThai, x.KhachHang != null));
}

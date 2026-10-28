// Codex: tách truy vấn từ service SanTheThao, giữ nguyên bộ lọc và tracking.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan;

public class SanTheThaoRepository(ApplicationDbContext db) : ISanTheThaoRepository
{
    private const int SoDongMoiTrang = 10;
    public async Task<TrangSanDoc> LayDanhSachAsync(TraCuuSan dieuKien, bool chiHoatDong, CancellationToken cancellationToken)
    {
        // Lọc quyền trước: điều kiện URL không thể làm lộ sân của khách.
        var truyVan = LaySanDuocXem(chiHoatDong);
        var tuKhoa = dieuKien.TuKhoa?.Trim().ToLower();
        if (!string.IsNullOrEmpty(tuKhoa))
            truyVan = truyVan.Where(x => x.TenSan.ToLower().Contains(tuKhoa)
                || x.LoaiSan.TenLoai.ToLower().Contains(tuKhoa) || x.DiaChi.ToLower().Contains(tuKhoa));
        if (dieuKien.MaLoaiSan.HasValue) truyVan = truyVan.Where(x => x.MaLoaiSan == dieuKien.MaLoaiSan);
        if (dieuKien.TrangThai.HasValue) truyVan = truyVan.Where(x => x.TrangThai == dieuKien.TrangThai);
        if (dieuKien.GiaTu.HasValue) truyVan = truyVan.Where(x => x.DonGia >= dieuKien.GiaTu.Value);
        if (dieuKien.GiaDen.HasValue) truyVan = truyVan.Where(x => x.DonGia <= dieuKien.GiaDen.Value);
        var tongSoSan = await truyVan.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSoSan / (double)SoDongMoiTrang));
        var trang = Math.Clamp(dieuKien.Trang, 1, tongTrang);
        var sapXep = dieuKien.SapXep switch
        {
            "ten_giam" => truyVan.OrderByDescending(x => x.TenSan),
            "gia_tang" => truyVan.OrderBy(x => x.DonGia),
            "gia_giam" => truyVan.OrderByDescending(x => x.DonGia),
            "gio_tang" => truyVan.OrderBy(x => x.GioMoCua),
            "gio_giam" => truyVan.OrderByDescending(x => x.GioMoCua),
            _ => truyVan.OrderBy(x => x.TenSan)
        };
        // MaSan bảo đảm thứ tự ổn định khi nhiều sân có cùng tên/giá/giờ.
        var danhSach = await sapXep.ThenBy(x => x.MaSan).Include(x => x.LoaiSan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang).ToListAsync(cancellationToken);
        return new(danhSach.Select(ThanhDuLieuDoc).ToList(), trang, tongTrang, tongSoSan);
    }

    public async Task<IReadOnlyList<LoaiSanLoc>> LayLoaiSanLocAsync(bool chiHoatDong, CancellationToken cancellationToken)
    {
        var loaiSan = db.LoaiSans.AsNoTracking();
        if (chiHoatDong)
            loaiSan = loaiSan.Where(x => x.TrangThai == TrangThaiLoaiSan.HoatDong);
        return await loaiSan.OrderBy(x => x.TenLoai).ThenBy(x => x.MaLoaiSan)
            .Select(x => new LoaiSanLoc(x.MaLoaiSan, x.TenLoai)).ToListAsync(cancellationToken);
    }

    public async Task<SanDoc?> LayChiTietAsync(int maSan, bool chiHoatDong, CancellationToken cancellationToken)
    {
        var san = await LaySanDuocXem(chiHoatDong).Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        return san is null ? null : ThanhDuLieuDoc(san);
    }

    public async Task<bool> KiemTraLoaiSanAsync(int? maLoaiSan, int? maLoaiSanHienTai, CancellationToken cancellationToken)
        => maLoaiSan.HasValue && await db.LoaiSans.AsNoTracking().AnyAsync(x => x.MaLoaiSan == maLoaiSan.Value
            && (x.TrangThai == TrangThaiLoaiSan.HoatDong || x.MaLoaiSan == maLoaiSanHienTai), cancellationToken);

    private IQueryable<SanTheThao> LaySanDuocXem(bool chiHoatDong)
    {
        var truyVan = db.SanTheThaos.AsNoTracking();
        if (chiHoatDong)
            truyVan = truyVan.Where(x => x.TrangThai == TrangThaiSan.HoatDong
                && x.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong);
        return truyVan;
    }

    private static SanDoc ThanhDuLieuDoc(SanTheThao x)
        => new(x.MaSan, x.TenSan, x.MaLoaiSan, x.LoaiSan.TenLoai, x.DiaChi, x.TienIch,
            x.DonGia, x.DonGiaCaoDiem, x.DonGiaCuoiTuan, x.TrangThai,
            x.GioMoCua, x.GioDongCua, x.NgayBaoTri, x.GhiChu);
    public async Task<List<LoaiSan>> LayLoaiSanLuaChonAsync(int? maLoaiSanHienTai, CancellationToken cancellationToken) =>
        await db.LoaiSans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiLoaiSan.HoatDong || x.MaLoaiSan == maLoaiSanHienTai)
            .OrderBy(x => x.TenLoai).ThenBy(x => x.MaLoaiSan).ToListAsync(cancellationToken);
    public Task<SanTheThao?> LayGiaAsync(int maSan, bool chiHoatDong, CancellationToken cancellationToken) =>
        LaySanDuocXem(chiHoatDong).SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
    public Task<SanTheThao?> LayDeCapNhatAsync(int maSan, CancellationToken cancellationToken) =>
        db.SanTheThaos.SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
    public void Them(SanTheThao san) => db.SanTheThaos.Add(san);
    public void Xoa(SanTheThao san) => db.SanTheThaos.Remove(san);
}

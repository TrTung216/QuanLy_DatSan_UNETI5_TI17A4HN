// Phan Giang Tâm - 23103100196. M2: Truy vấn, kiểm tra và lưu sân theo quy ước service.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

public class SanTheThaoService(ApplicationDbContext db, ITaiKhoanHienTai taiKhoanHienTai) : ISanTheThaoService
{
    private const int SoDongMoiTrang = 10;

    public async Task<TrangSanDoc> LayDanhSachAsync(int trang, CancellationToken cancellationToken)
    {
        var truyVan = LaySanDuocXem();
        var tongSoSan = await truyVan.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSoSan / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);
        var danhSach = await truyVan.Include(x => x.LoaiSan)
            .OrderBy(x => x.TenSan).ThenBy(x => x.MaSan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang).ToListAsync(cancellationToken);
        return new(danhSach.Select(ThanhDuLieuDoc).ToList(), trang, tongTrang, tongSoSan);
    }

    public async Task<SanDoc?> LayChiTietAsync(int maSan, CancellationToken cancellationToken)
    {
        var san = await LaySanDuocXem().Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        return san is null ? null : ThanhDuLieuDoc(san);
    }

    public async Task<IReadOnlyList<LoaiSanLuaChon>> LayLuaChonAsync(int? maLoaiSanHienTai, CancellationToken cancellationToken)
    {
        var cacLoai = await db.LoaiSans.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiLoaiSan.HoatDong || x.MaLoaiSan == maLoaiSanHienTai)
            .OrderBy(x => x.TenLoai).ToListAsync(cancellationToken);
        return cacLoai.Select(x =>
        {
            var gia = GiaThueSan.GiaGoiY(x.TenLoai, x.DonGiaTheoGio);
            return new LoaiSanLuaChon(x.MaLoaiSan, x.TenLoai, x.TrangThai == TrangThaiLoaiSan.HoatDong,
                gia.Thuong, gia.CaoDiem, gia.CuoiTuan);
        }).ToList();
    }

    public async Task<bool> KiemTraLoaiSanAsync(int? maLoaiSan, int? maLoaiSanHienTai, CancellationToken cancellationToken)
        => maLoaiSan.HasValue && await db.LoaiSans.AsNoTracking().AnyAsync(x => x.MaLoaiSan == maLoaiSan.Value
            && (x.TrangThai == TrangThaiLoaiSan.HoatDong || x.MaLoaiSan == maLoaiSanHienTai), cancellationToken);

    public async Task<KetQuaSan<int>> TaoAsync(GhiSan yeuCau, CancellationToken cancellationToken)
    {
        if (!DuLieuHopLe(yeuCau)) return KetQuaSan<int>.Loi(MaLoiSan.DuLieuKhongHopLe);
        if (!await KiemTraLoaiSanAsync(yeuCau.MaLoaiSan, null, cancellationToken))
            return KetQuaSan<int>.Loi(MaLoiSan.LoaiSanKhongHopLe);
        var san = new SanTheThao();
        GanDuLieu(san, yeuCau);
        db.SanTheThaos.Add(san);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (LaLoiRangBuoc(exception))
        {
            db.Entry(san).State = EntityState.Detached;
            return KetQuaSan<int>.Loi(MaLoiSan.DuLieuLienQuanThayDoi);
        }
        return KetQuaSan<int>.Dat(san.MaSan);
    }

    public async Task<KetQuaSan<int>> SuaAsync(int maSan, GhiSan yeuCau, CancellationToken cancellationToken)
    {
        var san = await db.SanTheThaos.SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        if (san is null) return KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai);
        if (!DuLieuHopLe(yeuCau)) return KetQuaSan<int>.Loi(MaLoiSan.DuLieuKhongHopLe);
        if (!await KiemTraLoaiSanAsync(yeuCau.MaLoaiSan, san.MaLoaiSan, cancellationToken))
            return KetQuaSan<int>.Loi(MaLoiSan.LoaiSanKhongHopLe);
        GanDuLieu(san, yeuCau);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(san).State = EntityState.Detached;
            return KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai);
        }
        catch (DbUpdateException exception) when (LaLoiRangBuoc(exception))
        {
            db.Entry(san).State = EntityState.Detached;
            return KetQuaSan<int>.Loi(MaLoiSan.DuLieuLienQuanThayDoi);
        }
        return KetQuaSan<int>.Dat(san.MaSan);
    }

    public async Task<KetQuaSan<int>> XoaAsync(int maSan, CancellationToken cancellationToken)
    {
        var san = await db.SanTheThaos.SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        if (san is null) return KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai);
        db.SanTheThaos.Remove(san);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (LaLoiRangBuoc(exception))
        {
            db.Entry(san).State = EntityState.Unchanged;
            return KetQuaSan<int>.Loi(MaLoiSan.DangDuocThamChieu);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(san).State = EntityState.Detached;
            return KetQuaSan<int>.Loi(MaLoiSan.DaBiXoa);
        }
        return KetQuaSan<int>.Dat(maSan);
    }

    public async Task<KetQuaSan<decimal>> TinhGiaAsync(int maSan, DateOnly ngay,
        TimeOnly batDau, TimeOnly ketThuc, CancellationToken cancellationToken)
    {
        var san = await LaySanDuocXem().SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        if (san is null) return KetQuaSan<decimal>.Loi(MaLoiSan.KhongTonTai);
        try { return KetQuaSan<decimal>.Dat(GiaThueSan.TinhTien(san, ngay, batDau, ketThuc)); }
        catch (ArgumentException) { return KetQuaSan<decimal>.Loi(MaLoiSan.GioThueKhongHopLe); }
    }

    private IQueryable<SanTheThao> LaySanDuocXem()
    {
        var truyVan = db.SanTheThaos.AsNoTracking();
        if (taiKhoanHienTai.VaiTro is not (VaiTro.Admin or VaiTro.NhanVien))
            truyVan = truyVan.Where(x => x.TrangThai == TrangThaiSan.HoatDong
                && x.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong);
        return truyVan;
    }

    private static bool LaLoiRangBuoc(DbUpdateException exception)
        => exception.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 };

    private static bool DuLieuHopLe(GhiSan x)
        => !string.IsNullOrWhiteSpace(x.TenSan) && x.TenSan.Length <= 100
        && !string.IsNullOrWhiteSpace(x.DiaChi) && x.DiaChi.Length <= 255
        && (x.TienIch?.Length ?? 0) <= 1000 && (x.GhiChu?.Length ?? 0) <= 1000
        && x.MaLoaiSan > 0 && Enum.IsDefined(x.TrangThai) && x.GioDongCua > x.GioMoCua
        && GiaHopLe(x.DonGia) && GiaHopLe(x.DonGiaCaoDiem) && GiaHopLe(x.DonGiaCuoiTuan);

    private static bool GiaHopLe(decimal gia)
        => gia >= 0 && gia <= 9999999999999999.99m && gia == decimal.Truncate(gia);

    private static void GanDuLieu(SanTheThao san, GhiSan x)
    {
        san.TenSan = x.TenSan.Trim();
        san.MaLoaiSan = x.MaLoaiSan;
        san.DiaChi = x.DiaChi.Trim();
        san.TienIch = x.TienIch?.Trim();
        san.DonGia = x.DonGia;
        san.DonGiaCaoDiem = x.DonGiaCaoDiem;
        san.DonGiaCuoiTuan = x.DonGiaCuoiTuan;
        san.TrangThai = x.TrangThai;
        san.GioMoCua = x.GioMoCua;
        san.GioDongCua = x.GioDongCua;
        san.NgayBaoTri = x.NgayBaoTri;
        san.GhiChu = x.GhiChu?.Trim();
    }

    private static SanDoc ThanhDuLieuDoc(SanTheThao x)
        => new(x.MaSan, x.TenSan, x.MaLoaiSan, x.LoaiSan.TenLoai, x.DiaChi, x.TienIch,
            x.DonGia, x.DonGiaCaoDiem, x.DonGiaCuoiTuan, x.TrangThai,
            x.GioMoCua, x.GioDongCua, x.NgayBaoTri, x.GhiChu);
}

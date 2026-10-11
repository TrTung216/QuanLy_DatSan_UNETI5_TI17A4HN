// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách CRUD, validation và bảo toàn tham chiếu loại sân.
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;

public class LoaiSanService(ApplicationDbContext db) : ILoaiSanService
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

    public async Task<KetQuaLoaiSan> TaoAsync(GhiLoaiSan yeuCau, CancellationToken cancellationToken)
    {
        if (!HopLe(yeuCau)) return new(MaLoiLoaiSan.DuLieuKhongHopLe);
        if (await TrungTenAsync(yeuCau.TenLoai, null, cancellationToken)) return new(MaLoiLoaiSan.TrungTen);
        var loaiSan = new LoaiSan();
        GanThongTin(loaiSan, yeuCau);
        db.LoaiSans.Add(loaiSan);
        return await LuuAsync(loaiSan, cancellationToken);
    }

    public async Task<KetQuaLoaiSan> SuaAsync(int id, GhiLoaiSan yeuCau, CancellationToken cancellationToken)
    {
        var loaiSan = await db.LoaiSans.SingleOrDefaultAsync(x => x.MaLoaiSan == id, cancellationToken);
        if (loaiSan is null) return new(MaLoiLoaiSan.KhongTonTai);
        if (!HopLe(yeuCau)) return new(MaLoiLoaiSan.DuLieuKhongHopLe);
        if (await TrungTenAsync(yeuCau.TenLoai, id, cancellationToken)) return new(MaLoiLoaiSan.TrungTen);
        // Không cập nhật giá hoặc trạng thái các sân đang tham chiếu.
        GanThongTin(loaiSan, yeuCau);
        return await LuuAsync(loaiSan, cancellationToken);
    }

    public async Task<KetQuaLoaiSan> XoaAsync(int id, CancellationToken cancellationToken)
    {
        var loaiSan = await db.LoaiSans.SingleOrDefaultAsync(x => x.MaLoaiSan == id, cancellationToken);
        if (loaiSan is null) return new(MaLoiLoaiSan.KhongTonTai);
        if (await db.SanTheThaos.AnyAsync(x => x.MaLoaiSan == id, cancellationToken))
            return new(MaLoiLoaiSan.DangDuocThamChieu);
        db.LoaiSans.Remove(loaiSan);
        try
        {
            // FK NoAction vẫn bảo vệ tham chiếu phát sinh sau AnyAsync.
            await db.SaveChangesAsync(cancellationToken);
            return new(MaLoiLoaiSan.ThanhCong, id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            return new(MaLoiLoaiSan.DangDuocThamChieu);
        }
        catch (DbUpdateConcurrencyException) { return new(MaLoiLoaiSan.KhongTonTai); }
    }

    private Task<bool> TrungTenAsync(string ten, int? id, CancellationToken cancellationToken)
    {
        ten = ten.Trim();
        // Dùng cùng collation và unique index hiện có tại database.
        return db.LoaiSans.AnyAsync(x => x.TenLoai == ten && (!id.HasValue || x.MaLoaiSan != id.Value), cancellationToken);
    }

    private async Task<KetQuaLoaiSan> LuuAsync(LoaiSan loaiSan, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new(MaLoiLoaiSan.ThanhCong, loaiSan.MaLoaiSan);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new(MaLoiLoaiSan.TrungTen);
        }
        catch (DbUpdateConcurrencyException) { return new(MaLoiLoaiSan.XungDotDuLieu); }
    }

    private static bool HopLe(GhiLoaiSan x) => !string.IsNullOrWhiteSpace(x.TenLoai) && x.TenLoai.Length <= 100
        && (x.MoTa is null || x.MoTa.Length <= 1000) && x.SoNguoiToiDa > 0
        && x.DonGiaTheoGio is >= 0 and <= 9999999999999999m
        && x.DonGiaTheoGio == decimal.Truncate(x.DonGiaTheoGio) && Enum.IsDefined(x.TrangThai);

    private static void GanThongTin(LoaiSan loaiSan, GhiLoaiSan x)
    {
        loaiSan.TenLoai = x.TenLoai.Trim();
        loaiSan.MoTa = string.IsNullOrWhiteSpace(x.MoTa) ? null : x.MoTa.Trim();
        loaiSan.SoNguoiToiDa = x.SoNguoiToiDa;
        loaiSan.DonGiaTheoGio = x.DonGiaTheoGio;
        loaiSan.TrangThai = x.TrangThai;
    }

    private static IQueryable<LoaiSanDoc> Chieu(IQueryable<LoaiSan> query) => query.Select(x => new LoaiSanDoc(
        x.MaLoaiSan, x.TenLoai, x.MoTa, x.SoNguoiToiDa, x.DonGiaTheoGio, x.TrangThai, x.DanhSachSan.Count));
}

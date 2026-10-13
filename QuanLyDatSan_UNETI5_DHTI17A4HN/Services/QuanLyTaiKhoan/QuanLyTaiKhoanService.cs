// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách nghiệp vụ tài khoản, transaction và thu hồi phiên.
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;

public class QuanLyTaiKhoanService(ApplicationDbContext db, IPasswordHasher<TaiKhoan> hasher,
    ITaiKhoanHienTai hienTai, PhienBanQuyenTaiKhoan phienBanQuyen) : IQuanLyTaiKhoanService
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

    public Task<KetQuaQuanLyTaiKhoan> TaoAsync(TaoTaiKhoan yeuCau, CancellationToken cancellationToken)
    {
        if (!ThongTinHopLe(yeuCau.HoTen, yeuCau.Email, yeuCau.VaiTro)
            || string.IsNullOrWhiteSpace(yeuCau.TenDangNhap) || yeuCau.TenDangNhap.Length > 50
            || string.IsNullOrWhiteSpace(yeuCau.MatKhau) || yeuCau.MatKhau.Length is < 12 or > 128)
            return Task.FromResult(new KetQuaQuanLyTaiKhoan([MaLoiTaiKhoan.DuLieuKhongHopLe]));
        var ten = yeuCau.TenDangNhap.Trim();
        return LuuAsync(async _ =>
        {
            if (await db.TaiKhoans.AnyAsync(x => x.TenDangNhap == ten, cancellationToken))
                return new([MaLoiTaiKhoan.TrungTen]);
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = ten, HoTen = yeuCau.HoTen, Email = yeuCau.Email,
                VaiTro = yeuCau.VaiTro, TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, yeuCau.MatKhau);
            db.TaiKhoans.Add(taiKhoan);
            return new([]);
        }, cancellationToken);
    }

    public Task<KetQuaQuanLyTaiKhoan> SuaAsync(int id, SuaTaiKhoan yeuCau, CancellationToken cancellationToken)
    {
        if (!ThongTinHopLe(yeuCau.HoTen, yeuCau.Email, yeuCau.VaiTro) || !Enum.IsDefined(yeuCau.TrangThai))
            return Task.FromResult(new KetQuaQuanLyTaiKhoan([MaLoiTaiKhoan.DuLieuKhongHopLe]));
        var doiQuyenHoacTrangThai = false;
        return LuuAsync(async adminIds =>
        {
            var taiKhoan = await db.TaiKhoans.SingleOrDefaultAsync(x => x.MaTaiKhoan == id, cancellationToken);
            if (taiKhoan is null) return new([MaLoiTaiKhoan.KhongTonTai]);
            var conLaAdminHoatDong = yeuCau.VaiTro == VaiTro.Admin && yeuCau.TrangThai == TrangThaiTaiKhoan.HoatDong;
            List<MaLoiTaiKhoan> loi = [];
            if (!conLaAdminHoatDong && adminIds.Contains(id) && adminIds.Count == 1) loi.Add(MaLoiTaiKhoan.AdminCuoiCung);
            if (id == hienTai.MaTaiKhoan && !conLaAdminHoatDong) loi.Add(MaLoiTaiKhoan.TuKhoaHoacHaQuyen);
            if (loi.Count > 0) return new(loi);

            doiQuyenHoacTrangThai = taiKhoan.VaiTro != yeuCau.VaiTro || taiKhoan.TrangThai != yeuCau.TrangThai;
            taiKhoan.HoTen = yeuCau.HoTen;
            taiKhoan.Email = yeuCau.Email;
            taiKhoan.VaiTro = yeuCau.VaiTro;
            taiKhoan.TrangThai = yeuCau.TrangThai;
            return new([]);
        }, cancellationToken, () =>
        {
            if (doiQuyenHoacTrangThai) phienBanQuyen.VoHieuHoa(id);
        });
    }

    private async Task<KetQuaQuanLyTaiKhoan> LuuAsync(Func<List<int>, Task<KetQuaQuanLyTaiKhoan>> capNhat,
        CancellationToken cancellationToken, Action? sauKhiLuu = null)
    {
        try
        {
            // Cùng transaction giữ tập Admin hoạt động và kiểm tra lại quyền người đang thao tác.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var adminIds = await db.TaiKhoans.Where(x => x.VaiTro == VaiTro.Admin && x.TrangThai == TrangThaiTaiKhoan.HoatDong)
                .Select(x => x.MaTaiKhoan).ToListAsync(cancellationToken);
            if (!hienTai.MaTaiKhoan.HasValue || !adminIds.Contains(hienTai.MaTaiKhoan.Value)) return new([MaLoiTaiKhoan.KhongDuQuyen]);
            var ketQua = await capNhat(adminIds);
            if (!ketQua.ThanhCong) return ketQua;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            sauKhiLuu?.Invoke();
            return ketQua;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return new([MaLoiTaiKhoan.TrungTen]);
        }
        catch (Exception ex) when (ex is SqlException { Number: 1205 }
            || ex is DbUpdateException { InnerException: SqlException { Number: 1205 } })
        {
            return new([MaLoiTaiKhoan.XungDotDuLieu]);
        }
    }

    private static bool ThongTinHopLe(string hoTen, string email, VaiTro vaiTro) =>
        !string.IsNullOrWhiteSpace(hoTen) && hoTen.Length <= 100
        && !string.IsNullOrWhiteSpace(email) && email.Length <= 254 && new EmailAddressAttribute().IsValid(email)
        && Enum.IsDefined(vaiTro);

    private static IQueryable<TaiKhoanDoc> Chieu(IQueryable<TaiKhoan> query) => query.Select(x => new TaiKhoanDoc(
        x.MaTaiKhoan, x.TenDangNhap, x.HoTen, x.Email, x.VaiTro, x.TrangThai));
}

// Lưu và commit/rollback transaction; dọn thay đổi khi thất bại và ánh xạ lỗi lưu trữ.

using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

public sealed class DonViCongViec(ApplicationDbContext db, ILogger<DonViCongViec>? logger = null) : IDonViCongViec
{
    private bool dangThucThi;

    public async Task LuuAsync(CancellationToken cancellationToken)
    {
        if (dangThucThi || db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Không được lưu độc lập trong giao dịch đang thực thi.");
        var thayDoi = db.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToArray();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (Exception ex)
        {
            foreach (var entry in thayDoi) entry.State = EntityState.Detached;
            if (AnhXa(ex) is { } loi) throw loi;
            throw;
        }
    }

    public async Task<T> TrongGiaoDichAsync<T>(Func<CancellationToken, Task<T>> xuLy, Func<T, bool> duocLuu,
        IsolationLevel isolationLevel, CancellationToken cancellationToken)
    {
        if (dangThucThi || db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("Không hỗ trợ giao dịch lồng.");
        if (db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Phải hoàn tất thay đổi trước khi bắt đầu một ca sử dụng mới.");
        var truoc = db.ChangeTracker.Entries().ToDictionary(x => x.Entity, x => x.CurrentValues.Clone(), ReferenceEqualityComparer.Instance);
        var daCommit = false;
        dangThucThi = true;
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
            Exception? loiGoc = null;
            try
            {
                var ketQua = await xuLy(cancellationToken);
                if (!duocLuu(ketQua)) return ketQua;
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                daCommit = true;
                return ketQua;
            }
            catch (Exception ex)
            {
                loiGoc = ex;
                throw;
            }
            finally
            {
                if (!daCommit)
                {
                    try { await transaction.RollbackAsync(CancellationToken.None); }
                    catch (Exception rollbackError) when (loiGoc is not null)
                    {
                        logger?.LogWarning(rollbackError, "Không hoàn tất rollback sau lỗi giao dịch.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (AnhXa(ex) is { } loi) throw loi;
            throw;
        }
        finally
        {
            if (!daCommit)
            {
                foreach (var entry in db.ChangeTracker.Entries().ToArray())
                {
                    if (!truoc.TryGetValue(entry.Entity, out var values) || entry.State != EntityState.Unchanged
                        || entry.Properties.Any(p => !Equals(p.CurrentValue, values[p.Metadata.Name])))
                        entry.State = EntityState.Detached;
                }
            }
            dangThucThi = false;
        }
    }

    private static LoiLuuTruException? AnhXa(Exception ex)
    {
        if (ex is DbUpdateConcurrencyException) return new(MaLoiLuuTru.XungDotPhienBan, ex);
        var sql = ex as SqlException ?? ex.InnerException as SqlException;
        if (sql is null) return null;
        if (sql.Number == 1205) return new(MaLoiLuuTru.Deadlock, ex);
        if (sql.Number is 2601 or 2627)
        {
            if (sql.Message.Contains("IX_LoaiSan_TenLoai", StringComparison.Ordinal)) return new(MaLoiLuuTru.TrungTenLoaiSan, ex);
            if (sql.Message.Contains("IX_TaiKhoan_TenDangNhap", StringComparison.Ordinal)) return new(MaLoiLuuTru.TrungTenTaiKhoan, ex);
        }
        if (sql.Number == 547)
        {
            if (sql.Message.Contains("FK_SanTheThao_LoaiSan_MaLoaiSan", StringComparison.Ordinal)) return new(MaLoiLuuTru.ThamChieuLoaiSan, ex);
            if (sql.Message.Contains("FK_DatSan_SanTheThao_MaSan", StringComparison.Ordinal)) return new(MaLoiLuuTru.ThamChieuSan, ex);
        }
        return null;
    }
}

// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ transaction, rollback và lỗi lưu dùng chung.
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

public sealed class DonViCongViec(ApplicationDbContext db, ILogger<DonViCongViec> logger) : IDonViCongViec
{
    private bool dangThucHien;
    private bool khongTheTaiSuDung;

    public async Task LuuAsync(CancellationToken cancellationToken) =>
        await ThucHienAsync(_ => Task.FromResult(new QuyetDinhLuu<bool>(true, true)),
            null, choPhepThayDoiTruoc: true, cancellationToken);

    public Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> congViec,
        IsolationLevel mucCoLap, CancellationToken cancellationToken) =>
        ThucHienAsync(congViec, mucCoLap, choPhepThayDoiTruoc: false, cancellationToken);

    private async Task<T> ThucHienAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> congViec,
        IsolationLevel? mucCoLap, bool choPhepThayDoiTruoc, CancellationToken ct)
    {
        if (khongTheTaiSuDung)
            throw new InvalidOperationException("Scope lưu dữ liệu đã lỗi khi dọn transaction; phải tạo scope mới.");
        if (dangThucHien || db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Không mở hoặc lưu transaction lồng nhau.");
        if (!choPhepThayDoiTruoc && db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Phải hoàn tất thay đổi đang chờ trước khi mở ca nghiệp vụ mới.");

        dangThucHien = true;
        // Lưu tham chiếu trước SaveChanges: sau khi ghi thành công EF có thể đổi state thành Unchanged/Detached.
        var daTacDong = new HashSet<object>(ReferenceEqualityComparer.Instance);
        IDbContextTransaction? transaction = null;
        var daCommit = false;
        try
        {
            GhiNhanThayDoi(daTacDong);
            transaction = mucCoLap.HasValue
                ? await db.Database.BeginTransactionAsync(mucCoLap.Value, ct)
                : await db.Database.BeginTransactionAsync(ct);
            var quyetDinh = await congViec(ct);
            GhiNhanThayDoi(daTacDong);
            if (!quyetDinh.DuocLuu)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                GoTheoDoi(daTacDong);
                return quyetDinh.KetQua;
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            daCommit = true;
            return quyetDinh.KetQua;
        }
        catch (Exception ex)
        {
            if (!daCommit)
            {
                try
                {
                    if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception cleanupException)
                {
                    khongTheTaiSuDung = true;
                    logger.LogError("Không rollback được transaction; loại lỗi: {LoaiLoi}", cleanupException.GetType().Name);
                }
                GhiNhanThayDoi(daTacDong);
                GoTheoDoi(daTacDong);
            }
            if (NhanDienLoi(ex) is { } maLoi) throw new LoiLuuTruException(maLoi, ex);
            // Giữ nguyên cancellation và lỗi ngoài dự kiến cho handler; không chuyển thành thành công/lỗi trùng.
            throw;
        }
        finally
        {
            try
            {
                if (transaction is not null) await transaction.DisposeAsync();
            }
            catch
            {
                khongTheTaiSuDung = true;
                throw;
            }
            finally { dangThucHien = false; }
        }
    }

    private void GhiNhanThayDoi(HashSet<object> entities)
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(x =>
                     x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            entities.Add(entry.Entity);
    }

    private void GoTheoDoi(HashSet<object> entities)
    {
        // Không Clear context: giữ các entity chỉ đọc/không liên quan.
        foreach (var entity in entities) db.Entry(entity).State = EntityState.Detached;
    }

    private static MaLoiLuuTru? NhanDienLoi(Exception exception)
    {
        if (exception is DbUpdateConcurrencyException) return MaLoiLuuTru.XungDotPhienBan;
        // EF execution strategy có thể bọc deadlock trong InvalidOperationException → DbUpdateException → SqlException.
        SqlException? sql = null;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException) { sql = sqlException; break; }
        }
        if (sql is null) return null;
        if (sql.Number == 1205) return MaLoiLuuTru.Deadlock;
        // SQL Server không cung cấp tên constraint thành trường riêng; chỉ khớp tên cụ thể của model hiện hành.
        if (sql.Number is 2601 or 2627)
        {
            if (sql.Message.Contains("'IX_LoaiSan_TenLoai'", StringComparison.Ordinal)) return MaLoiLuuTru.TrungTenLoaiSan;
            if (sql.Message.Contains("'IX_TaiKhoan_TenDangNhap'", StringComparison.Ordinal)) return MaLoiLuuTru.TrungTenDangNhap;
        }
        if (sql.Number == 547 && sql.Message.Contains("FK_SanTheThao_LoaiSan_MaLoaiSan", StringComparison.Ordinal))
            return MaLoiLuuTru.LoaiSanDangDuocThamChieu;
        return null;
    }
}

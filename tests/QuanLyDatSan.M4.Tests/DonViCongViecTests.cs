// Codex: kiểm thử Unit of Work trên SQL Server riêng của fixture, không dùng DB vận hành.
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class DonViCongViecTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    private static DichVu Moi() => new() { TenDichVu = Guid.NewGuid().ToString("N"), DonViTinh = "Chai", DonGia = 1000 };

    [Fact]
    public async Task CallbackThatBaiKhongLuuVaKhongLamMatEntityKhongLienQuan()
    {
        await using var db = fixture.Db();
        var khongLienQuan = await db.LoaiSans.FirstAsync();
        var uow = new DonViCongViec(db);
        var dv = Moi();
        var ketQua = await uow.TrongGiaoDichAsync(_ =>
        {
            db.DichVus.Add(dv);
            return Task.FromResult(false);
        }, x => x, IsolationLevel.Serializable, default);
        Assert.False(ketQua);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(EntityState.Detached, db.Entry(dv).State);
        Assert.Equal(EntityState.Unchanged, db.Entry(khongLienQuan).State);
        await uow.LuuAsync(default);
        Assert.False(await db.DichVus.AnyAsync(x => x.TenDichVu == dv.TenDichVu));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoiSauSqlHoacTruocCommitRollbackVaKhongLuuLai(bool loiCommit)
    {
        await using var seed = fixture.Db();
        var dv = Moi();
        seed.Add(dv);
        await seed.SaveChangesAsync();
        var tenCu = dv.TenDichVu;
        IInterceptor fault = loiCommit ? new LoiCommit() : new LoiSauSql();
        await using var db = fixture.Db(fault);
        var tracked = await db.DichVus.SingleAsync(x => x.MaDichVu == dv.MaDichVu);
        var uow = new DonViCongViec(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.TrongGiaoDichAsync(_ =>
        {
            tracked.TenDichVu = "Không được lưu";
            return Task.FromResult(true);
        }, x => x, IsolationLevel.Serializable, default));
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(EntityState.Detached, db.Entry(tracked).State);
        Assert.False(db.ChangeTracker.HasChanges());
        // Interceptor gây lỗi đúng một lần, nên lần lưu kế tiếp kiểm chứng cleanup thật.
        await uow.LuuAsync(default);
        await using var verify = fixture.Db();
        Assert.Equal(tenCu, (await verify.DichVus.SingleAsync(x => x.MaDichVu == dv.MaDichVu)).TenDichVu);
    }

    [Fact]
    public async Task HuyRequestRollbackBangTokenCleanupVaTaiSuDungScopeDuoc()
    {
        await using var db = fixture.Db();
        using var cts = new CancellationTokenSource();
        var uow = new DonViCongViec(db);
        var dv = Moi();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => uow.TrongGiaoDichAsync(token =>
        {
            db.Add(dv);
            cts.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }, x => x, IsolationLevel.Serializable, cts.Token));
        Assert.Null(db.Database.CurrentTransaction);
        Assert.False(db.ChangeTracker.HasChanges());
        await uow.LuuAsync(default);
        Assert.False(await db.DichVus.AnyAsync(x => x.TenDichVu == dv.TenDichVu));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChanServiceConLuuHoacMoGiaoDichRieng(bool giaoDichLong)
    {
        await using var db = fixture.Db();
        var uow = new DonViCongViec(db);
        var dv = Moi();
        await Assert.ThrowsAsync<InvalidOperationException>(() => uow.TrongGiaoDichAsync(async token =>
        {
            db.Add(dv);
            if (giaoDichLong)
                await uow.TrongGiaoDichAsync(_ => Task.FromResult(true), x => x, IsolationLevel.Serializable, token);
            else await uow.LuuAsync(token);
            return true;
        }, x => x, IsolationLevel.Serializable, default));
        await uow.LuuAsync(default);
        Assert.False(await db.DichVus.AnyAsync(x => x.TenDichVu == dv.TenDichVu));
    }

    [Fact]
    public async Task ChiTraThanhCongSauCommitVaGiuIsolationLevel()
    {
        await using var db = fixture.Db();
        var uow = new DonViCongViec(db);
        var dv = Moi();
        Assert.True(await uow.TrongGiaoDichAsync(_ =>
        {
            Assert.Equal(IsolationLevel.Serializable, db.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            db.Add(dv);
            return Task.FromResult(true);
        }, x => x, IsolationLevel.Serializable, default));
        Assert.Null(db.Database.CurrentTransaction);
        await using var verify = fixture.Db();
        Assert.True(await verify.DichVus.AnyAsync(x => x.MaDichVu == dv.MaDichVu));
    }

    [Fact]
    public async Task KhongAmThamLuuThayDoiCoSanCuaThaoTacKhac()
    {
        await using var db = fixture.Db();
        var dv = Moi(); db.Add(dv);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new DonViCongViec(db).TrongGiaoDichAsync(
            _ => Task.FromResult(true), x => x, IsolationLevel.Serializable, default));
        Assert.Equal(EntityState.Added, db.Entry(dv).State);
    }

    private sealed class LoiSauSql : SaveChangesInterceptor
    {
        private bool daGayLoi;
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (!daGayLoi) { daGayLoi = true; throw new InvalidOperationException("Lỗi thử sau SQL."); }
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task UniqueDuocPhanBietTheoRangBuocVaLoiCheckKhongBiChe()
    {
        await using var db = fixture.Db();
        var uow = new DonViCongViec(db);
        var tenLoai = await db.LoaiSans.Select(x => x.TenLoai).FirstAsync();
        var loai = new LoaiSan { TenLoai = tenLoai, SoNguoiToiDa = 10, DonGiaTheoGio = 1000 };
        db.Add(loai);
        var loiLoai = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.TrungTenLoaiSan, loiLoai.Ma);
        Assert.Equal(EntityState.Detached, db.Entry(loai).State);
        db.Add(new TaiKhoan { TenDangNhap = "admin", MatKhau = "hash-test", HoTen = "Test", Email = "test@example.test" });
        var loiTaiKhoan = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.TrungTenTaiKhoan, loiTaiKhoan.Ma);
        db.Add(new LoaiSan { TenLoai = "", SoNguoiToiDa = 10, DonGiaTheoGio = 1000 });
        await Assert.ThrowsAsync<DbUpdateException>(() => uow.LuuAsync(default));
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task FkLoaiSanTraMaThamChieuVaKhongXoaLaiSauLoi()
    {
        var don = await fixture.Booking();
        await using var db = fixture.Db();
        var loai = await db.LoaiSans.SingleAsync(x => x.MaLoaiSan == don.SanTheThao.MaLoaiSan);
        db.Remove(loai);
        var uow = new DonViCongViec(db);
        var loi = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.ThamChieuLoaiSan, loi.Ma);
        await uow.LuuAsync(default);
        Assert.True(await db.LoaiSans.AnyAsync(x => x.MaLoaiSan == loai.MaLoaiSan));
    }

    private sealed class LoiCommit : DbTransactionInterceptor
    {
        private bool daGayLoi;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!daGayLoi) { daGayLoi = true; throw new InvalidOperationException("Lỗi thử trước commit."); }
            return ValueTask.FromResult(result);
        }
    }

    [Fact]
    public async Task DeadlockTrongTruyVanDuocAnhXaVaGiuLoiGocKhiServerDaRollback()
    {
        await using var seed = fixture.Db();
        var a = Moi(); var b = Moi();
        seed.AddRange(a, b); await seed.SaveChangesAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var daKhoaA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var daKhoaB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<MaLoiLuuTru?> CapNhat(int truoc, int sau, TaskCompletionSource daKhoa, Task choDoi)
        {
            await using var db = fixture.Db();
            var uow = new DonViCongViec(db);
            try
            {
                await uow.TrongGiaoDichAsync(async token =>
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DichVu SET DonGia = DonGia + 1 WHERE MaDichVu = {truoc}", token);
                    daKhoa.SetResult();
                    await choDoi.WaitAsync(token);
                    await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE DichVu SET DonGia = DonGia + 1 WHERE MaDichVu = {sau}", token);
                    return true;
                }, x => x, IsolationLevel.ReadCommitted, timeout.Token);
                return null;
            }
            catch (LoiLuuTruException ex)
            {
                Assert.Null(db.Database.CurrentTransaction);
                await uow.LuuAsync(timeout.Token);
                return ex.Ma;
            }
        }
        var ketQua = await Task.WhenAll(
            CapNhat(a.MaDichVu, b.MaDichVu, daKhoaA, daKhoaB.Task),
            CapNhat(b.MaDichVu, a.MaDichVu, daKhoaB, daKhoaA.Task));
        Assert.Single(ketQua, x => x is null);
        Assert.Single(ketQua, x => x == MaLoiLuuTru.Deadlock);
        await seed.Entry(a).ReloadAsync(); await seed.Entry(b).ReloadAsync();
        Assert.Equal(1001m, a.DonGia); Assert.Equal(1001m, b.DonGia);
    }
}

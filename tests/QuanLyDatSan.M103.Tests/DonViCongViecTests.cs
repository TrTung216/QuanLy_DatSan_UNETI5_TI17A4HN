// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử transaction và rollback theo quy ước repository.
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class DonViCongViecTests
{
    [Fact]
    public async Task TuChoiNghiepVuKhongLuuVaKhongLamMatEntityKhongLienQuan()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var donVi = scope.ServiceProvider.GetRequiredService<IDonViCongViec>();
        var repository = scope.ServiceProvider.GetRequiredService<ITaiKhoanRepository>();
        var khongDoi = await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 1);
        var ketQua = await donVi.ThucHienTrongGiaoDichAsync(async ct =>
        {
            var tk = await repository.LayDeCapNhatAsync(2, ct);
            tk!.HoTen = "KHONG DUOC LUU";
            repository.Them(new TaiKhoan { TenDangNhap = "rollback", HoTen = "Test", Email = "test@example.test", MatKhau = "test-only" });
            return new QuyetDinhLuu<int>(false, 42);
        }, IsolationLevel.Serializable, default);
        Assert.Equal(42, ketQua);
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Equal(EntityState.Unchanged, db.Entry(khongDoi).State);
        await donVi.LuuAsync(default);
        Assert.Equal("nhanvien", (await app.DocTaiKhoanAsync(2)).HoTen);
        Assert.False(await db.TaiKhoans.AnyAsync(x => x.TenDangNhap == "rollback"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoiSauGhiHoacTruocCommitRollbackVaKhongLuuLai(bool loiCommit)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        IInterceptor interceptor = loiCommit ? new LoiCommitMotLan() : new LoiSauGhiMotLan();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>(scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>())
            .AddInterceptors(interceptor).Options;
        await using var db = new ApplicationDbContext(options);
        var donVi = new DonViCongViec(db, NullLogger<DonViCongViec>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => donVi.ThucHienTrongGiaoDichAsync(async ct =>
        {
            (await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2, ct)).HoTen = "ROLLBACK";
            return new QuyetDinhLuu<bool>(true, true);
        }, IsolationLevel.Serializable, default));
        Assert.Null(db.Database.CurrentTransaction);
        await donVi.LuuAsync(default);
        Assert.Equal("nhanvien", (await app.DocTaiKhoanAsync(2)).HoTen);
    }

    [Fact]
    public async Task HuyRequestVanRollbackVaTruyenCancellation()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var donVi = scope.ServiceProvider.GetRequiredService<IDonViCongViec>();
        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => donVi.ThucHienTrongGiaoDichAsync(async ct =>
        {
            (await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2, ct)).HoTen = "CANCELLED";
            cts.Cancel(); ct.ThrowIfCancellationRequested();
            return new QuyetDinhLuu<bool>(true, true);
        }, IsolationLevel.Serializable, cts.Token));
        Assert.Null(db.Database.CurrentTransaction);
        await donVi.LuuAsync(default);
        Assert.Equal("nhanvien", (await app.DocTaiKhoanAsync(2)).HoTen);
    }

    [Fact]
    public async Task ChanLuuLongVaKhongCommitSom()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var donVi = scope.ServiceProvider.GetRequiredService<IDonViCongViec>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => donVi.ThucHienTrongGiaoDichAsync(async ct =>
        {
            (await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2, ct)).HoTen = "NESTED";
            await donVi.LuuAsync(ct);
            return new QuyetDinhLuu<bool>(true, true);
        }, IsolationLevel.Serializable, default));
        await donVi.LuuAsync(default);
        Assert.Equal("nhanvien", (await app.DocTaiKhoanAsync(2)).HoTen);
    }

    [Fact]
    public async Task ChanThayDoiChoTuTacVuKhacTruocCallback()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var donVi = scope.ServiceProvider.GetRequiredService<IDonViCongViec>();
        var tk = await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2);
        tk.HoTen = "Pending";
        var daGoi = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => donVi.ThucHienTrongGiaoDichAsync(_ =>
        {
            daGoi = true;
            return Task.FromResult(new QuyetDinhLuu<bool>(true, true));
        }, IsolationLevel.Serializable, default));
        Assert.False(daGoi);
        Assert.Equal(EntityState.Modified, db.Entry(tk).State);
        Assert.Equal("nhanvien", (await app.DocTaiKhoanAsync(2)).HoTen);
    }

    private sealed class LoiSauGhiMotLan : SaveChangesInterceptor
    {
        private bool daNem;
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (!daNem) { daNem = true; throw new InvalidOperationException("TEST: after save"); }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class LoiCommitMotLan : DbTransactionInterceptor
    {
        private bool daNem;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (!daNem) { daNem = true; throw new InvalidOperationException("TEST: before commit"); }
            return ValueTask.FromResult(result);
        }
    }
}

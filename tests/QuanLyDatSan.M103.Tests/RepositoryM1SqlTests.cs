// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử repository/transaction trên SQL Server riêng.
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public sealed class M1SqlFactAttribute : FactAttribute
{
    public M1SqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M1_TEST_SQLSERVER") != "1")
            Skip = "Bật QLDS_M1_TEST_SQLSERVER=1 để chạy với SQL Server LocalDB riêng.";
    }
}

public class RepositoryM1SqlTests(M1SqlFixture fixture) : IClassFixture<M1SqlFixture>
{
    private static DonViCongViec DonVi(ApplicationDbContext db) => new(db, NullLogger<DonViCongViec>.Instance);
    private static LoaiSan Loai(string ten) => new() { TenLoai = ten, SoNguoiToiDa = 10, DonGiaTheoGio = 100000 };

    [M1SqlFact]
    public async Task TrungTenLoaiSanDuocPhanLoaiVaKhongLuuLaiSauLoi()
    {
        var ten = "Loai_" + Guid.NewGuid().ToString("N");
        await using var db = fixture.Db();
        var repo = new LoaiSanRepository(db); var uow = DonVi(db);
        repo.Them(Loai(ten)); await uow.LuuAsync(default);
        repo.Them(Loai(ten.ToUpperInvariant()));
        var ex = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.TrungTenLoaiSan, ex.MaLoi);
        await uow.LuuAsync(default);
        Assert.Equal(1, await db.LoaiSans.CountAsync(x => x.TenLoai == ten));
    }

    [M1SqlFact]
    public async Task TrungTenTaiKhoanDuocPhanLoaiRieng()
    {
        var ten = "tk_" + Guid.NewGuid().ToString("N");
        await using var db = fixture.Db();
        var repo = new TaiKhoanRepository(db); var uow = DonVi(db);
        repo.Them(TaiKhoanMoi(ten)); await uow.LuuAsync(default);
        repo.Them(TaiKhoanMoi(ten.ToUpperInvariant()));
        var ex = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.TrungTenDangNhap, ex.MaLoi);
        Assert.Null(db.Database.CurrentTransaction);
    }

    [M1SqlFact]
    public async Task XoaLoaiSanCoThamChieuKhongMatSan()
    {
        int id;
        await using (var setup = fixture.Db())
        {
            var loai = Loai("FK_" + Guid.NewGuid().ToString("N"));
            setup.SanTheThaos.Add(new SanTheThao { TenSan = "Test", DiaChi = "Test", LoaiSan = loai,
                DonGia = 100000, GioMoCua = new(6, 0), GioDongCua = new(22, 0) });
            await setup.SaveChangesAsync(); id = loai.MaLoaiSan;
        }
        await using var db = fixture.Db();
        var repo = new LoaiSanRepository(db); var uow = DonVi(db);
        repo.Xoa((await repo.LayDeCapNhatAsync(id, default))!);
        var ex = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.LoaiSanDangDuocThamChieu, ex.MaLoi);
        await uow.LuuAsync(default);
        Assert.True(await db.LoaiSans.AnyAsync(x => x.MaLoaiSan == id));
        Assert.True(await db.SanTheThaos.AnyAsync(x => x.MaLoaiSan == id));
    }

    [M1SqlFact]
    public async Task LoiCheckConstraintKhongBiCheThanhLoiThamChieu()
    {
        await using var db = fixture.Db();
        var loai = Loai("CHECK_" + Guid.NewGuid().ToString("N")); loai.SoNguoiToiDa = -1;
        db.LoaiSans.Add(loai);
        await Assert.ThrowsAsync<DbUpdateException>(() => DonVi(db).LuuAsync(default));
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [M1SqlFact]
    public async Task BanGhiBiXoaDongThoiTraXungDotPhienBan()
    {
        await using var db = fixture.Db();
        var loai = Loai("DELETE_" + Guid.NewGuid().ToString("N"));
        db.LoaiSans.Add(loai); await db.SaveChangesAsync();
        await using (var other = fixture.Db())
        {
            other.LoaiSans.Remove((await other.LoaiSans.FindAsync(loai.MaLoaiSan))!);
            await other.SaveChangesAsync();
        }
        loai.MoTa = "STALE";
        var ex = await Assert.ThrowsAsync<LoiLuuTruException>(() => DonVi(db).LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.XungDotPhienBan, ex.MaLoi);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [M1SqlFact]
    public async Task RepositoryLocSapXepPhanTrangVaDocKhongTracking()
    {
        var prefix = "PAGE_" + Guid.NewGuid().ToString("N");
        await using (var setup = fixture.Db())
        {
            for (var i = 0; i < 13; i++) setup.LoaiSans.Add(Loai(prefix + i.ToString("D2")));
            await setup.SaveChangesAsync();
        }
        await using var db = fixture.Db();
        var repo = new LoaiSanRepository(db);
        var trang1 = await repo.LayDanhSachAsync(prefix, TrangThaiLoaiSan.HoatDong, "ten_giam", 1, default);
        var trang2 = await repo.LayDanhSachAsync(prefix, TrangThaiLoaiSan.HoatDong, "ten_giam", 2, default);
        Assert.Equal(13, trang1.TongSo); Assert.Equal(10, trang1.DanhSach.Count); Assert.Equal(3, trang2.DanhSach.Count);
        Assert.Equal(13, trang1.DanhSach.Concat(trang2.DanhSach).Select(x => x.MaLoaiSan).Distinct().Count());
        Assert.EndsWith("12", trang1.DanhSach[0].TenLoai);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [M1SqlFact]
    public async Task GiuSerializableVaRollbackKhiNghiepVuTuChoi()
    {
        await using var db = fixture.Db(); var uow = DonVi(db);
        var ten = "ROLL_" + Guid.NewGuid().ToString("N");
        await uow.ThucHienTrongGiaoDichAsync(ct =>
        {
            Assert.Equal(IsolationLevel.Serializable, db.Database.CurrentTransaction!.GetDbTransaction().IsolationLevel);
            db.TaiKhoans.Add(TaiKhoanMoi(ten));
            return Task.FromResult(new QuyetDinhLuu<bool>(false, false));
        }, IsolationLevel.Serializable, default);
        await uow.LuuAsync(default);
        Assert.False(await db.TaiKhoans.AnyAsync(x => x.TenDangNhap == ten));
    }

    [M1SqlFact]
    public async Task HaiAdminHaQuyenDongThoiVanConAdminVaChiThuHoiPhienDaCommit()
    {
        int mot, hai;
        await using (var setup = fixture.Db())
        {
            // Fixture không seed tài khoản khác; hai Admin là toàn bộ tập Admin của test.
            var a = TaiKhoanMoi("admin1"); a.VaiTro = VaiTro.Admin;
            var b = TaiKhoanMoi("admin2"); b.VaiTro = VaiTro.Admin;
            setup.AddRange(a, b); await setup.SaveChangesAsync(); mot = a.MaTaiKhoan; hai = b.MaTaiKhoan;
        }
        var phien = new PhienBanQuyenTaiKhoan(); var phienMot = phien.Lay(mot); var phienHai = phien.Lay(hai);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<KetQuaQuanLyTaiKhoan> Sua(int actor, int target)
        {
            await using var db = fixture.Db();
            var service = new QuanLyTaiKhoanService(new TaiKhoanRepository(db), DonVi(db), new PasswordHasher<TaiKhoan>(),
                new Actor(actor), phien, new DongHoTest());
            await start.Task;
            return await service.SuaAsync(target, new("Staff", "staff@example.test", VaiTro.NhanVien, TrangThaiTaiKhoan.HoatDong), default);
        }
        var t1 = Sua(mot, hai); var t2 = Sua(hai, mot); start.SetResult();
        var results = await Task.WhenAll(t1, t2);
        Assert.Single(results, x => x.ThanhCong);
        await using var read = fixture.Db();
        Assert.Equal(1, await read.TaiKhoans.CountAsync(x => x.VaiTro == VaiTro.Admin && x.TrangThai == TrangThaiTaiKhoan.HoatDong));
        Assert.Equal(results[0].ThanhCong, phienHai != phien.Lay(hai));
        Assert.Equal(results[1].ThanhCong, phienMot != phien.Lay(mot));
    }

    private static TaiKhoan TaiKhoanMoi(string ten) => new()
    { TenDangNhap = ten, MatKhau = "test-only-not-login", HoTen = "Test", Email = "test@example.test", VaiTro = VaiTro.NhanVien };

    private sealed class DongHoTest : IDongHo
    {
        public DateTime BayGio => new(2030, 1, 1, 8, 0, 0);
        public DateOnly HomNay => DateOnly.FromDateTime(BayGio);
    }

    private sealed class Actor(int id) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => id;
        public string HoTen => "Test";
        public VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.Admin;
    }
}

public sealed class M1SqlFixture : IAsyncLifetime
{
    private readonly string tenDb = "QLDS_M1_Repository_Test_" + Guid.NewGuid().ToString("N");
    private bool daTao;
    public ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = tenDb,
            IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 15 }.ConnectionString).Options);

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M1_TEST_SQLSERVER") != "1") return;
        await using var db = Db(); daTao = await db.Database.EnsureCreatedAsync();
        if (!daTao) throw new InvalidOperationException("Không dùng lại database không do test tạo.");
    }

    public async Task DisposeAsync()
    {
        if (!daTao) return;
        await using var db = Db();
        var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog;
        if (actual != tenDb || !actual.StartsWith("QLDS_M1_Repository_Test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Sai database cleanup.");
        await db.Database.EnsureDeletedAsync();
    }
}

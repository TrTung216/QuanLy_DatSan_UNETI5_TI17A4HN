// M1: Trần Trọng Tùng - 23103100202. SQL Server test riêng có guard, migration nâng cấp và ghi ảnh đồng thời.
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;
using Xunit;
namespace QuanLyDatSan.M104.Tests;

public sealed class AnhSqlFactAttribute : FactAttribute
{
    public AnhSqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M1_TEST_SQLSERVER") != "1") Skip = "Bật QLDS_M1_TEST_SQLSERVER=1, dùng LocalDB test riêng.";
    }
}

public class AnhSqlTests(AnhSqlFixture fixture) : IClassFixture<AnhSqlFixture>
{
    private ServiceProvider Provider(IKhoAnh kho)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(fixture.ConnectionString));
        services.AddScoped<IAnhRepository, AnhRepository>();
        services.AddScoped<ILoaiSanRepository, LoaiSanRepository>();
        services.AddScoped<IDonViCongViec, DonViCongViec>();
        services.AddScoped<IDonDepAnh, DonDepAnh>();
        services.AddScoped<IAnhService, AnhService>();
        services.AddScoped<ILoaiSanService, LoaiSanService>();
        services.AddSingleton<ITaiKhoanHienTai>(new AdminTest());
        services.AddSingleton(kho);
        return services.BuildServiceProvider();
    }

    private static async Task<(int Id, string Key)> TaoLoai(ServiceProvider services, string root)
    {
        var b = KhoAnhTests.TaoAnh(); using var stream = new MemoryStream(b);
        var key = await services.GetRequiredService<IKhoAnh>().LuuAsync(NhomAnh.LoaiSan, new(stream, "a.png", b.Length), default);
        using var scope = services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entity = new LoaiSan { TenLoai = "Test_" + Guid.NewGuid().ToString("N"), SoNguoiToiDa = 10, DonGiaTheoGio = 100000, DuongDanAnh = key };
        db.LoaiSans.Add(entity); await db.SaveChangesAsync();
        Assert.True(File.Exists(Path.Combine(root, key)));
        return (entity.MaLoaiSan, key);
    }

    [AnhSqlFact]
    public async Task MigrationNangCapGiuDuLieuCuVaTokenCaHaiBang()
    {
        using var db = fixture.Db();
        var loai = await db.LoaiSans.AsNoTracking().SingleAsync(x => x.MaLoaiSan == 1);
        Assert.Equal("Giữ nguyên dữ liệu trước migration ảnh", loai.MoTa); Assert.Null(loai.DuongDanAnh);
        Assert.False(db.Database.HasPendingModelChanges());
        foreach (var type in new[] { typeof(LoaiSan), typeof(SanTheThao) })
        {
            var property = db.Model.FindEntityType(type)!.FindProperty("DuongDanAnh")!;
            Assert.True(property.IsConcurrencyToken); Assert.True(property.IsNullable); Assert.Equal(255, property.GetMaxLength());
        }
    }

    [AnhSqlFact]
    public async Task SanTheThaoDungTokenAnhChanGhiDeVaSuaThongTinGiuAnh()
    {
        using var tao = fixture.Db();
        var san = new SanTheThao { TenSan = "Test_" + Guid.NewGuid().ToString("N"), DiaChi = "Test",
            MaLoaiSan = 1, DonGia = 100000, GioMoCua = new(6, 0), GioDongCua = new(22, 0) };
        tao.SanTheThaos.Add(san);
        await tao.SaveChangesAsync();
        using var mot = fixture.Db(); using var hai = fixture.Db();
        var a = (await mot.SanTheThaos.FindAsync(san.MaSan))!;
        var b = (await hai.SanTheThaos.FindAsync(san.MaSan))!;
        a.DuongDanAnh = $"san/{Guid.NewGuid():N}.jpg";
        b.DuongDanAnh = $"san/{Guid.NewGuid():N}.jpg";
        await mot.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => hai.SaveChangesAsync());
        using var doc = fixture.Db();
        var hienTai = (await doc.SanTheThaos.FindAsync(san.MaSan))!;
        Assert.Equal(a.DuongDanAnh, hienTai.DuongDanAnh);
        hienTai.TenSan = "Thông tin cập nhật";
        await doc.SaveChangesAsync();
        await doc.Entry(hienTai).ReloadAsync();
        Assert.Equal(a.DuongDanAnh, hienTai.DuongDanAnh);
    }

    [AnhSqlFact]
    public async Task HaiNguoiThayAnhDongThoiChiMotThangVaKhongMatFile()
    {
        using var dir = new ThuMucAnhTest(); var kho = KhoAnhTests.Kho(dir.Root);
        using var services = Provider(kho); var (id, cu) = await TaoLoai(services, dir.Root);
        using var mot = services.CreateScope(); using var hai = services.CreateScope();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var soNguoi = 0;
        async Task ChoCungDoc() { if (Interlocked.Increment(ref soNguoi) == 2) gate.SetResult(); await gate.Task.WaitAsync(TimeSpan.FromSeconds(15)); }
        AnhService Service(IServiceProvider sp) => new(new AnhRepository(sp.GetRequiredService<ApplicationDbContext>()),
            new RepositoryDongBo(new LoaiSanRepository(sp.GetRequiredService<ApplicationDbContext>()), ChoCungDoc),
            sp.GetRequiredService<IDonViCongViec>(), new AdminTest(), kho, sp.GetRequiredService<IDonDepAnh>(), NullLogger<AnhService>.Instance);
        var bytes = KhoAnhTests.TaoAnh(); using var a = new MemoryStream(bytes); using var b = new MemoryStream(bytes);
        var results = await Task.WhenAll(Service(mot.ServiceProvider).CapNhatLoaiSanAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(a, "a.png", bytes.Length)), default),
            Service(hai.ServiceProvider).CapNhatLoaiSanAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(b, "b.png", bytes.Length)), default));
        Assert.Single(results, x => x.MaLoi == MaLoiAnh.ThanhCong); Assert.Single(results, x => x.MaLoi == MaLoiAnh.XungDotAnh);
        using var read = fixture.Db(); var key = (await read.LoaiSans.FindAsync(id))!.DuongDanAnh;
        Assert.NotNull(key); Assert.True(File.Exists(Path.Combine(dir.Root, key)));
        Assert.Single(Directory.GetFiles(Path.Combine(dir.Root, "loai-san"), "*.jpg"));
        Assert.False(File.Exists(Path.Combine(dir.Root, cu)));
    }

    [AnhSqlFact]
    public async Task GoAnhVaThayAnhDocCungPhienBanBiChanBoiToken()
    {
        using var dir = new ThuMucAnhTest(); using var services = Provider(KhoAnhTests.Kho(dir.Root));
        var (id, cu) = await TaoLoai(services, dir.Root);
        using var stale = services.CreateScope(); using var winner = services.CreateScope();
        // Giữ entity tracking cũ để kiểm cả precondition form và WHERE concurrency thực tế của EF.
        await stale.ServiceProvider.GetRequiredService<ILoaiSanRepository>().LayDeCapNhatAsync(id, default);
        var bytes = KhoAnhTests.TaoAnh(); using var input = new MemoryStream(bytes);
        Assert.Equal(MaLoiAnh.ThanhCong, (await winner.ServiceProvider.GetRequiredService<IAnhService>()
            .CapNhatLoaiSanAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(input, "a.png", bytes.Length)), default)).MaLoi);
        Assert.Equal(MaLoiAnh.XungDotAnh, (await stale.ServiceProvider.GetRequiredService<IAnhService>()
            .CapNhatLoaiSanAsync(id, new(ThaoTacAnh.GoAnh, cu, null), default)).MaLoi);
        using var db = fixture.Db(); var key = (await db.LoaiSans.FindAsync(id))!.DuongDanAnh;
        Assert.True(File.Exists(Path.Combine(dir.Root, key!)));
    }

    [AnhSqlFact]
    public async Task XoaCoThamChieuGiuAnhXoaThanhCongMoiDonTep()
    {
        using var dir = new ThuMucAnhTest(); using var services = Provider(KhoAnhTests.Kho(dir.Root));
        var (id, key) = await TaoLoai(services, dir.Root);
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.SanTheThaos.Add(new SanTheThao { TenSan = "Sân test", DiaChi = "Test", MaLoaiSan = id, DonGia = 100000, GioMoCua = new(6, 0), GioDongCua = new(22, 0) });
            await db.SaveChangesAsync();
            var result = await scope.ServiceProvider.GetRequiredService<ILoaiSanService>().XoaAsync(id, default);
            Assert.Equal(MaLoiLoaiSan.DangDuocThamChieu, result.MaLoi); Assert.True(File.Exists(Path.Combine(dir.Root, key)));
            db.SanTheThaos.Remove(await db.SanTheThaos.SingleAsync(x => x.MaLoaiSan == id)); await db.SaveChangesAsync();
            Assert.Equal(MaLoiLoaiSan.ThanhCong, (await scope.ServiceProvider.GetRequiredService<ILoaiSanService>().XoaAsync(id, default)).MaLoi);
            Assert.False(File.Exists(Path.Combine(dir.Root, key)));
        }
    }

    [AnhSqlFact]
    public async Task LoiCommitKhongXoaAnhCoTheDaDuocLuuVaDonMoCoiSau24Gio()
    {
        foreach (var daCommit in new[] { false, true })
        {
            using var dir = new ThuMucAnhTest(); var kho = KhoAnhTests.Kho(dir.Root);
            using var services = Provider(kho); var (id, cu) = await TaoLoai(services, dir.Root);
            using var scope = services.CreateScope(); var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var service = new AnhService(new AnhRepository(db), new LoaiSanRepository(db),
                new LuuLoi(sp.GetRequiredService<IDonViCongViec>(), daCommit), new AdminTest(), kho,
                sp.GetRequiredService<IDonDepAnh>(), NullLogger<AnhService>.Instance);
            var bytes = KhoAnhTests.TaoAnh(); using var stream = new MemoryStream(bytes);
            await Assert.ThrowsAsync<IOException>(() => service.CapNhatLoaiSanAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(stream, "a.png", bytes.Length)), default));
            using var read = fixture.Db(); var current = (await read.LoaiSans.FindAsync(id))!.DuongDanAnh;
            Assert.True(File.Exists(Path.Combine(dir.Root, current!)));
            var files = Directory.GetFiles(Path.Combine(dir.Root, "loai-san")); Assert.Equal(2, files.Length);
            Assert.Equal(0, await DonAnhMoCoi.ChayAsync(services, xoa: true)); // Cả hai tệp còn mới, không dọn.
            Assert.Equal(2, Directory.GetFiles(Path.Combine(dir.Root, "loai-san")).Length);
            foreach (var file in files) File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddHours(-25));
            Assert.Equal(0, await DonAnhMoCoi.ChayAsync(services, xoa: false)); Assert.Equal(2, Directory.GetFiles(Path.Combine(dir.Root, "loai-san")).Length);
            Assert.Equal(0, await DonAnhMoCoi.ChayAsync(services, xoa: true));
            Assert.Single(Directory.GetFiles(Path.Combine(dir.Root, "loai-san"))); Assert.True(File.Exists(Path.Combine(dir.Root, current!)));
        }
    }

    [AnhSqlFact]
    public async Task LoiDonFileSauCommitVanBaoThanhCongVaWarning()
    {
        using var dir = new ThuMucAnhTest(); var kho = KhoAnhTests.Kho(dir.Root);
        using var services = Provider(kho); var (id, cu) = await TaoLoai(services, dir.Root);
        using var scope = services.CreateScope(); var sp = scope.ServiceProvider; var db = sp.GetRequiredService<ApplicationDbContext>();
        // Khóa file cũ để mô phỏng lỗi xóa filesystem sau khi commit DB thành công.
        using var locked = new FileStream(Path.Combine(dir.Root, cu), FileMode.Open, FileAccess.Read, FileShare.None);
        var bytes = KhoAnhTests.TaoAnh(); using var input = new MemoryStream(bytes);
        var result = await sp.GetRequiredService<IAnhService>().CapNhatLoaiSanAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(input, "a.png", bytes.Length)), default);
        Assert.Equal(MaLoiAnh.ThanhCong, result.MaLoi); Assert.True(result.CanDonTep);
        using var read = fixture.Db(); Assert.NotEqual(cu, (await read.LoaiSans.FindAsync(id))!.DuongDanAnh);
    }

    private sealed class AdminTest : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => 1; public string HoTen => "Test";
        public VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.Admin;
    }
    private sealed class LuuLoi(IDonViCongViec inner, bool sauCommit) : IDonViCongViec
    {
        public async Task LuuAsync(CancellationToken ct) { if (sauCommit) await inner.LuuAsync(ct); throw new IOException("Mô phỏng kết quả commit chưa rõ."); }
        public Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> action, IsolationLevel level, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class RepositoryDongBo(ILoaiSanRepository inner, Func<Task> sauDoc) : ILoaiSanRepository
    {
        public async Task<LoaiSan?> LayDeCapNhatAsync(int id, CancellationToken ct) { var x = await inner.LayDeCapNhatAsync(id, ct); await sauDoc(); return x; }
        public Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken ct) => inner.LayChiTietAsync(id, ct);
        public Task<TrangLoaiSan> LayDanhSachAsync(string? keyword, TrangThaiLoaiSan? state, string sort, int page, CancellationToken ct) => inner.LayDanhSachAsync(keyword, state, sort, page, ct);
        public Task<bool> TrungTenAsync(string name, int? id, CancellationToken ct) => inner.TrungTenAsync(name, id, ct);
        public Task<bool> CoSanThamChieuAsync(int id, CancellationToken ct) => inner.CoSanThamChieuAsync(id, ct);
        public void Them(LoaiSan x) => inner.Them(x);
        public void Xoa(LoaiSan x) => inner.Xoa(x);
    }
}

public sealed class AnhSqlFixture : IAsyncLifetime
{
    private readonly string tenDb = "QLDS_M1_Anh_Test_" + Guid.NewGuid().ToString("N");
    private bool daTao;
    public string ConnectionString => new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = tenDb, IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 15 }.ConnectionString;
    public ApplicationDbContext Db() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options);
    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M1_TEST_SQLSERVER") != "1") return;
        using var db = Db();
        if (await db.Database.CanConnectAsync()) throw new InvalidOperationException("Không dùng lại DB đã tồn tại.");
        daTao = true;
        var migrations = db.Database.GetMigrations().ToArray();
        Assert.EndsWith("_ThemAnhDaiDienSanVaLoaiSan", migrations[^1]);
        await db.GetService<IMigrator>().MigrateAsync(migrations[^2]);
        await db.Database.ExecuteSqlRawAsync("UPDATE LoaiSan SET MoTa = N'Giữ nguyên dữ liệu trước migration ảnh' WHERE MaLoaiSan = 1");
        await db.GetService<IMigrator>().MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        if (!daTao) return;
        using var db = Db();
        var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog;
        if (actual != tenDb || !actual.StartsWith("QLDS_M1_Anh_Test_", StringComparison.Ordinal)) throw new InvalidOperationException("Sai DB test.");
        await db.Database.EnsureDeletedAsync();
    }
}

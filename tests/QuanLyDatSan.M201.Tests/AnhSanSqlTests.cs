// Phan Giang Tâm - 23103100196. M2: SQL Server kiểm tra xung đột ảnh, FK, commit và dọn file.
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using Models = QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

[Trait("Category", "SqlServer")]
public class AnhSanSqlTests(TraCuuSanSqlServerFixture fixture) : IClassFixture<TraCuuSanSqlServerFixture>
{
    private sealed class NguoiDung(VaiTro? vaiTro, bool coMa = true) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => coMa && vaiTro.HasValue ? 1 : null;
        public string? HoTen => "Test";
        public VaiTro? VaiTro => vaiTro;
    }

    private ServiceProvider Provider(IKhoAnh kho, SaveChangesInterceptor? interceptor = null)
    {
        using var db = fixture.TaoDb();
        var connectionString = db.Database.GetConnectionString();
        var services = new ServiceCollection().AddLogging();
        services.AddDbContext<ApplicationDbContext>(o =>
        {
            o.UseSqlServer(connectionString);
            if (interceptor is not null) o.AddInterceptors(interceptor);
        });
        services.AddSingleton(kho);
        services.AddScoped<IAnhRepository, AnhRepository>();
        services.AddScoped<IDonDepAnh, DonDepAnh>();
        services.AddScoped<IDonViCongViec, DonViCongViec>();
        services.AddScoped<ISanTheThaoRepository, SanTheThaoRepository>();
        return services.BuildServiceProvider();
    }

    private static SanTheThaoService Service(IServiceProvider sp, IDonViCongViec? uow = null,
        ITaiKhoanHienTai? actor = null, IDonDepAnh? donDep = null)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var luu = uow ?? sp.GetRequiredService<IDonViCongViec>();
        return new(sp.GetRequiredService<ISanTheThaoRepository>(), luu, actor ?? new NguoiDung(VaiTro.Admin),
            new DatSanService(new DatSanRepository(db), luu, new DongHoHeThong(), new TinhTienService(),
                new NguoiDung(VaiTro.Admin), new Microsoft.Extensions.Hosting.Internal.HostingEnvironment { EnvironmentName = "Production" },
                Microsoft.Extensions.Options.Options.Create(new CocMoPhongOptions())),
            sp.GetRequiredService<IKhoAnh>(), donDep ?? sp.GetRequiredService<IDonDepAnh>(),
            NullLogger<SanTheThaoService>.Instance);
    }

    private static async Task<(int Id, string Khoa)> TaoSan(ServiceProvider services)
    {
        var bytes = AnhSanTest.Tep(); using var stream = new MemoryStream(bytes);
        var key = await services.GetRequiredService<IKhoAnh>().LuuAsync(NhomAnh.San, new(stream, "a.png", bytes.Length), default);
        using var scope = services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var san = new SanTheThao
        {
            TenSan = "Ảnh test " + Guid.NewGuid().ToString("N"), MaLoaiSan = 1, DiaChi = "Hà Nội", DonGia = 200000,
            GioMoCua = new(6, 0), GioDongCua = new(22, 0), DuongDanAnh = key
        };
        db.SanTheThaos.Add(san); await db.SaveChangesAsync();
        return (san.MaSan, key);
    }

    [M2SqlServerTheory]
    [InlineData(null, false)]
    [InlineData(VaiTro.KhachHang, true)]
    [InlineData(VaiTro.Admin, false)]
    public async Task ServiceGhiAnhTuKiemTraQuyen(VaiTro? vaiTro, bool coMa)
    {
        using var dir = new ThuMucAnhSanTest(); using var services = Provider(AnhSanTest.Kho(dir.Root));
        using var scope = services.CreateScope();
        var result = await Service(scope.ServiceProvider, actor: new NguoiDung(vaiTro, coMa))
            .CapNhatAnhAsync(1, new(ThaoTacAnh.GoAnh, null, null), default);
        Assert.Equal(MaLoiAnh.KhongDuQuyen, result.MaLoi); Assert.False(Directory.Exists(dir.Root));
    }

    [M2SqlServerFact]
    public async Task HaiNguoiThayAnhChiMotThanhCongVaDonDungFile()
    {
        using var dir = new ThuMucAnhSanTest(); var gate = new HaiNguoiCungLuu();
        using var services = Provider(AnhSanTest.Kho(dir.Root), gate);
        var (id, cu) = await TaoSan(services); gate.Bat = true;
        using var a = services.CreateScope(); using var b = services.CreateScope();
        var bytes = AnhSanTest.Tep(); using var mot = new MemoryStream(bytes); using var hai = new MemoryStream(bytes);
        var results = await Task.WhenAll(
            Service(a.ServiceProvider).CapNhatAnhAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(mot, "a.png", bytes.Length)), default),
            Service(b.ServiceProvider).CapNhatAnhAsync(id, new(ThaoTacAnh.ThayAnh, cu, new(hai, "b.png", bytes.Length)), default));
        Assert.Single(results, x => x.MaLoi == MaLoiAnh.ThanhCong);
        Assert.Single(results, x => x.MaLoi == MaLoiAnh.XungDotAnh);
        using var read = fixture.TaoDb(); var key = (await read.SanTheThaos.FindAsync(id))!.DuongDanAnh;
        Assert.NotEqual(cu, key); Assert.True(File.Exists(Path.Combine(dir.Root, key!)));
        Assert.False(File.Exists(Path.Combine(dir.Root, cu)));
        Assert.Single(Directory.GetFiles(Path.Combine(dir.Root, "san"), "*.jpg"));
    }

    [M2SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormGoCuKhongXoaAnhMoiVaSuaXoaThongTinBaoXungDot(bool xoa)
    {
        using var dir = new ThuMucAnhSanTest(); using var services = Provider(AnhSanTest.Kho(dir.Root));
        var (id, cu) = await TaoSan(services);
        using var stale = services.CreateScope(); using var winner = services.CreateScope();
        await stale.ServiceProvider.GetRequiredService<ISanTheThaoRepository>().LayDeCapNhatAsync(id, default);
        var bytes = AnhSanTest.Tep(); using var stream = new MemoryStream(bytes);
        Assert.Equal(MaLoiAnh.ThanhCong, (await Service(winner.ServiceProvider).CapNhatAnhAsync(id,
            new(ThaoTacAnh.ThayAnh, cu, new(stream, "a.png", bytes.Length)), default)).MaLoi);
        var result = xoa ? await Service(stale.ServiceProvider).XoaAsync(id, default) :
            await Service(stale.ServiceProvider).SuaAsync(id, new("Tên cũ", 1, "Hà Nội", null, 200000, 350000, 450000,
                TrangThaiSan.HoatDong, new(6, 0), new(22, 0), null, null), default);
        Assert.Equal(MaLoiSan.XungDotDuLieu, result.MaLoi);
        using var fresh = services.CreateScope();
        Assert.Equal(MaLoiAnh.XungDotAnh, (await Service(fresh.ServiceProvider).CapNhatAnhAsync(id,
            new(ThaoTacAnh.GoAnh, cu, null), default)).MaLoi);
        using var read = fixture.TaoDb(); var san = (await read.SanTheThaos.FindAsync(id))!;
        Assert.NotEqual("Tên cũ", san.TenSan); Assert.NotEqual(cu, san.DuongDanAnh);
        Assert.True(File.Exists(Path.Combine(dir.Root, san.DuongDanAnh!)));
    }

    [M2SqlServerFact]
    public async Task XoaSanCoDonGiuAnhXoaSanChuaDatMoiDonAnh()
    {
        using var dir = new ThuMucAnhSanTest(); using var services = Provider(AnhSanTest.Kho(dir.Root));
        var (id, key) = await TaoSan(services);
        using var db = fixture.TaoDb();
        var khach = new Models.KhachHang
        {
            TaiKhoan = new Models.TaiKhoan { TenDangNhap = Guid.NewGuid().ToString("N"), MatKhau = "test-only", HoTen = "Test", Email = "test@example.test", VaiTro = VaiTro.KhachHang },
            HoTen = "Test", Email = "test@example.test", SoDienThoai = "0900000000", NgayDangKy = new(2030, 1, 1)
        };
        var don = new Models.DatSan
        {
            MaSan = id, KhachHang = khach, NgayDat = new(2030, 1, 1), GioBatDau = new(2030, 1, 2, 10, 0, 0),
            GioKetThuc = new(2030, 1, 2, 11, 0, 0), DonGia = 200000, TrangThai = TrangThaiDatSan.DaHuy
        };
        db.DatSans.Add(don); await db.SaveChangesAsync();
        using var blocked = services.CreateScope();
        Assert.Equal(MaLoiSan.DangDuocThamChieu, (await Service(blocked.ServiceProvider).XoaAsync(id, default)).MaLoi);
        Assert.True(File.Exists(Path.Combine(dir.Root, key)));
        // Chỉ dọn dữ liệu fixture để kiểm tra nhánh xóa thành công, không có endpoint xóa lịch sử đơn.
        db.DatSans.Remove(don); await db.SaveChangesAsync();
        using var final = services.CreateScope();
        Assert.True((await Service(final.ServiceProvider).XoaAsync(id, default)).ThanhCong);
        Assert.False(File.Exists(Path.Combine(dir.Root, key)));
    }

    [M2SqlServerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommitChuaRoGiuFileCoTheDuocDbThamChieu(bool daCommit)
    {
        using var dir = new ThuMucAnhSanTest(); using var services = Provider(AnhSanTest.Kho(dir.Root));
        var (id, cu) = await TaoSan(services); using var scope = services.CreateScope();
        var uow = new LuuLoi(scope.ServiceProvider.GetRequiredService<IDonViCongViec>(), daCommit);
        var bytes = AnhSanTest.Tep(); using var input = new MemoryStream(bytes);
        await Assert.ThrowsAsync<IOException>(() => Service(scope.ServiceProvider, uow).CapNhatAnhAsync(id,
            new(ThaoTacAnh.ThayAnh, cu, new(input, "a.png", bytes.Length)), default));
        using var db = fixture.TaoDb(); var key = (await db.SanTheThaos.FindAsync(id))!.DuongDanAnh;
        Assert.Equal(!daCommit, key == cu);
        Assert.True(File.Exists(Path.Combine(dir.Root, key!)));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(dir.Root, "san"), "*.jpg").Length);
    }

    [M2SqlServerFact]
    public async Task DonFileLoiSauCommitVanThanhCongVaDocDuocSauMoLaiScope()
    {
        using var dir = new ThuMucAnhSanTest(); using var services = Provider(AnhSanTest.Kho(dir.Root));
        var (id, cu) = await TaoSan(services);
        using (var scope = services.CreateScope())
        using (var locked = new FileStream(Path.Combine(dir.Root, cu), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var bytes = AnhSanTest.Tep(); using var input = new MemoryStream(bytes);
            var result = await Service(scope.ServiceProvider).CapNhatAnhAsync(id,
                new(ThaoTacAnh.ThayAnh, cu, new(input, "a.png", bytes.Length)), default);
            Assert.Equal(MaLoiAnh.ThanhCong, result.MaLoi); Assert.True(result.CanDonTep);
        }
        using var read = fixture.TaoDb(); var key = (await read.SanTheThaos.FindAsync(id))!.DuongDanAnh;
        Assert.NotEqual(cu, key);
        var khoMoi = AnhSanTest.Kho(dir.Root); using var image = await khoMoi.MoDocAsync(key!, default);
        Assert.NotNull(image); Assert.True(image.Length > 0);
    }

    private sealed class LuuLoi(IDonViCongViec inner, bool daCommit) : IDonViCongViec
    {
        public async Task LuuAsync(CancellationToken token)
        {
            if (daCommit) await inner.LuuAsync(token);
            throw new IOException("Lỗi commit mô phỏng.");
        }
        public Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> action,
            IsolationLevel level, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class HaiNguoiCungLuu : SaveChangesInterceptor
    {
        public bool Bat { get; set; }
        private int count;
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Bat)
            {
                if (Interlocked.Increment(ref count) == 2) gate.TrySetResult();
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
            }
            return result;
        }
    }
}

// M4: kiểm thử bằng SQL Server thật trên database riêng; Codex hỗ trợ.
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class DongHoTest : IDongHo
{
    public DateTime BayGio => new(2030, 1, 2, 8, 0, 0);
    public DateOnly HomNay => DateOnly.FromDateTime(BayGio);
}

public sealed class SqlFixture : IAsyncLifetime
{
    private readonly string database = "QuanLyDatSan_M4Test_" + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; }
    public DongHoTest Clock { get; } = new();
    public string Password { get; } = "M4-Test-" + Guid.NewGuid().ToString("N");
    public int CustomerId { get; private set; }
    public int CustomerAccountId { get; private set; }
    public int OtherCustomerAccountId { get; private set; }

    public SqlFixture()
    {
        ConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = Environment.GetEnvironmentVariable("M4_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB",
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 15
        }.ConnectionString;
    }

    public ApplicationDbContext Db(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString)
            .AddInterceptors(interceptors).Options);
    public DatSanService Service(ApplicationDbContext db) => new(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan.DatSanRepository(db), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(db), Clock, new QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien.TinhTienService());

    public async Task InitializeAsync()
    {
        await using var db = Db();
        // Kiểm thử schema model hiện hành; không áp dụng migration vào DB người dùng.
        await db.Database.EnsureCreatedAsync();
        var hasher = new PasswordHasher<TaiKhoan>();
        foreach (var (name, role) in new[] { ("admin", VaiTro.Admin), ("staff", VaiTro.NhanVien),
                     ("customer", VaiTro.KhachHang), ("other", VaiTro.KhachHang) })
        {
            var account = new TaiKhoan { TenDangNhap = name, HoTen = name, Email = name + "@example.test", VaiTro = role };
            account.MatKhau = hasher.HashPassword(account, Password);
            if (role == VaiTro.KhachHang)
                account.KhachHang = new KhachHang
                {
                    HoTen = name,
                    Email = account.Email,
                    SoDienThoai = "0900000000",
                    NgayDangKy = Clock.BayGio
                };
            db.Add(account);
            await db.SaveChangesAsync();
            if (name == "customer") { CustomerId = account.KhachHang!.MaKhachHang; CustomerAccountId = account.MaTaiKhoan; }
            if (name == "other") OtherCustomerAccountId = account.MaTaiKhoan;
        }
    }

    public async Task<DatSan> Booking(TrangThaiDatSan state = TrangThaiDatSan.ChoXuLy,
        int startHours = 2, decimal deposit = 0)
    {
        await using var db = Db();
        var court = new SanTheThao
        {
            TenSan = "M4 " + Guid.NewGuid().ToString("N"),
            MaLoaiSan = 1,
            DiaChi = "Test",
            DonGia = 200000m,
            GioMoCua = new(6, 0),
            GioDongCua = new(22, 0),
            TrangThai = TrangThaiSan.HoatDong
        };
        var order = new DatSan
        {
            MaKhachHang = CustomerId,
            SanTheThao = court,
            NgayDat = Clock.BayGio,
            GioBatDau = Clock.BayGio.AddHours(startHours),
            GioKetThuc = Clock.BayGio.AddHours(startHours + 1),
            DonGia = court.DonGia,
            TienCoc = deposit,
            TrangThai = state
        };
        db.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    public async Task<DatSan> Read(int id)
    {
        await using var db = Db();
        return await db.DatSans.AsNoTracking().SingleAsync(x => x.MaDatSan == id);
    }

    public async Task DisposeAsync()
    {
        // Tên ngẫu nhiên được sinh nội bộ; không bao giờ xóa QuanLyDatSan_Local.
        if (new SqlConnectionStringBuilder(ConnectionString).InitialCatalog != database
            || !database.StartsWith("QuanLyDatSan_M4Test_", StringComparison.Ordinal))
            throw new InvalidOperationException("Database cleanup guard failed.");
        await using var db = Db();
        await db.Database.EnsureDeletedAsync();
    }
}

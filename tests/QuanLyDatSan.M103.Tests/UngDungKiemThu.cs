// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử HTTP với cookie/Session thật.
using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan.M103.Tests;

public sealed class UngDungKiemThu : WebApplicationFactory<TaiKhoanController>
{
    private readonly SqliteConnection connection;
    private readonly string connectionString = $"Data Source=m103_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    public string MatKhau { get; } = Guid.NewGuid().ToString("N") + "!aA1";

    public UngDungKiemThu()
    {
        connection = MoKetNoi();
        // Giữ kết nối gốc để database RAM sống trong suốt test.
        // Mỗi request có kết nối riêng, tránh dùng đồng thời một SqliteConnection khi tải CSS/JS.
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE TaiKhoan (
                MaTaiKhoan INTEGER PRIMARY KEY AUTOINCREMENT,
                TenDangNhap TEXT NOT NULL COLLATE Vietnamese_100_CI_AS UNIQUE,
                MatKhau TEXT NOT NULL, HoTen TEXT NOT NULL, Email TEXT NOT NULL,
                VaiTro INTEGER NOT NULL CHECK(VaiTro IN (1,2,3)),
                TrangThai INTEGER NOT NULL CHECK(TrangThai IN (0,1))
            );
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection MoKetNoi()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.CreateCollation(ApplicationDbContext.CollationTen, (a, b) =>
            CultureInfo.GetCultureInfo("vi-VN").CompareInfo.Compare(a, b, CompareOptions.IgnoreCase));
        return connection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddScoped<SqliteConnection>(_ => MoKetNoi());
            services.AddDbContext<ApplicationDbContext>((provider, options) =>
                options.UseSqlite(provider.GetRequiredService<SqliteConnection>()));
            // Không ghi khóa Data Protection ra máy; cookie/antiforgery vẫn đi qua middleware thật.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    public async Task KhoiTaoAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>();
        foreach (var (id, ten, vaiTro) in new[]
        {
            (1, "admin", VaiTro.Admin), (2, "nhanvien", VaiTro.NhanVien), (3, "khachhang", VaiTro.KhachHang)
        })
        {
            var tk = new TaiKhoan { MaTaiKhoan = id, TenDangNhap = ten, HoTen = ten, Email = ten + "@example.test", VaiTro = vaiTro };
            tk.MatKhau = hasher.HashPassword(tk, MatKhau);
            db.TaiKhoans.Add(tk);
        }
        await db.SaveChangesAsync();
    }

    public HttpClient TaoClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost")
    });

    public async Task<TaiKhoan> DocTaiKhoanAsync(int id)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .TaiKhoans.AsNoTracking().SingleAsync(x => x.MaTaiKhoan == id);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

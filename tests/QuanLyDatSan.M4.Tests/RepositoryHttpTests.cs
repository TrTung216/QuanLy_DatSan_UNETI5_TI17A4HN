// Codex: hồi quy các controller được chuyển khỏi DbContext trong đợt refactor.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class RepositoryHttpTests(SqlFixture fixture) : IClassFixture<SqlFixture>
{
    [Fact]
    public async Task DangKyGiuMatKhauTamKyTuTaoCaHoSoVaKhongNhanRoleTuForm()
    {
        using var app = new App(fixture); using var client = app.Client();
        var ten = "khach_" + Guid.NewGuid().ToString("N");
        var form = new Dictionary<string, string>
        {
            ["TenDangNhap"] = ten,
            ["MatKhau"] = "Khach123",
            ["XacNhanMatKhau"] = "Khach123",
            ["HoTen"] = "Khách mới",
            ["Email"] = "khach@example.test",
            ["SoDienThoai"] = "0912345678",
            ["GioiTinh"] = "0",
            ["VaiTro"] = "1"
        };
        // Không cần biết giá trị enum của KhongKhaiBao ở client test.
        form["GioiTinh"] = ((int)GioiTinh.KhongKhaiBao).ToString();
        var response = await Post(client, "/KhachHang/DangKy", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/TaiKhoan/DangNhap", response.Headers.Location?.OriginalString);
        await using var db = fixture.Db();
        var tk = await db.TaiKhoans.Include(x => x.KhachHang).SingleAsync(x => x.TenDangNhap == ten);
        Assert.Equal(VaiTro.KhachHang, tk.VaiTro);
        Assert.NotNull(tk.KhachHang);
        Assert.Equal(tk.HoTen, tk.KhachHang.HoTen);
        Assert.Equal(tk.Email, tk.KhachHang.Email);
        var trung = await Post(client, "/KhachHang/DangKy", form);
        Assert.Contains("Tên đăng nhập đã được sử dụng.", WebUtility.HtmlDecode(await trung.Content.ReadAsStringAsync()));
        await Login(client, ten, "Khach123");
        Assert.Contains("Khách mới", WebUtility.HtmlDecode(await client.GetStringAsync("/KhachHang/TrangChu")));
    }

    [Fact]
    public async Task DichVuQuaServiceGiuFormThongBaoVaAntiforgery()
    {
        using var app = new App(fixture); using var client = app.Client();
        await Login(client, "admin", fixture.Password);
        var ten = "Dịch vụ " + Guid.NewGuid().ToString("N");
        var form = new Dictionary<string, string> { ["TenDichVu"] = ten, ["DonViTinh"] = "Chai", ["DonGia"] = "15000" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/DichVu/Tao", new FormUrlEncodedContent(form))).StatusCode);
        var response = await Post(client, "/DichVu/Tao", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/DichVu/DanhSach", response.Headers.Location?.OriginalString);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/DichVu/DanhSach?tuKhoa=" + Uri.EscapeDataString(ten)));
        Assert.Contains(ten, html);
        Assert.Contains("Đã thêm dịch vụ.", html);
        await using var db = fixture.Db();
        var dv = await db.DichVus.SingleAsync(x => x.TenDichVu == ten);
        var edit = new Dictionary<string, string>(form) { ["MaDichVu"] = dv.MaDichVu.ToString(), ["DonGia"] = "20000" };
        response = await Post(client, "/DichVu/Sua/" + dv.MaDichVu, edit);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await db.Entry(dv).ReloadAsync();
        Assert.Equal(20000m, dv.DonGia);
    }

    [Fact]
    public async Task DieuHanhQuaServiceTraDuLieuThatVaDiScopedKhongLap()
    {
        using var app = new App(fixture); using var client = app.Client();
        await Login(client, "staff", fixture.Password);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/QuanTri")).StatusCode);
        using var scope = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDieuHanhService>();
        var duLieu = await service.LaySoLieuAsync(default);
        await using var db = fixture.Db();
        Assert.Equal(await db.KhachHangs.CountAsync(x => x.TrangThai == TrangThaiKhachHang.HoatDong), duLieu.SoKhachHangHoatDong);
        Assert.Single(scope.ServiceProvider.GetServices<IDonViCongViec>());
        Assert.Same(scope.ServiceProvider.GetRequiredService<IDonViCongViec>(), scope.ServiceProvider.GetRequiredService<IDonViCongViec>());
        Assert.NotSame(scope.ServiceProvider.GetRequiredService<IDonViCongViec>(), scope2.ServiceProvider.GetRequiredService<IDonViCongViec>());
        Assert.NotEqual(HttpStatusCode.OK, (await client.GetAsync("/DichVu/Tao")).StatusCode);
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(path);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        var form = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value) };
        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    private static async Task Login(HttpClient client, string ten, string matKhau)
    {
        var response = await Post(client, "/TaiKhoan/DangNhap", new() { ["TenDangNhap"] = ten, ["MatKhau"] = matKhau });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed class App(SqlFixture f) : WebApplicationFactory<QuanTriController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(Path.GetFullPath("../../../../../QuanLyDatSan_UNETI5_DHTI17A4HN", AppContext.BaseDirectory));
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(f.ConnectionString));
                services.RemoveAll<IDongHo>(); services.AddSingleton<IDongHo>(f.Clock);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    }
}

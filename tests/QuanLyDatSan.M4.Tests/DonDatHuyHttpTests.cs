// M3: Nguyễn Văn Quý; MSSV: 23103100181. Kiểm thử HTTP thực tế cho khách hủy đơn chờ (SQL Server thật, cookie, antiforgery).
// Dùng lại SqlFixture của M4; không sửa file của M4.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class DonDatHuyHttpTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    private const string LyDo = "Lý do thử nghiệm";

    private sealed class App(SqlFixture fixture) : WebApplicationFactory<DonDatController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(Path.GetFullPath("../../../../../QuanLyDatSan_UNETI5_DHTI17A4HN", AppContext.BaseDirectory));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(fixture.ConnectionString));
                services.RemoveAll<IDongHo>(); services.AddSingleton<IDongHo>(fixture.Clock);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    }

    private async Task Login(HttpClient client, string user)
    {
        var token = Token(await client.GetStringAsync("/TaiKhoan/DangNhap"));
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["TenDangNhap"] = user, ["MatKhau"] = f.Password, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Không tìm thấy antiforgery token trong trang.");
        return LamSachHtml(match.Groups[1].Value);
    }
    private static string LamSachHtml(string s) => WebUtility.HtmlDecode(s);

    private static string PhienBanTrongTrang(string html)
    {
        var match = Regex.Match(html, "name=\"PhienBan\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Không tìm thấy form hủy đơn trong trang chi tiết.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent Form(int maDon, string phienBan, string token, string lyDo = LyDo) =>
        new(new Dictionary<string, string>
        { ["MaDon"] = maDon.ToString(), ["PhienBan"] = phienBan, ["LyDo"] = lyDo, ["__RequestVerificationToken"] = token });

    [Fact]
    public async Task KhachHuyDonChoHopLeThanhCongVaLuuLyDo()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");
        Assert.Contains("Hủy đơn", WebUtility.HtmlDecode(html));

        var response = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, PhienBanTrongTrang(html), Token(html)));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/DonDat/ChiTiet/{don.MaDatSan}", response.Headers.Location!.OriginalString);
        var daLuu = await f.Read(don.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DaHuy, daLuu.TrangThai);
        Assert.Equal(LyDo, daLuu.LyDoHuy);
        Assert.Contains("Đã hủy đơn", WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location)));
    }

    [Fact]
    public async Task ThieuAntiforgeryTokenBiTuChoiVaKhongDoiTrangThai()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["MaDon"] = don.MaDatSan.ToString(), ["PhienBan"] = PhienBanTrongTrang(html), ["LyDo"] = LyDo });

        var response = await client.PostAsync("/DonDat/Huy", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task PhienBanBase64HongBaoLoiVaKhongDoiTrangThai()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");

        var response = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, "@@khong-phai-base64@@", Token(html)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Phiên bản đơn không hợp lệ", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task PhienBanSaiKhongDoiTrangThaiVaGiuLaiLyDoDaNhap()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");
        var phienBanSai = Convert.ToBase64String(new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 });

        var response = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, phienBanSai, Token(html)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(LyDo, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task ThieuLyDoBaoLoiFormVaKhongDoiTrangThai()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");

        var response = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, PhienBanTrongTrang(html), Token(html), lyDo: ""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vui lòng nhập lý do hủy.", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task KhachKhacKhongTheHuyDonCuaNguoiKhac()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "other");
        var token = Token(await client.GetStringAsync("/DonDat/Tao"));
        var phienBanThat = Convert.ToBase64String((await f.Read(don.MaDatSan)).RowVersion);

        var response = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, phienBanThat, token));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task GuiLapKhongGayLoiVaKhongGhiDeNgayHuy()
    {
        var don = await f.Booking();
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");
        var phienBan = PhienBanTrongTrang(html); var token = Token(html);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, phienBan, token))).StatusCode);
        var sauLanDau = await f.Read(don.MaDatSan);

        var lanHai = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, phienBan, token, "Lý do khác"));

        Assert.NotEqual(HttpStatusCode.InternalServerError, lanHai.StatusCode);
        var sauLanHai = await f.Read(don.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DaHuy, sauLanHai.TrangThai);
        Assert.Equal(LyDo, sauLanHai.LyDoHuy);
        Assert.Equal(sauLanDau.NgayHuy, sauLanHai.NgayHuy);
    }

    [Fact]
    public async Task DonDaQuaGioBatDauKhongCoNutHuyVaServiceTuChoiPost()
    {
        var don = await f.Booking(startHours: -1);
        using var host = new App(f); using var client = host.Client();
        await Login(client, "customer");
        var html = await client.GetStringAsync($"/DonDat/ChiTiet/{don.MaDatSan}");
        Assert.DoesNotContain("name=\"PhienBan\"", html);
        var token = Token(await client.GetStringAsync("/DonDat/Tao"));
        var phienBan = Convert.ToBase64String((await f.Read(don.MaDatSan)).RowVersion);

        var response = await client.PostAsync("/DonDat/Huy", Form(don.MaDatSan, phienBan, token));

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(don.MaDatSan)).TrangThai);
    }
}

// Phan Giang Tâm - 23103100196. M2: Kiểm thử HTTP đặt sân sau tra cứu với database SQL Server riêng.
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

[Trait("Category", "SqlServer")]
public class LuongDatSanSqlTests(TraCuuSanSqlServerFixture fixture) : IClassFixture<TraCuuSanSqlServerFixture>
{
    private sealed class App(string connectionString) : WebApplicationFactory<DonDatController>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureLogging(logging => logging.ClearProviders());
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }

        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private sealed record DuLieuThu(int MaSan, string TenSan, string TenDangNhap, string MatKhau);

    private async Task<DuLieuThu> TaoDuLieuAsync()
    {
        await using var db = fixture.TaoDb();
        var nhan = Guid.NewGuid().ToString("N");
        var matKhau = Guid.NewGuid().ToString("N") + "!aA1";
        var taiKhoan = new TaiKhoan
        {
            TenDangNhap = "m2_" + nhan, HoTen = "Khách kiểm thử", Email = nhan + "@example.test",
            VaiTro = VaiTro.KhachHang
        };
        taiKhoan.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(taiKhoan, matKhau);
        db.KhachHangs.Add(new KhachHang
        {
            TaiKhoan = taiKhoan, HoTen = taiKhoan.HoTen, Email = taiKhoan.Email,
            SoDienThoai = "0901234567", NgayDangKy = new(2026, 1, 1)
        });
        var san = new SanTheThao
        {
            TenSan = "M2 luồng " + nhan, MaLoaiSan = 1, DiaChi = "Hà Nội",
            DonGia = 200000, DonGiaCaoDiem = 350000, DonGiaCuoiTuan = 450000,
            GioMoCua = new(6, 0), GioDongCua = new(22, 0), TrangThai = TrangThaiSan.HoatDong
        };
        db.SanTheThaos.Add(san);
        await db.SaveChangesAsync();
        return new(san.MaSan, san.TenSan, taiKhoan.TenDangNhap, matKhau);
    }

    private App TaoApp()
    {
        using var db = fixture.TaoDb();
        return new(db.Database.GetConnectionString()!);
    }

    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task DangNhapAsync(HttpClient client, DuLieuThu duLieu)
    {
        var trang = await client.GetStringAsync("/TaiKhoan/DangNhap");
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = duLieu.TenDangNhap, ["MatKhau"] = duLieu.MatKhau,
            ["__RequestVerificationToken"] = Token(trang)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static FormUrlEncodedContent Form(int maSan, string ngay, string batDau, string ketThuc, string token)
        => new(new Dictionary<string, string>
        {
            ["MaSan"] = maSan.ToString(CultureInfo.InvariantCulture), ["NgaySuDung"] = ngay,
            ["GioBatDau"] = batDau, ["GioKetThuc"] = ketThuc, ["__RequestVerificationToken"] = token,
            // Giá gửi thêm từ client phải bị bỏ qua, đơn giá thật lấy từ server.
            ["DonGia"] = "1", ["TongTien"] = "1"
        });

    [M2SqlServerTheory]
    [InlineData("2100-01-04", "10:00", "11:00")]
    [InlineData("2100-01-04", "18:00", "19:00")]
    [InlineData("2100-01-09", "18:00", "19:00")]
    public async Task TraCuuDenDatDonDungSanNgayGioVaGiaChinhThucV1(string ngay, string batDau, string ketThuc)
    {
        var duLieu = await TaoDuLieuAsync();
        using var app = TaoApp();
        using var client = app.Client();
        await DangNhapAsync(client, duLieu);
        var danhSach = await client.GetStringAsync("/SanTheThao?tuKhoa=" + Uri.EscapeDataString(duLieu.TenSan)
            + $"&ngaySuDung={ngay}&gioBatDau={batDau}&gioKetThuc={ketThuc}");
        Assert.Contains(duLieu.TenSan, WebUtility.HtmlDecode(danhSach));
        var chiTiet = await client.GetStringAsync($"/SanTheThao/Details/{duLieu.MaSan}?ngay={ngay}&batDau={batDau}&ketThuc={ketThuc}");
        var match = Regex.Match(chiTiet, "href=\"([^\"]+)\"[^>]*>Đặt sân</a>");
        Assert.True(match.Success);
        var trangDat = await client.GetStringAsync(WebUtility.HtmlDecode(match.Groups[1].Value));
        Assert.Contains("value=\"" + ngay + "\"", trangDat);
        await using (var db = fixture.TaoDb())
            Assert.Equal(0, await db.DatSans.CountAsync(don => don.MaSan == duLieu.MaSan));

        var response = await client.PostAsync("/DonDat/Tao", Form(duLieu.MaSan, ngay, batDau, ketThuc, Token(trangDat)));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/DonDat/ChiTiet/", response.Headers.Location!.OriginalString);
        await using (var db = fixture.TaoDb())
        {
            var don = await db.DatSans.AsNoTracking().SingleAsync(x => x.MaSan == duLieu.MaSan);
            Assert.Equal(DateOnly.ParseExact(ngay, "yyyy-MM-dd").ToDateTime(TimeOnly.Parse(batDau)), don.GioBatDau);
            Assert.Equal(DateOnly.ParseExact(ngay, "yyyy-MM-dd").ToDateTime(TimeOnly.Parse(ketThuc)), don.GioKetThuc);
            Assert.Equal(200000m, don.DonGia);
            Assert.Equal(TrangThaiDatSan.ChoXuLy, don.TrangThai);
            // Đổi giá danh mục sau đặt không làm thay đổi giá snapshot trên đơn.
            var san = await db.SanTheThaos.SingleAsync(x => x.MaSan == duLieu.MaSan);
            san.DonGia = 900000;
            await db.SaveChangesAsync();
        }
        var daDat = WebUtility.HtmlDecode(await client.GetStringAsync(response.Headers.Location));
        Assert.Contains("200.000", daDat);
        Assert.DoesNotContain("900.000", daDat);
        var sauDat = WebUtility.HtmlDecode(await client.GetStringAsync("/SanTheThao?tuKhoa="
            + Uri.EscapeDataString(duLieu.TenSan) + $"&ngaySuDung={ngay}&gioBatDau={batDau}&gioKetThuc={ketThuc}"));
        Assert.Contains("Không có sân phù hợp", sauDat);
    }

    [M2SqlServerFact]
    public async Task LichDaBiDatSauTraCuuBaoLoiVaKhongGhiThemDon()
    {
        var duLieu = await TaoDuLieuAsync();
        using var app = TaoApp();
        using var client = app.Client();
        await DangNhapAsync(client, duLieu);
        var trang = await client.GetStringAsync($"/DonDat/Tao?maSan={duLieu.MaSan}&ngaySuDung=2100-01-04&gioBatDau=10:00&gioKetThuc=11:00");
        var token = Token(trang);
        var thanhCong = await client.PostAsync("/DonDat/Tao", Form(duLieu.MaSan, "2100-01-04", "10:00", "11:00", token));
        Assert.Equal(HttpStatusCode.Redirect, thanhCong.StatusCode);
        var trung = await client.PostAsync("/DonDat/Tao", Form(duLieu.MaSan, "2100-01-04", "10:00", "11:00", token));
        Assert.Equal(HttpStatusCode.OK, trung.StatusCode);
        var html = WebUtility.HtmlDecode(await trung.Content.ReadAsStringAsync());
        Assert.Contains("Khung giờ này đã có người đặt", html);
        Assert.Contains("value=\"2100-01-04\"", html);
        await using var db = fixture.TaoDb();
        Assert.Equal(1, await db.DatSans.CountAsync(x => x.MaSan == duLieu.MaSan));
    }
}

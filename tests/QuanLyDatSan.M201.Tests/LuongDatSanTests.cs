// Phan Giang Tâm - 23103100196. M2: Kiểm thử chuyển từ tra cứu sang đặt sân và giữ ngày giờ qua đăng nhập.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

public partial class TrangThaiSanTests
{
    private static string LienKet(string html, string nhan)
    {
        var link = Regex.Match(html, "href=\"([^\"]+)\"[^>]*>" + Regex.Escape(nhan) + "</a>");
        Assert.True(link.Success, "Không tìm thấy liên kết " + nhan);
        return WebUtility.HtmlDecode(link.Groups[1].Value);
    }

    [Fact]
    public async Task TraCuuChiTietDangNhapVaFormDatGiuDungSanNgayGio()
    {
        using var app = await TaoAppKhaDungAsync();
        using var client = app.TaoClient();
        var danhSach = await client.GetStringAsync("/SanTheThao?tuKhoa=Alpha&" + GioTraCuu);
        var chiTiet = await client.GetStringAsync(LienKet(danhSach, "Xem"));
        var datSan = LienKet(chiTiet, "Đặt sân");
        Assert.Contains("maSan=3", datSan);
        Assert.Contains("ngaySuDung=2100-01-04", datSan);
        Assert.Contains("gioBatDau=10", datSan);
        Assert.Contains("gioKetThuc=11", datSan);

        var challenge = await client.GetAsync(datSan);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var urlDangNhap = challenge.Headers.Location!;
        var htmlDangNhap = await client.GetStringAsync(urlDangNhap);
        var returnUrl = Regex.Match(htmlDangNhap, "name=\"ReturnUrl\"[^>]*value=\"([^\"]*)\"");
        Assert.True(returnUrl.Success);
        var dangNhap = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = "khachhang", ["MatKhau"] = app.MatKhau,
            ["ReturnUrl"] = WebUtility.HtmlDecode(returnUrl.Groups[1].Value),
            ["__RequestVerificationToken"] = Token(htmlDangNhap)
        }));
        Assert.Equal(HttpStatusCode.Redirect, dangNhap.StatusCode);
        Assert.Equal(datSan, dangNhap.Headers.Location!.OriginalString);
        var form = await client.GetStringAsync(dangNhap.Headers.Location);
        Assert.Matches("<option(?=[^>]*value=\"3\")(?=[^>]*selected)[^>]*>", form);
        Assert.Contains("value=\"2100-01-04\"", form);
        Assert.Contains("value=\"10:00\"", form);
        Assert.Contains("value=\"11:00\"", form);
        Assert.Contains("giá giờ thường tại thời điểm đặt", WebUtility.HtmlDecode(form));
        var xemLai = LienKet(form, "Xem lại sân");
        Assert.Contains("ngay=2100-01-04", xemLai);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(xemLai)).StatusCode);
        var veDanhSach = LienKet(chiTiet, "Về danh sách sân");
        Assert.Contains("ngaySuDung=2100-01-04", veDanhSach);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(veDanhSach)).StatusCode);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("nhanvien")]
    public async Task TaiKhoanQuanLyKhongCoNutDatCuaKhach(string ten)
    {
        using var app = await TaoAppKhaDungAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, ten);
        var html = await client.GetStringAsync("/SanTheThao/Details/3");
        Assert.DoesNotContain(">Đặt sân</a>", html);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/DonDat/Tao?maSan=3")).StatusCode);
    }

    [Theory]
    [InlineData("ngaySuDung=2100-01-04")]
    [InlineData("ngaySuDung=loi&gioBatDau=10:00&gioKetThuc=11:00")]
    [InlineData("ngaySuDung=2100-01-04&gioBatDau=loi&gioKetThuc=11:00")]
    public async Task ThamSoChuyenTiepSaiHienLoiVaKhongTaoDon(string query)
    {
        using var app = await TaoAppKhaDungAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "khachhang");
        var response = await client.GetAsync("/DonDat/Tao?maSan=3&" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Nhập đủ ngày sử dụng", html);
        using var scope = app.Services.CreateScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().DatSans.CountAsync());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(999999)]
    public async Task SanBiAnHoacKhongTonTaiKhongDuocChonSanTrongForm(int maSan)
    {
        using var app = await TaoAppKhaDungAsync();
        using var client = app.TaoClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/SanTheThao/Details/{maSan}")).StatusCode);
        await DangNhapAsync(app, client, "khachhang");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/DonDat/Tao?maSan={maSan}"));
        Assert.Contains("Sân đã ngừng nhận đặt hoặc không còn tồn tại", html);
        Assert.DoesNotContain(">Xem lại sân</a>", html);
    }

    [Fact]
    public async Task ChiTietThieuHoacSaiKhungGioChiChuyenMaSan()
    {
        using var app = await TaoAppKhaDungAsync();
        using var client = app.TaoClient();
        foreach (var query in new[] { "", "?ngay=2100-01-04", "?ngay=2100-01-04&batDau=11:00&ketThuc=10:00" })
        {
            var html = await client.GetStringAsync("/SanTheThao/Details/3" + query);
            var url = LienKet(html, "Đặt sân");
            Assert.Contains("maSan=3", url);
            Assert.DoesNotContain("ngaySuDung", url);
            Assert.DoesNotContain("gioBatDau", url);
            Assert.DoesNotContain("gioKetThuc", url);
        }
    }
}

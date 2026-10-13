// Họ tên: Phan Giang Tâm; MSSV: 23103100196.
// Nội dung: Kiểm tra trạng thái bắt buộc khi thêm và sửa sân qua HTTP.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan.M103.Tests;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

public class TrangThaiSanTests
{
    private static async Task<UngDungKiemThu> TaoAppAsync()
    {
        var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Database thử trong RAM, không chạy migration trên database local của thành viên.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE LoaiSan (
                MaLoaiSan INTEGER PRIMARY KEY AUTOINCREMENT,
                TenLoai TEXT NOT NULL COLLATE Vietnamese_100_CI_AS,
                MoTa TEXT, SoNguoiToiDa INTEGER NOT NULL,
                DonGiaTheoGio TEXT NOT NULL, TrangThai INTEGER NOT NULL
            );
            CREATE TABLE SanTheThao (
                MaSan INTEGER PRIMARY KEY AUTOINCREMENT, TenSan TEXT NOT NULL,
                MaLoaiSan INTEGER NOT NULL REFERENCES LoaiSan(MaLoaiSan) ON DELETE NO ACTION,
                DiaChi TEXT NOT NULL, TienIch TEXT, DonGia TEXT NOT NULL,
                DonGiaCaoDiem TEXT, DonGiaCuoiTuan TEXT,
                TrangThai INTEGER NOT NULL CHECK(TrangThai IN (1,2,3)),
                GioMoCua TEXT NOT NULL, GioDongCua TEXT NOT NULL, GhiChu TEXT, NgayBaoTri TEXT
            );
            """);
        db.LoaiSans.Add(new LoaiSan { TenLoai = "Bóng đá 5 người", SoNguoiToiDa = 10, DonGiaTheoGio = 200000 });
        await db.SaveChangesAsync();
        foreach (var (id, ten, trangThai) in new[]
        {
            (1, "Sân bảo trì", TrangThaiSan.BaoTri),
            (2, "Sân ngừng hoạt động", TrangThaiSan.NgungHoatDong)
        })
        {
            db.SanTheThaos.Add(new SanTheThao
            {
                MaSan = id, TenSan = ten, MaLoaiSan = 1, DiaChi = "Hà Nội",
                DonGia = 200000, DonGiaCaoDiem = 350000, DonGiaCuoiTuan = 450000,
                TrangThai = trangThai, GioMoCua = new(6, 0), GioDongCua = new(22, 0)
            });
        }
        await db.SaveChangesAsync();
        return app;
    }

    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static async Task DangNhapAsync(UngDungKiemThu app, HttpClient client)
    {
        var html = await client.GetStringAsync("/TaiKhoan/DangNhap");
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = "admin", ["MatKhau"] = app.MatKhau, ["__RequestVerificationToken"] = Token(html)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static Dictionary<string, string> Form(string? trangThai)
    {
        var form = new Dictionary<string, string>
        {
            ["TenSan"] = "Sân kiểm thử", ["DiaChi"] = "Hà Nội", ["MaLoaiSan"] = "1",
            ["DonGia"] = "200000", ["DonGiaCaoDiem"] = "350000", ["DonGiaCuoiTuan"] = "450000",
            ["GioMoCua"] = "06:00", ["GioDongCua"] = "22:00"
        };
        if (trangThai is not null) form["TrangThai"] = trangThai;
        return form;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Dictionary<string, string> form)
    {
        var html = await client.GetStringAsync(path);
        form["__RequestVerificationToken"] = Token(html);
        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    private static async Task<SanTheThao> DocSanAsync(UngDungKiemThu app, int id)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SanTheThaos.AsNoTracking().SingleAsync(x => x.MaSan == id);
    }

    private static void KiemTraTrangThaiDuocChon(string html, int trangThai)
    {
        var select = Regex.Match(html, "<select[^>]*name=\"TrangThai\"[^>]*>([\\s\\S]*?)</select>");
        Assert.True(select.Success);
        Assert.Matches($"<option(?=[^>]*value=\"{trangThai}\")(?=[^>]*selected)[^>]*>", select.Groups[1].Value);
    }

    [Fact]
    public async Task FormThemChonSanHoatDong()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        KiemTraTrangThaiDuocChon(await client.GetStringAsync("/SanTheThao/Create"), 1);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task FormSuaLayDungTrangThaiDangLuu(int id, int trangThai)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        KiemTraTrangThaiDuocChon(await client.GetStringAsync($"/SanTheThao/Edit/{id}"), trangThai);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("99")]
    [InlineData("abc")]
    public async Task KhongThemSanKhiThieuHoacSaiTrangThai(string? trangThai)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        var response = await PostAsync(client, "/SanTheThao/Create", Form(trangThai));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("field-validation-error", html);
        Assert.Contains("-- Chọn trạng thái --", WebUtility.HtmlDecode(html));
        using var scope = app.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SanTheThaos.CountAsync());
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(1, "")]
    [InlineData(1, "0")]
    [InlineData(1, "99")]
    [InlineData(1, "abc")]
    [InlineData(2, null)]
    [InlineData(2, "")]
    [InlineData(2, "0")]
    [InlineData(2, "99")]
    [InlineData(2, "abc")]
    public async Task SuaSaiTrangThaiKhongDoiDuLieuCu(int id, string? trangThai)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        var before = await DocSanAsync(app, id);
        var form = Form(trangThai); form["MaSan"] = id.ToString();
        var response = await PostAsync(client, $"/SanTheThao/Edit/{id}", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("field-validation-error", html);
        Assert.Contains("-- Chọn trạng thái --", WebUtility.HtmlDecode(html));
        var after = await DocSanAsync(app, id);
        Assert.Equal(before.TrangThai, after.TrangThai);
        Assert.Equal(before.TenSan, after.TenSan);
        Assert.Equal(before.DonGiaCaoDiem, after.DonGiaCaoDiem);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public async Task ThemSanVoiTrangThaiHopLe(string trangThai)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, "/SanTheThao/Create", Form(trangThai))).StatusCode);
        using var scope = app.Services.CreateScope();
        var san = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SanTheThaos.SingleAsync(x => x.TenSan == "Sân kiểm thử");
        Assert.Equal((TrangThaiSan)int.Parse(trangThai), san.TrangThai);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    public async Task SuaSanVoiTrangThaiHopLe(string trangThai)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        var form = Form(trangThai); form["MaSan"] = "1";
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, "/SanTheThao/Edit/1", form)).StatusCode);
        Assert.Equal((TrangThaiSan)int.Parse(trangThai), (await DocSanAsync(app, 1)).TrangThai);
    }
}

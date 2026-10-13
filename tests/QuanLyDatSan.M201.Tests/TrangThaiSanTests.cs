// Họ tên: Phan Giang Tâm; MSSV: 23103100196.
// Nội dung: Kiểm tra trạng thái, quyền xem sân và phân trang qua HTTP.
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

public partial class TrangThaiSanTests
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

    private static async Task DangNhapAsync(UngDungKiemThu app, HttpClient client, string tenDangNhap = "admin")
    {
        var html = await client.GetStringAsync("/TaiKhoan/DangNhap");
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = tenDangNhap, ["MatKhau"] = app.MatKhau, ["__RequestVerificationToken"] = Token(html)
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

    private static async Task ThemDuLieuDanhSachAsync(UngDungKiemThu app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.LoaiSans.Add(new LoaiSan
        {
            MaLoaiSan = 2, TenLoai = "Loại sân ngừng hoạt động", SoNguoiToiDa = 10,
            DonGiaTheoGio = 200000, TrangThai = TrangThaiLoaiSan.NgungHoatDong
        });
        await db.SaveChangesAsync();
        for (var i = 1; i <= 13; i++)
        {
            db.SanTheThaos.Add(new SanTheThao
            {
                MaSan = i + 2,
                TenSan = i <= 12 ? $"Sân hoạt động {i:00}" : "Sân thuộc loại ngừng hoạt động",
                MaLoaiSan = i <= 12 ? 1 : 2, DiaChi = "Hà Nội", DonGia = 200000,
                DonGiaCaoDiem = 350000, DonGiaCuoiTuan = 450000,
                TrangThai = TrangThaiSan.HoatDong,
                GioMoCua = new(6, 0), GioDongCua = new(22, 0)
            });
        }
        await db.SaveChangesAsync();
    }

    private static string[] TenSanTrongBang(string html)
        => Regex.Matches(html, "<th scope=\"row\">([^<]*)</th>")
            .Select(x => WebUtility.HtmlDecode(x.Groups[1].Value)).ToArray();

    [Theory]
    [InlineData(null, false)]
    [InlineData("khachhang", false)]
    [InlineData("admin", true)]
    [InlineData("nhanvien", true)]
    public async Task DanhSachVaHaiTrangGiuDungQuyenXem(string? taiKhoan, bool laQuanLy)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuDanhSachAsync(app);
        if (taiKhoan is not null) await DangNhapAsync(app, client, taiKhoan);

        // Tham số URL không được nâng quyền xem của khách.
        var trang1 = await client.GetStringAsync("/SanTheThao?trang=1&laQuanLy=true");
        var trang2 = await client.GetStringAsync("/SanTheThao?trang=2&laQuanLy=true");
        var tenTrang1 = TenSanTrongBang(trang1);
        var tenTrang2 = TenSanTrongBang(trang2);
        var tatCaTen = tenTrang1.Concat(tenTrang2).ToArray();

        Assert.Equal(10, tenTrang1.Length);
        Assert.Equal(laQuanLy ? 5 : 2, tenTrang2.Length);
        Assert.Equal(tatCaTen.Length, tatCaTen.Distinct().Count());
        for (var i = 1; i <= 12; i++) Assert.Contains($"Sân hoạt động {i:00}", tatCaTen);
        foreach (var tenAn in new[] { "Sân bảo trì", "Sân ngừng hoạt động", "Sân thuộc loại ngừng hoạt động" })
            Assert.Equal(laQuanLy, tatCaTen.Contains(tenAn));
        Assert.Contains("Trang 1 / 2", trang1);
        Assert.Contains("Trang 2 / 2", trang2);
        Assert.Equal(laQuanLy, trang1.Contains("/SanTheThao/Edit/"));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("khachhang", false)]
    [InlineData("admin", true)]
    [InlineData("nhanvien", true)]
    public async Task ChiTietGiuQuyenXemVaTinhGia(string? taiKhoan, bool laQuanLy)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuDanhSachAsync(app);
        if (taiKhoan is not null) await DangNhapAsync(app, client, taiKhoan);
        foreach (var id in new[] { 1, 2, 15 })
        {
            var response = await client.GetAsync($"/SanTheThao/Details/{id}?laQuanLy=true");
            Assert.Equal(laQuanLy ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/SanTheThao/Details/999")).StatusCode);

        var html = WebUtility.HtmlDecode(await client.GetStringAsync(
            "/SanTheThao/Details/3?ngay=2026-10-12&batDau=17:00&ketThuc=19:00"));
        Assert.Contains("Sân hoạt động 01", html);
        Assert.Contains("Bóng đá 5 người", html);
        Assert.Contains("550.000", html);
    }

    [Theory]
    [InlineData(-3, 1, 10)]
    [InlineData(999, 2, 2)]
    public async Task TrangNgoaiPhamViDuocDuaVeTrangHopLe(int trang, int trangDung, int soSan)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuDanhSachAsync(app);
        var html = await client.GetStringAsync($"/SanTheThao?trang={trang}");
        Assert.Contains($"Trang {trangDung} / 2", html);
        Assert.Equal(soSan, TenSanTrongBang(html).Length);
    }

    [Fact]
    public async Task KhachKhongCoSanDuocXemNhanTrangRong()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/SanTheThao"));
        Assert.Contains("Chưa có sân để hiển thị", html);
        Assert.Empty(TenSanTrongBang(html));
    }
}

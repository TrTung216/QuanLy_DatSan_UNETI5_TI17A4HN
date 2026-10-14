// M4-02 (#22): kiểm thử SQL Server và HTTP cho tra cứu đơn quản lý, bộ lọc và phân quyền.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class QuanLyDonDatTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    private static LocDonQuanLy Filter(string? keyword = null) =>
        new(keyword, null, null, null, null, SapXepDonQuanLy.NgayGiam, 1);

    private async Task<DatSan> Seed(string name, DateTime? start = null,
        TrangThaiDatSan state = TrangThaiDatSan.ChoXuLy, int? type = null, bool other = false)
    {
        var order = await f.Booking(state, deposit: 50000m);
        await using var db = f.Db();
        var saved = await db.DatSans.Include(x => x.SanTheThao).SingleAsync(x => x.MaDatSan == order.MaDatSan);
        saved.SanTheThao.TenSan = name;
        if (type.HasValue) saved.SanTheThao.MaLoaiSan = type.Value;
        if (other) saved.MaKhachHang = await db.KhachHangs.Where(x => x.MaTaiKhoan == f.OtherCustomerAccountId)
            .Select(x => x.MaKhachHang).SingleAsync();
        saved.GioBatDau = start ?? saved.GioBatDau;
        saved.GioKetThuc = saved.GioBatDau.AddMinutes(20);
        await db.SaveChangesAsync();
        return saved;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("customer")]
    public async Task KhachKhongVaoDuocDanhSachVaChiTiet(string? user)
    {
        var order = await f.Booking();
        using var app = new App(f); using var client = app.Client();
        if (user is not null) await Login(client, user);
        foreach (var path in new[] { "/QuanLyDonDat/DanhSach", $"/QuanLyDonDat/ChiTiet/{order.MaDatSan}" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(user is null ? "DangNhap" : "TuChoiTruyCap", response.Headers.Location!.OriginalString);
        }
        if (user is not null)
            Assert.DoesNotContain("/QuanLyDonDat/", await client.GetStringAsync("/KhachHang/TrangChu"));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("staff")]
    public async Task NhanVienVaAdminDocDonCuaMoiKhachVaMenu(string user)
    {
        var order = await Seed("Quyen " + Guid.NewGuid().ToString("N"), other: true);
        using var app = new App(f); using var client = app.Client();
        await Login(client, user);
        var list = await client.GetStringAsync("/QuanLyDonDat/DanhSach");
        Assert.Contains("Quản lý đơn đặt sân", WebUtility.HtmlDecode(list));
        Assert.Contains("ql-side-link active", list);
        Assert.Contains("/QuanLyDonDat/DanhSach", await client.GetStringAsync("/QuanTri"));
        var response = await client.GetAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var detail = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("other@example.test", detail);
        Assert.Contains("0900000000", detail);
        Assert.Contains(order.SanTheThao.TenSan, detail);
        Assert.Contains("Chưa chốt", detail);
        Assert.DoesNotContain("MatKhau", detail);
        foreach (var id in new[] { -1, 0, int.MaxValue })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/QuanLyDonDat/ChiTiet/{id}")).StatusCode);
    }

    [Fact]
    public async Task LocKetHopDungNgaySuDungBaoGomHaiBienVaLoaiSanDaNgung()
    {
        var key = Guid.NewGuid().ToString("N");
        await using var db = f.Db();
        var type = new LoaiSan { TenLoai = key, DonGiaTheoGio = 200000, SoNguoiToiDa = 10,
            TrangThai = TrangThaiLoaiSan.NgungHoatDong };
        db.Add(type); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var day = new DateTime(2031, 2, 3);
        var first = await Seed(key + " A", day, type: type.MaLoaiSan);
        var last = await Seed(key + " B", day.AddHours(23), type: type.MaLoaiSan);
        await Seed(key + " before", day.AddMinutes(-30), type: type.MaLoaiSan);
        await Seed(key + " after", day.AddDays(1), type: type.MaLoaiSan);
        await Seed(key + " wrong type", day);
        await Seed(key + " wrong state", day, TrangThaiDatSan.DaHuy, type.MaLoaiSan);
        await Seed("unrelated", day, type: type.MaLoaiSan);
        var service = f.Service(db);
        var result = await service.LayDanhSachQuanLyAsync(Filter("  " + key + "  ") with
        { MaLoaiSan = type.MaLoaiSan, TrangThai = TrangThaiDatSan.ChoXuLy,
            TuNgay = DateOnly.FromDateTime(day), DenNgay = DateOnly.FromDateTime(day) }, default);
        Assert.Equal(new[] { last.MaDatSan, first.MaDatSan }, result.Dong.Select(x => x.MaDatSan));
        Assert.Equal(2, result.TongSo);
        Assert.Contains(await service.LayLoaiSanLocDonAsync(default), x => x.MaLoaiSan == type.MaLoaiSan);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task TimTheoTenKhachVaKhongCoKetQua()
    {
        var day = new DateTime(2033, 1, 1);
        var order = await Seed("Court " + Guid.NewGuid(), day, other: true);
        await using var db = f.Db(); var service = f.Service(db);
        var result = await service.LayDanhSachQuanLyAsync(Filter("other") with
        { TuNgay = DateOnly.FromDateTime(day), DenNgay = DateOnly.FromDateTime(day) }, default);
        Assert.Contains(result.Dong, x => x.MaDatSan == order.MaDatSan);
        Assert.All(result.Dong, x => Assert.Equal("other", x.TenKhachHang));
        var empty = await service.LayDanhSachQuanLyAsync(Filter(Guid.NewGuid().ToString()) with { Trang = int.MaxValue }, default);
        Assert.Empty(empty.Dong); Assert.Equal(0, empty.TongSo); Assert.Equal(1, empty.Trang); Assert.Equal(1, empty.TongTrang);
    }

    [Theory]
    [InlineData(SapXepDonQuanLy.NgayTang)]
    [InlineData(SapXepDonQuanLy.NgayGiam)]
    [InlineData(SapXepDonQuanLy.KhachTang)]
    [InlineData(SapXepDonQuanLy.KhachGiam)]
    [InlineData(SapXepDonQuanLy.SanTang)]
    [InlineData(SapXepDonQuanLy.SanGiam)]
    public async Task SapXepOnDinhVaPhanTrang(SapXepDonQuanLy sort)
    {
        var key = Guid.NewGuid().ToString("N"); var orders = new List<DatSan>();
        for (var i = 0; i < 12; i++)
            orders.Add(await Seed(key + (i % 3), new DateTime(2032, 1, 1).AddDays(i % 4), other: i % 2 == 0));
        IOrderedEnumerable<DatSan> sorted = sort switch
        {
            SapXepDonQuanLy.NgayTang => orders.OrderBy(x => x.GioBatDau),
            SapXepDonQuanLy.NgayGiam => orders.OrderByDescending(x => x.GioBatDau),
            SapXepDonQuanLy.KhachTang => orders.OrderBy(x => x.MaKhachHang == f.CustomerId ? "customer" : "other"),
            SapXepDonQuanLy.KhachGiam => orders.OrderByDescending(x => x.MaKhachHang == f.CustomerId ? "customer" : "other"),
            SapXepDonQuanLy.SanTang => orders.OrderBy(x => x.SanTheThao.TenSan),
            _ => orders.OrderByDescending(x => x.SanTheThao.TenSan)
        };
        var expected = sorted.ThenByDescending(x => x.MaDatSan).Select(x => x.MaDatSan).ToArray();
        await using var db = f.Db(); var service = f.Service(db);
        var first = await service.LayDanhSachQuanLyAsync(Filter(key) with { SapXep = sort, Trang = -1 }, default);
        var last = await service.LayDanhSachQuanLyAsync(Filter(key) with { SapXep = sort, Trang = int.MaxValue }, default);
        Assert.Equal(12, first.TongSo); Assert.Equal(2, first.TongTrang); Assert.Equal(1, first.Trang);
        Assert.Equal(2, last.Trang); Assert.Equal(10, first.Dong.Count); Assert.Equal(2, last.Dong.Count);
        Assert.Equal(expected, first.Dong.Concat(last.Dong).Select(x => x.MaDatSan));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("TrangThai=99")]
    [InlineData("SapXep=99")]
    [InlineData("MaLoaiSan=0")]
    [InlineData("TuNgay=2030-02-01&DenNgay=2030-01-01")]
    [InlineData("TuNgay=bad-date")]
    [InlineData("Trang=abc")]
    public async Task QuerySaiTraThongBaoKhongLoi500(string query)
    {
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = await client.GetStringAsync("/QuanLyDonDat/DanhSach?" + query);
        Assert.Contains("validation-summary-errors", html);
        Assert.DoesNotContain("Bảng đơn đặt sân", WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task ChuyenTrangChiTietVaQuayLaiGiuTatCaBoLoc()
    {
        var key = "Loc & " + Guid.NewGuid().ToString("N");
        for (var i = 0; i < 12; i++) await Seed(key + i);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var query = new Dictionary<string, string?> { ["tuKhoa"] = key, ["maLoaiSan"] = "1", ["trangThai"] = "ChoXuLy",
            ["tuNgay"] = "2030-01-02", ["denNgay"] = "2030-01-02", ["sapXep"] = "SanTang" };
        var html = await client.GetStringAsync(QueryHelpers.AddQueryString("/QuanLyDonDat/DanhSach", query));
        var links = Links(html);
        var page = links.First(x => x.StartsWith("/QuanLyDonDat/DanhSach?") && x.Contains("trang=2"));
        var pageQuery = QueryHelpers.ParseQuery(new Uri(client.BaseAddress!, page).Query);
        foreach (var pair in query) Assert.Equal(pair.Value, pageQuery[pair.Key].ToString());
        var second = await client.GetStringAsync(page);
        var detailLink = Links(second).First(x => x.StartsWith("/QuanLyDonDat/ChiTiet/"));
        var back = QueryHelpers.ParseQuery(new Uri(client.BaseAddress!, detailLink).Query)["quayLai"].ToString();
        Assert.Equal(page, back);
        Assert.Contains(back, Links(await client.GetStringAsync(detailLink)));
        var unsafeLink = detailLink.Split('?')[0] + "?quayLai=https%3A%2F%2Fevil.example";
        var safe = await client.GetStringAsync(unsafeLink);
        Assert.DoesNotContain("evil.example", safe);
        Assert.Contains("/QuanLyDonDat/DanhSach", Links(safe));
    }

    [Fact]
    public async Task ChiTietTraTienDaChotVaMaHoaLyDo()
    {
        var order = await f.Booking(TrangThaiDatSan.HoanThanh);
        await using (var db = f.Db())
        {
            var saved = await db.DatSans.FindAsync(order.MaDatSan);
            saved!.TienSan = 200000; saved.TienDichVu = 30000; saved.TongTien = 230000;
            saved.NgayHoanThanh = f.Clock.BayGio; saved.LyDoHuy = "<script>alert(1)</script>";
            await db.SaveChangesAsync();
        }
        await using (var db = f.Db())
        {
            var detail = await f.Service(db).LayChiTietQuanLyAsync(order.MaDatSan, default);
            Assert.NotNull(detail); Assert.Equal(230000m, detail.Don.TongTien);
            Assert.Equal(200000m, detail.Don.TienSanDuKien); Assert.Empty(db.ChangeTracker.Entries());
            Assert.Null(await f.Service(db).LayChiTietQuanLyAsync(int.MaxValue, default));
        }
        using var app = new App(f); using var client = app.Client(); await Login(client, "admin");
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        Assert.Contains("Tổng tiền đã chốt", WebUtility.HtmlDecode(html));
        Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>alert(1)</script>", html);
    }

    private static string[] Links(string html) => Regex.Matches(html, "href=\"([^\"]+)\"")
        .Select(x => WebUtility.HtmlDecode(x.Groups[1].Value)).ToArray();

    private async Task Login(HttpClient client, string user)
    {
        var html = await client.GetStringAsync("/TaiKhoan/DangNhap");
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["TenDangNhap"] = user, ["MatKhau"] = f.Password, ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private sealed class App(SqlFixture fixture) : WebApplicationFactory<QuanLyDonDatController>
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
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(fixture.ConnectionString));
                services.RemoveAll<IDongHo>(); services.AddSingleton<IDongHo>(fixture.Clock);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
    }
}

// M4: Phạm Thành Nghĩa; MSSV: 23103100308. HTTP đối soát trên SQL Server test riêng.
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed partial class QuanLyDonDatTests
{
    [Fact]
    public async Task CocYeuCauDonCuKhopCocDaNhanQuaXacNhanKemCoc()
    {
        var don = await f.Booking();
        await using (var db = f.Db())
        {
            var service = f.Service(db);
            Assert.True((await service.XacNhanDonKemCocAsync(new(don.MaDatSan, don.RowVersion), default)).ThanhCong);
            var info = (await service.LayDoiSoatCocAsync(don.MaDatSan, default)).DuLieu!;
            Assert.Equal(100000m, info.Don.TienCocYeuCau);
            Assert.Equal(info.Don.TienCocYeuCau, info.Don.TienCocDaNhan);
            Assert.False(info.CoTheXacNhan);
        }
        // Phần đọc không sửa dữ liệu lịch sử hoặc biến đơn cũ thành chính sách mới.
        var saved = await f.Read(don.MaDatSan);
        Assert.Equal(0, saved.PhienBanChinhSachCoc);
        Assert.Equal(0m, saved.TienCocYeuCau);
        await using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{don.MaDatSan}"));
        Assert.Contains("không cần xác nhận cọc lần nữa", html);
        Assert.DoesNotContain("Đơn hiện không đủ điều kiện", html);
        Assert.Contains("Cọc đã được ghi nhận qua luồng cũ", html);
    }

    private async Task<int> DonDoiSoat()
    {
        var don = await f.Booking();
        await using var db = f.Db();
        var saved = (await db.DatSans.FindAsync(don.MaDatSan))!;
        saved.PhienBanChinhSachCoc = 1;
        saved.TienCocYeuCau = 100000m;
        await db.SaveChangesAsync();
        return don.MaDatSan;
    }

    private static HttpClient CocClient(WebApplicationFactory<QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers.QuanLyDonDatController> app) =>
        app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

    private async Task<int> GiaoDichDoiSoat(int id, decimal tien = 100000m)
    {
        await using var db = f.Db();
        var result = await f.Service(db).TaoGiaoDichMoPhongAsync(new(Guid.NewGuid().ToString("N"),
            $"COC {id}", tien, f.Clock.BayGio), default);
        Assert.True(result.ThanhCong);
        return result.DuLieu!.MaGiaoDich;
    }

    private static Dictionary<string, string> FormDoiSoat(string html, int id, int gd) => new()
    {
        ["__RequestVerificationToken"] = Token(html), ["MaDon"] = id.ToString(),
        ["MaGiaoDich"] = gd.ToString(), ["PhienBan"] = Hidden(html, "PhienBan"),
        ["DaKiemTraGiaoDich"] = "true"
    };

    [Fact]
    public async Task DoiSoatHttpGhiCocVaXacNhanMotLan()
    {
        var id = await DonDoiSoat(); var gd = await GiaoDichDoiSoat(id);
        await using var original = new App(f);
        using var app = original.WithWebHostBuilder(b => b.ConfigureServices(s => s.Configure<CocMoPhongOptions>(o => o.Bat = true)));
        using var client = CocClient(app); await Login(client, "staff");
        var detail = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{id}");
        Assert.Contains("/QuanLyDonDat/DoiSoatCoc/", detail);
        Assert.DoesNotContain("action=\"/QuanLyDonDat/XacNhan\"", detail);
        var html = await client.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{id}");
        var form = FormDoiSoat(html, id, gd);
        // Không tin các trường tiền/actor dư do client gửi lên.
        form["SoTien"] = "1"; form["MaNguoiXacNhan"] = f.CustomerAccountId.ToString();
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/QuanLyDonDat/XacNhanCoc", new FormUrlEncodedContent(form))).StatusCode);
        var saved = await f.Read(id);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai); Assert.Equal(100000m, saved.TienCoc);
        await using var db = f.Db();
        Assert.Single(db.BienNhanCocs.Where(x => x.MaDatSan == id));
        Assert.NotEqual(f.CustomerAccountId, db.BienNhanCocs.Single(x => x.MaDatSan == id).MaNguoiXacNhan);
        var after = WebUtility.HtmlDecode(await client.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{id}"));
        Assert.Contains("Biên nhận", after);
        Assert.DoesNotContain("action=\"/QuanLyDonDat/XacNhanCoc\"", after);
    }

    [Theory]
    [InlineData("checkbox", "Vui lòng xác nhận đã kiểm tra")]
    [InlineData("version", "Phiên bản đơn không hợp lệ")]
    [InlineData("stale", "Đơn đã thay đổi")]
    [InlineData("amount", "Số tiền giao dịch không khớp")]
    [InlineData("other", "Giao dịch không khớp")]
    public async Task DoiSoatHttpLoiKhongGhiTien(string scenario, string message)
    {
        var id = await DonDoiSoat();
        var gd = await GiaoDichDoiSoat(id, scenario == "amount" ? 1m : 100000m);
        await using var original = new App(f);
        using var app = original.WithWebHostBuilder(b => b.ConfigureServices(s => s.Configure<CocMoPhongOptions>(o => o.Bat = true)));
        using var client = CocClient(app); await Login(client, "admin");
        var html = await client.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{id}");
        var form = FormDoiSoat(html, id, gd);
        if (scenario == "checkbox") form.Remove("DaKiemTraGiaoDich");
        if (scenario == "version") form["PhienBan"] = "invalid";
        if (scenario == "stale") form["PhienBan"] = Convert.ToBase64String(new byte[8]);
        if (scenario == "other") form["MaGiaoDich"] = (await GiaoDichDoiSoat(await DonDoiSoat())).ToString();
        var response = await client.PostAsync("/QuanLyDonDat/XacNhanCoc", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(message, WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        var saved = await f.Read(id);
        Assert.Equal(0m, saved.TienCoc); Assert.Equal(TrangThaiDatSan.ChoXuLy, saved.TrangThai);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("customer")]
    public async Task KhachVaKhachVangKhongDuocDoiSoat(string? user)
    {
        var id = await DonDoiSoat();
        await using var app = new App(f); using var client = app.Client();
        if (user is not null) await Login(client, user);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync($"/QuanLyDonDat/DoiSoatCoc/{id}")).StatusCode);
        foreach (var action in new[] { "XacNhanCoc", "TaoGiaoDichMoPhong" })
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync($"/QuanLyDonDat/{action}", new FormUrlEncodedContent([]))).StatusCode);
    }

    [Fact]
    public async Task PostCocBatBuocAntiforgery()
    {
        await using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        foreach (var action in new[] { "XacNhanCoc", "TaoGiaoDichMoPhong" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/QuanLyDonDat/{action}", new FormUrlEncodedContent([]))).StatusCode);
    }

    [Theory]
    [InlineData("Development", true, true)]
    [InlineData("Development", false, false)]
    [InlineData("Production", true, false)]
    public async Task MoPhongChiDuocGhiKhiMoiTruongVaCauHinhChoPhep(string env, bool enabled, bool allowed)
    {
        var id = await DonDoiSoat();
        await using var original = new App(f);
        using var app = original.WithWebHostBuilder(b =>
        {
            b.UseEnvironment(env);
            b.ConfigureServices(s => s.Configure<CocMoPhongOptions>(o => o.Bat = enabled));
        });
        using var client = CocClient(app); await Login(client, "admin");
        var html = await client.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{id}");
        Assert.Equal(allowed, html.Contains("action=\"/QuanLyDonDat/TaoGiaoDichMoPhong\""));
        var detailHtml = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{id}");
        var form = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(detailHtml), ["MaDon"] = id.ToString(),
            ["MaGiaoDichNgoai"] = Guid.NewGuid().ToString("N"), ["NoiDung"] = $"COC {id}",
            ["SoTien"] = "100000", ["NgayNhan"] = "2030-01-02T08:00:00"
        };
        var response = await client.PostAsync("/QuanLyDonDat/TaoGiaoDichMoPhong", new FormUrlEncodedContent(form));
        Assert.Equal(allowed ? HttpStatusCode.Redirect : HttpStatusCode.OK, response.StatusCode);
        await using var db = f.Db();
        Assert.Equal(allowed ? 1 : 0, db.GiaoDichCocs.Count(x => x.NoiDung == $"COC {id}"));
        Assert.Equal(0m, (await f.Read(id)).TienCoc);
    }

    [Fact]
    public async Task DonMienPhiChinhSachMoiXacNhanKhongTaoBienNhan()
    {
        var id = await DonDoiSoat();
        await using (var db = f.Db())
        {
            var don = (await db.DatSans.FindAsync(id))!;
            don.TienCocYeuCau = 0m; don.DonGia = 0m;
            await db.SaveChangesAsync();
        }
        await using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{id}");
        Assert.Contains("action=\"/QuanLyDonDat/XacNhan\"", html);
        Assert.DoesNotContain("name=\"TinhTrangCoc\"", html);
        Assert.Equal(HttpStatusCode.Redirect, (await PostFromDetail(client, id, "XacNhan")).StatusCode);
        Assert.Equal(TrangThaiDatSan.DangXuLy, (await f.Read(id)).TrangThai);
        await using var verify = f.Db();
        Assert.Empty(verify.BienNhanCocs.Where(x => x.MaDatSan == id));
    }
}

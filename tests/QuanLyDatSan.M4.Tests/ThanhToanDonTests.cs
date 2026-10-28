// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-04 - kiểm thử SQL/HTTP ghi cọc, chốt tiền server, quyền và phiên bản đồng thời.
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed partial class QuanLyDonDatTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("staff")]
    public async Task GhiCocVaHoanThanhTinhTienServerKhongNhanTienGiaMao(string user)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        await using (var db = f.Db())
        {
            var dichVu = new DichVu { TenDichVu = "Nước", DonViTinh = "Chai", DonGia = 10000 };
            db.Add(dichVu);
            await db.SaveChangesAsync();
            Assert.True((await DichVuService(db).ThemAsync(order.MaDatSan, dichVu.MaDichVu, 2, default)).ThanhCong);
            // Giá danh mục thay đổi không được đổi giá chụp trên dòng.
            dichVu.DonGia = 99000;
            await db.SaveChangesAsync();
        }
        using var app = new App(f); using var client = app.Client(); await Login(client, user);
        Assert.Equal(HttpStatusCode.Redirect,
            (await PostFromDetail(client, order.MaDatSan, "GhiCoc", "TongCoc", "50000")).StatusCode);
        var first = await f.Read(order.MaDatSan);
        Assert.Equal(50000m, first.TienCoc);
        Assert.Null(first.TongTien);
        Assert.Equal(HttpStatusCode.Redirect,
            (await PostFromDetail(client, order.MaDatSan, "GhiCoc", "TongCoc", "80000")).StatusCode);
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        Assert.Contains("140.000", WebUtility.HtmlDecode(html));
        var response = await PostPayment(client, order.MaDatSan, "HoanThanh", Token(html), Hidden(html, "PhienBan"),
            new() { ["DaThuDu"] = "true", ["TienSan"] = "1", ["TienDichVu"] = "0", ["TongTien"] = "1", ["TienCoc"] = "0" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(TrangThaiDatSan.HoanThanh, saved.TrangThai);
        Assert.Equal(200000m, saved.TienSan); Assert.Equal(20000m, saved.TienDichVu);
        Assert.Equal(220000m, saved.TongTien); Assert.Equal(80000m, saved.TienCoc);
        Assert.Equal(f.Clock.BayGio, saved.NgayHoanThanh);
        var completed = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        Assert.DoesNotContain("action=\"/QuanLyDonDat/GhiCoc\"", completed);
        Assert.DoesNotContain("action=\"/QuanLyDonDat/HoanThanh\"", completed);
        Assert.Contains("/DichVuTrongDon", completed);
        await using var check = f.Db();
        Assert.False((await DichVuService(check).ThemAsync(saved.MaDatSan,
            await check.ChiTietDatSans.Where(x => x.MaDatSan == saved.MaDatSan).Select(x => x.MaDichVu).SingleAsync(),
            1, default)).ThanhCong);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    [InlineData("-1", 0)]
    [InlineData("0", 0)]
    [InlineData("1.5", 0)]
    [InlineData("10000000000000000", 0)]
    [InlineData("200001", 0)]
    [InlineData("50000", 50000)]
    [InlineData("40000", 50000)]
    public async Task GhiCocSaiKhongGhiVaGiuDuLieuNhap(string input, int deposit)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, deposit: deposit);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var response = await PostFromDetail(client, order.MaDatSan, "GhiCoc", "TongCoc", input);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation-summary-errors", html);
        Assert.Equal(input, Hidden(html, "TongCoc"));
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Theory]
    [InlineData("GhiCoc", TrangThaiDatSan.ChoXuLy, 2)]
    [InlineData("GhiCoc", TrangThaiDatSan.DaHuy, 2)]
    [InlineData("GhiCoc", TrangThaiDatSan.TuChoi, 2)]
    [InlineData("GhiCoc", TrangThaiDatSan.HoanThanh, -1)]
    [InlineData("HoanThanh", TrangThaiDatSan.ChoXuLy, -1)]
    [InlineData("HoanThanh", TrangThaiDatSan.DaHuy, -1)]
    [InlineData("HoanThanh", TrangThaiDatSan.TuChoi, -1)]
    [InlineData("HoanThanh", TrangThaiDatSan.HoanThanh, -1)]
    [InlineData("HoanThanh", TrangThaiDatSan.DangXuLy, 0)]
    public async Task ThanhToanChanTrangThaiVaGioQuaHttp(string action, TrangThaiDatSan state, int hours)
    {
        var order = await f.Booking(state, hours);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        // Lấy token từ danh sách để vẫn kiểm tra POST khi chi tiết không còn form.
        var token = Token(await client.GetStringAsync("/QuanLyDonDat/DanhSach"));
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        Assert.DoesNotContain($"action=\"/QuanLyDonDat/{action}\"", html);
        var response = await PostPayment(client, order.MaDatSan, action, token, Convert.ToBase64String(order.RowVersion));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Theory]
    [InlineData("GhiCoc", null)]
    [InlineData("HoanThanh", null)]
    [InlineData("GhiCoc", "customer")]
    [InlineData("HoanThanh", "customer")]
    public async Task ThanhToanChiChoQuanLy(string action, string? user)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        using var app = new App(f); using var client = app.Client();
        if (user is not null) await Login(client, user);
        var response = await PostPayment(client, order.MaDatSan, action, "", Convert.ToBase64String(order.RowVersion));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(user is null ? "DangNhap" : "TuChoiTruyCap", response.Headers.Location!.OriginalString);
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Theory]
    [InlineData("GhiCoc")]
    [InlineData("HoanThanh")]
    public async Task ThanhToanCanAntiforgeryVaPhienBanHopLe(string action)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await PostPayment(client, order.MaDatSan, action, "", Hidden(html, "PhienBan"))).StatusCode);
        foreach (var version in new[] { "", "bad", Convert.ToBase64String(new byte[7]), Convert.ToBase64String(new byte[9]) })
        {
            var response = await PostPayment(client, order.MaDatSan, action, Token(html), version);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Phiên bản đơn không hợp lệ", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        }
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Fact]
    public async Task HoanThanhCanXacNhanThuDuVaKhongTuTichLaiKhiLoi()
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var response = await PostFromDetail(client, order.MaDatSan, "HoanThanh");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cần xác nhận đã thu đủ", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Theory]
    [InlineData("GhiCoc")]
    [InlineData("HoanThanh")]
    public async Task ThanhToanGuiDongThoiVaLapChiGhiMotLan(string action)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        var token = Token(html); var version = Hidden(html, "PhienBan");
        var responses = await Task.WhenAll(PostPayment(client, order.MaDatSan, action, token, version),
            PostPayment(client, order.MaDatSan, action, token, version));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Redirect);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        var saved = await f.Read(order.MaDatSan);
        var repeated = await PostPayment(client, order.MaDatSan, action, token, version);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(saved.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
        Assert.Equal(action == "GhiCoc" ? 50000m : 0m, saved.TienCoc);
    }

    [Theory]
    [InlineData("GhiCoc")]
    [InlineData("HoanThanh")]
    public async Task SuaDichVuSauKhiMoFormBuocTaiLaiTienVaPhienBan(string action)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        await using (var db = f.Db())
        {
            var dv = new DichVu { TenDichVu = "Nước", DonViTinh = "Chai", DonGia = 10000 };
            db.Add(dv); await db.SaveChangesAsync();
            Assert.True((await DichVuService(db).ThemAsync(order.MaDatSan, dv.MaDichVu, 1, default)).ThanhCong);
        }
        var response = await PostPayment(client, order.MaDatSan, action, Token(html), Hidden(html, "PhienBan"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var returned = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Đơn đã được cập nhật ở nơi khác", returned);
        Assert.Contains("210.000", returned);
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai);
        Assert.Equal(0m, saved.TienCoc);
        Assert.Equal(Convert.ToBase64String(saved.RowVersion), Hidden(returned, "PhienBan"));
        Assert.DoesNotContain("checked", returned);
    }

    private static DichVuTrongDonService DichVuService(QuanLyDatSan_UNETI5_DHTI17A4HN.Data.ApplicationDbContext db)
        => new(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon.DichVuTrongDonRepository(db), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(db), new TinhTienService());

    private static Task<HttpResponseMessage> PostPayment(HttpClient client, int id, string action,
        string token, string version, Dictionary<string, string>? extra = null)
    {
        var values = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["MaDon"] = id.ToString(),
            ["PhienBan"] = version,
            ["TongCoc"] = "50000",
            ["DaThuDu"] = "true",
            ["QuayLai"] = "/QuanLyDonDat/DanhSach"
        };
        if (extra is not null) foreach (var (key, value) in extra) values[key] = value;
        return client.PostAsync($"/QuanLyDonDat/{action}", new FormUrlEncodedContent(values));
    }
}

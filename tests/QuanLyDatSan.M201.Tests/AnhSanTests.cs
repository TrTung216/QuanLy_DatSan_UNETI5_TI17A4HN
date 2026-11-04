// Phan Giang Tâm - 23103100196. M2: HTTP ảnh sân, quyền, validation và giữ ảnh khi sửa thông tin.
using System.Net;
using System.Text.RegularExpressions;
using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

internal sealed class ThuMucAnhSanTest : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "qlds-m2-anh-" + Guid.NewGuid().ToString("N"));
    public void Dispose()
    {
        if (!Path.GetFullPath(Root).StartsWith(Path.Combine(Path.GetTempPath(), "qlds-m2-anh-"), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Sai thư mục ảnh test M2.");
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}

internal static class AnhSanTest
{
    public static byte[] Tep()
    {
        ChuanHoaAnh.KhoiTao();
        using var anh = new MagickImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII="));
        return anh.ToByteArray(MagickFormat.Png);
    }

    public static IKhoAnh Kho(string? root = null) => new KhoAnhDiaPhuong(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["LuuTruAnh:ThuMucGoc"] = root }).Build(),
        new MoiTruong(), NullLogger<KhoAnhDiaPhuong>.Instance);

    // Test nghiệp vụ cũ không có ảnh: không giả vờ đã dọn một file thật.
    public sealed class DonDepKhongCoAnh : IDonDepAnh
    {
        public Task<bool> XoaNeuKhongThamChieuAsync(string? khoa)
            => khoa is null ? Task.FromResult(true) : throw new InvalidOperationException("Test này chưa cấu hình dọn file.");
    }

    private sealed class MoiTruong : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public partial class TrangThaiSanTests
{
    private static MultipartFormDataContent FormAnhSan(string token, byte[]? bytes = null,
        string? cu = null, string thaoTac = "ThayAnh", string tenTep = "a.png")
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(thaoTac), "ThaoTac");
        form.Add(new StringContent(cu ?? ""), "AnhCu");
        if (bytes is not null) form.Add(new ByteArrayContent(bytes), "Tep", tenTep);
        return form;
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("nhanvien")]
    public async Task QuanLyTaiThayGoAnhSanVaSuaThongTinKhongMatAnh(string ten)
    {
        using var dir = new ThuMucAnhSanTest(); using var app = await TaoAppAsync(dir.Root);
        using var client = app.TaoClient(); await DangNhapAsync(app, client, ten);
        var token = Token(await client.GetStringAsync("/SanTheThao/Details/1"));
        using var form = FormAnhSan(token, AnhSanTest.Tep());
        form.Add(new StringContent("Không được ghi đè"), "TenSan");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", form)).StatusCode);
        var san = await DocSanAsync(app, 1); var cu = san.DuongDanAnh;
        Assert.Matches(@"^san/[a-f0-9]{32}\.jpg$", cu!);
        Assert.Equal("Sân bảo trì", san.TenSan); Assert.Equal(200000m, san.DonGia);
        Assert.True(File.Exists(Path.Combine(dir.Root, cu!)));
        using var thay = FormAnhSan(token, AnhSanTest.Tep(), cu);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", thay)).StatusCode);
        var moi = (await DocSanAsync(app, 1)).DuongDanAnh;
        Assert.NotEqual(cu, moi); Assert.False(File.Exists(Path.Combine(dir.Root, cu!)));
        using var stale = FormAnhSan(token, null, cu, "GoAnh");
        var loi = await client.PostAsync("/SanTheThao/CapNhatAnh/1", stale);
        Assert.Contains("Ảnh đã thay đổi", WebUtility.HtmlDecode(await loi.Content.ReadAsStringAsync()));
        Assert.Equal(moi, (await DocSanAsync(app, 1)).DuongDanAnh);
        var sua = Form("1"); sua["MaSan"] = "1";
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, "/SanTheThao/Edit/1", sua)).StatusCode);
        Assert.Equal(moi, (await DocSanAsync(app, 1)).DuongDanAnh);
        var html = await client.GetStringAsync("/SanTheThao");
        Assert.Contains("/Anh/San/1", html);
        using var go = FormAnhSan(token, null, moi, "GoAnh");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", go)).StatusCode);
        Assert.Null((await DocSanAsync(app, 1)).DuongDanAnh);
        Assert.False(File.Exists(Path.Combine(dir.Root, moi!)));
        Assert.Contains("Chưa có ảnh sân", WebUtility.HtmlDecode(await client.GetStringAsync("/SanTheThao/Details/1")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("khachhang")]
    public async Task KhachKhongDuocGhiAnhSanVaKhongThayForm(string? ten)
    {
        using var dir = new ThuMucAnhSanTest(); using var app = await TaoAppAsync(dir.Root);
        await ThemDuLieuTraCuuAsync(app);
        using var client = app.TaoClient(); if (ten is not null) await DangNhapAsync(app, client, ten);
        var html = await client.GetStringAsync("/SanTheThao/Details/3");
        Assert.DoesNotContain("/SanTheThao/CapNhatAnh", html);
        using var form = FormAnhSan("token-khong-can-vi-quyen-bi-chan-truoc", AnhSanTest.Tep());
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/SanTheThao/CapNhatAnh/3", form)).StatusCode);
        Assert.Null((await DocSanAsync(app, 3)).DuongDanAnh);
        Assert.False(Directory.Exists(dir.Root));
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISanTheThaoService>();
        Assert.Equal(MaLoiAnh.KhongDuQuyen, (await service.CapNhatAnhAsync(3, new(ThaoTacAnh.GoAnh, null, null), default)).MaLoi);
    }

    [Theory]
    [InlineData("thieu")]
    [InlineData("rong")]
    [InlineData("nhieu")]
    [InlineData("go-kem-tep")]
    [InlineData("sai-thao-tac")]
    [InlineData("gia-duoi")]
    [InlineData("hong")]
    [InlineData("qua-dung-luong")]
    public async Task FileSaiGiuNguyenAnhCu(string tinhHuong)
    {
        using var dir = new ThuMucAnhSanTest(); using var app = await TaoAppAsync(dir.Root);
        using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var token = Token(await client.GetStringAsync("/SanTheThao/Details/1"));
        using (var tao = FormAnhSan(token, AnhSanTest.Tep()))
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", tao)).StatusCode);
        var cu = (await DocSanAsync(app, 1)).DuongDanAnh;
        byte[]? bytes = tinhHuong == "thieu" ? null : tinhHuong == "rong" ? [] : tinhHuong == "hong" ? [255, 216, 255, 0] :
            tinhHuong == "qua-dung-luong" ? new byte[ChuanHoaAnh.DungLuongToiDa + 1] : AnhSanTest.Tep();
        using var form = FormAnhSan(token, bytes, cu,
            tinhHuong == "go-kem-tep" ? "GoAnh" : tinhHuong == "sai-thao-tac" ? "99" : "ThayAnh",
            tinhHuong is "gia-duoi" or "hong" ? "a.jpg" : "a.png");
        if (tinhHuong == "nhieu") form.Add(new ByteArrayContent(AnhSanTest.Tep()), "Tep", "b.png");
        var response = await client.PostAsync("/SanTheThao/CapNhatAnh/1", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        Assert.Equal(cu, (await DocSanAsync(app, 1)).DuongDanAnh);
        Assert.True(File.Exists(Path.Combine(dir.Root, cu!)));
        Assert.Single(Directory.GetFiles(Path.Combine(dir.Root, "san"), "*.jpg"));
    }

    [Fact]
    public async Task AnhSanChanCsrfQuaRequestVaKhoChuaCauHinh()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var token = Token(await client.GetStringAsync("/SanTheThao/Details/1"));
        using var csrf = FormAnhSan("sai", AnhSanTest.Tep());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", csrf)).StatusCode);
        using var quaLon = FormAnhSan(token, new byte[ChuanHoaAnh.GioiHanRequest + 1]);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", quaLon)).StatusCode);
        using var hopLe = FormAnhSan(token, AnhSanTest.Tep());
        var response = await client.PostAsync("/SanTheThao/CapNhatAnh/1", hopLe);
        Assert.Contains("Kho ảnh chưa sẵn sàng", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Null((await DocSanAsync(app, 1)).DuongDanAnh);
    }

    [Fact]
    public async Task GoAnhRongKhongCanKhoVaMaSanKhongTonTaiTra404()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var token = Token(await client.GetStringAsync("/SanTheThao/Details/1"));
        using var go = FormAnhSan(token, null, null, "GoAnh");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/SanTheThao/CapNhatAnh/1", go)).StatusCode);
        using var sai = FormAnhSan(token, AnhSanTest.Tep());
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/SanTheThao/CapNhatAnh/99999", sai)).StatusCode);
        using var khoaGia = FormAnhSan(token, null, "../../file.jpg", "GoAnh");
        var response = await client.PostAsync("/SanTheThao/CapNhatAnh/1", khoaGia);
        Assert.Contains("Ảnh đã thay đổi", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Null((await DocSanAsync(app, 1)).DuongDanAnh);
    }

    [Fact]
    public async Task AnhDaLuuGiuQuyenDocTheoTrangThaiVaFileMatKhongHongChiTiet()
    {
        using var dir = new ThuMucAnhSanTest(); using var app = await TaoAppAsync(dir.Root);
        using var admin = app.TaoClient(); await DangNhapAsync(app, admin);
        var token = Token(await admin.GetStringAsync("/SanTheThao/Details/1"));
        using var post = FormAnhSan(token, AnhSanTest.Tep());
        Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync("/SanTheThao/CapNhatAnh/1", post)).StatusCode);
        using var guest = app.TaoClient();
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/Anh/San/1")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/Anh/San/1")).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.SanTheThaos.FindAsync(1))!.TrangThai = TrangThaiSan.HoatDong;
            await db.SaveChangesAsync();
        }
        var image = await guest.GetAsync("/Anh/San/1");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.True(image.Headers.CacheControl!.Private); Assert.True(image.Headers.CacheControl.NoStore);
        Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("image/jpeg", image.Content.Headers.ContentType!.MediaType);
        var san = await DocSanAsync(app, 1);
        await app.Services.GetRequiredService<IKhoAnh>().XoaAsync(san.DuongDanAnh!, default);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/Anh/San/1")).StatusCode);
        var html = WebUtility.HtmlDecode(await guest.GetStringAsync("/SanTheThao/Details/1"));
        Assert.Contains("Chưa có ảnh sân", html); Assert.Contains("/Anh/San/1", html);
    }
}

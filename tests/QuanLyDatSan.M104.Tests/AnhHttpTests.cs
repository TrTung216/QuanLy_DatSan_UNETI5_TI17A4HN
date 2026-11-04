// M1: Trần Trọng Tùng - 23103100202. HTTP thật qua TestServer/cookie, SQLite RAM, kho tạm ngoài repo.
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
using Xunit;
namespace QuanLyDatSan.M104.Tests;

public partial class LoaiSanTests
{
    private static MultipartFormDataContent FormAnh(string token, byte[]? bytes = null, string? cu = null,
        string thaoTac = "ThayAnh", string fileName = "a.png")
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(thaoTac), "ThaoTac");
        form.Add(new StringContent(cu ?? ""), "AnhCu");
        if (bytes is not null) form.Add(new ByteArrayContent(bytes), "Tep", fileName);
        return form;
    }

    [Fact]
    public async Task AnhThemThayGoGiuThongTinLoaiSanVaXungDotFormCu()
    {
        using var dir = new ThuMucAnhTest(); using var app = await TaoAppAsync(dir.Root);
        using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var token = await TokenAsync(client, "/LoaiSan/ChiTiet/2");
        using var post = FormAnh(token, KhoAnhTests.TaoAnh());
        post.Add(new StringContent("Gia mao"), "TenLoai"); post.Add(new StringContent("1"), "DonGiaTheoGio");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/LoaiSan/CapNhatAnh/2", post)).StatusCode);
        async Task<string?> Khoa()
        {
            using var scope = app.Services.CreateScope();
            var entity = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().LoaiSans.AsNoTracking().SingleAsync(x => x.MaLoaiSan == 2);
            Assert.Equal("Loai chua su dung", entity.TenLoai); Assert.Equal(80000, entity.DonGiaTheoGio);
            return entity.DuongDanAnh;
        }
        var cu = await Khoa(); Assert.NotNull(cu);
        using var publicClient = app.TaoClient();
        var anh = await publicClient.GetAsync("/Anh/LoaiSan/2");
        Assert.Equal(HttpStatusCode.OK, anh.StatusCode); Assert.Equal("image/jpeg", anh.Content.Headers.ContentType!.MediaType);
        Assert.True(anh.Headers.CacheControl!.Private); Assert.True(anh.Headers.CacheControl.NoStore);
        Assert.Equal("nosniff", anh.Headers.GetValues("X-Content-Type-Options").Single());
        using var thay = FormAnh(token, KhoAnhTests.TaoAnh(), cu);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/LoaiSan/CapNhatAnh/2", thay)).StatusCode);
        var moi = await Khoa(); Assert.NotEqual(cu, moi); Assert.False(File.Exists(Path.Combine(dir.Root, cu!)));
        using var loi = FormAnh(token, null, cu, "GoAnh");
        var response = await client.PostAsync("/LoaiSan/CapNhatAnh/2", loi);
        Assert.Contains("Ảnh đã thay đổi", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(moi, await Khoa());
        using var go = FormAnh(token, null, moi, "GoAnh");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/LoaiSan/CapNhatAnh/2", go)).StatusCode);
        Assert.Null(await Khoa()); Assert.False(File.Exists(Path.Combine(dir.Root, moi!)));
        Assert.Equal(HttpStatusCode.NotFound, (await publicClient.GetAsync("/Anh/LoaiSan/2")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nhanvien")]
    [InlineData("khachhang")]
    public async Task AnhLoaiSanKhongChoNguoiKhacGhi(string? taiKhoan)
    {
        using var dir = new ThuMucAnhTest(); using var app = await TaoAppAsync(dir.Root);
        using var client = app.TaoClient();
        if (taiKhoan is not null) await DangNhapAsync(app, client, taiKhoan);
        using var form = FormAnh("sai", KhoAnhTests.TaoAnh());
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/LoaiSan/CapNhatAnh/2", form)).StatusCode);
        Assert.False(Directory.Exists(dir.Root));
        // Service cũng chặn gọi trực tiếp, không chỉ ẩn nút/chặn Controller.
        using var scope = app.Services.CreateScope();
        Assert.Equal(MaLoiAnh.KhongDuQuyen, (await scope.ServiceProvider.GetRequiredService<IAnhService>()
            .CapNhatLoaiSanAsync(2, new(ThaoTacAnh.GoAnh, null, null), default)).MaLoi);
    }

    [Fact]
    public async Task AnhChanCsrfNhieuTepGoKemTepVaRequestQuaLon()
    {
        using var dir = new ThuMucAnhTest(); using var app = await TaoAppAsync(dir.Root);
        using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var token = await TokenAsync(client, "/LoaiSan/ChiTiet/2");
        using var sai = FormAnh("sai", KhoAnhTests.TaoAnh());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/LoaiSan/CapNhatAnh/2", sai)).StatusCode);
        using var nhieu = FormAnh(token, KhoAnhTests.TaoAnh()); nhieu.Add(new ByteArrayContent([1, 2]), "Tep", "b.jpg");
        var response = await client.PostAsync("/LoaiSan/CapNhatAnh/2", nhieu);
        Assert.Contains("Chọn đúng một ảnh", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        using var goKemTep = FormAnh(token, KhoAnhTests.TaoAnh(), thaoTac: "GoAnh");
        Assert.Contains("Chọn đúng một ảnh", WebUtility.HtmlDecode(await (await client.PostAsync("/LoaiSan/CapNhatAnh/2", goKemTep)).Content.ReadAsStringAsync()));
        using var quaLon = FormAnh(token, new byte[ChuanHoaAnh.GioiHanRequest + 1]);
        var large = await client.PostAsync("/LoaiSan/CapNhatAnh/2", quaLon);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, large.StatusCode);
        Assert.Contains("Ảnh hiện tại chưa bị thay đổi", await large.Content.ReadAsStringAsync());
        Assert.False(Directory.Exists(dir.Root));
    }

    [Fact]
    public async Task AnhAnTheoTrangThaiVaVaiTroTepMatTra404()
    {
        using var dir = new ThuMucAnhTest(); using var app = await TaoAppAsync(dir.Root);
        var kho = app.Services.GetRequiredService<IKhoAnh>(); var bytes = KhoAnhTests.TaoAnh();
        using var input = new MemoryStream(bytes);
        var khoa = await kho.LuuAsync(NhomAnh.San, new(input, "a.png", bytes.Length), default);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.SanTheThaos.FindAsync(1))!.DuongDanAnh = khoa;
            var loai = (await db.LoaiSans.FindAsync(2))!; loai.TrangThai = TrangThaiLoaiSan.NgungHoatDong;
            loai.DuongDanAnh = khoa; // Chỉ để kiểm tra đọc/quyền; không phải luồng ghi chia sẻ key.
            await db.SaveChangesAsync();
        }
        foreach (var ten in new string?[] { null, "khachhang", "nhanvien", "admin" })
        {
            using var client = app.TaoClient(); if (ten is not null) await DangNhapAsync(app, client, ten);
            Assert.Equal(ten is "admin" or "nhanvien" ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                (await client.GetAsync("/Anh/San/1")).StatusCode);
            Assert.Equal(ten is "admin" ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                (await client.GetAsync("/Anh/LoaiSan/2")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Anh/LoaiSan/999")).StatusCode);
        }
        await kho.XoaAsync(khoa, default);
        using var admin = app.TaoClient(); await DangNhapAsync(app, admin);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Anh/LoaiSan/2")).StatusCode);
        Assert.Contains("data-anh-dai-dien", await admin.GetStringAsync("/LoaiSan/ChiTiet/2"));
    }
}

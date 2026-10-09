// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử HTTP M1-04 và bảo toàn tham chiếu.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan.M103.Tests;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using Xunit;

namespace QuanLyDatSan.M104.Tests;

public class LoaiSanTests
{
    private static async Task<UngDungKiemThu> TaoAppAsync()
    {
        var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Schema riêng trong RAM. FK chặn xóa cha được bật bởi provider SQLite; không chạy migration thật.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE LoaiSan (
                MaLoaiSan INTEGER PRIMARY KEY AUTOINCREMENT,
                TenLoai TEXT NOT NULL COLLATE Vietnamese_100_CI_AS UNIQUE,
                MoTa TEXT, SoNguoiToiDa INTEGER NOT NULL CHECK(SoNguoiToiDa > 0),
                DonGiaTheoGio TEXT NOT NULL, TrangThai INTEGER NOT NULL CHECK(TrangThai IN (0,1))
            );
            CREATE TABLE SanTheThao (
                MaSan INTEGER PRIMARY KEY AUTOINCREMENT, TenSan TEXT NOT NULL,
                MaLoaiSan INTEGER NOT NULL REFERENCES LoaiSan(MaLoaiSan) ON DELETE NO ACTION,
                DiaChi TEXT NOT NULL, TienIch TEXT, DonGia TEXT NOT NULL, TrangThai INTEGER NOT NULL,
                GioMoCua TEXT NOT NULL, GioDongCua TEXT NOT NULL, GhiChu TEXT, NgayBaoTri TEXT
            );
            """);
        db.LoaiSans.AddRange(
            new LoaiSan { MaLoaiSan = 1, TenLoai = "Loai da co san", SoNguoiToiDa = 10, DonGiaTheoGio = 200000 },
            new LoaiSan { MaLoaiSan = 2, TenLoai = "Loai chua su dung", SoNguoiToiDa = 4, DonGiaTheoGio = 80000 });
        db.SanTheThaos.Add(new SanTheThao
        {
            MaSan = 1, MaLoaiSan = 1, TenSan = "San lich su", DiaChi = "Dia chi test", DonGia = 250000,
            TrangThai = TrangThaiSan.NgungHoatDong, GioMoCua = new TimeOnly(6, 0), GioDongCua = new TimeOnly(22, 0)
        });
        await db.SaveChangesAsync();
        return app;
    }

    private static Dictionary<string, string> Form() => new()
    {
        ["TenLoai"] = "Loai moi", ["MoTa"] = "Mo ta", ["SoNguoiToiDa"] = "12",
        ["DonGiaTheoGio"] = "150000", ["TrangThai"] = "1"
    };

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        return WebUtility.HtmlDecode(token.Groups[1].Value);
    }

    private static async Task DangNhapAsync(UngDungKiemThu app, HttpClient client, string ten = "admin")
    {
        var token = await TokenAsync(client, "/TaiKhoan/DangNhap");
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["TenDangNhap"] = ten, ["MatKhau"] = app.MatKhau, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Dictionary<string, string> form)
    {
        form["__RequestVerificationToken"] = await TokenAsync(client, "/LoaiSan/Tao");
        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nhanvien")]
    [InlineData("khachhang")]
    public async Task NguoiKhongDuQuyenKhongDuocGetHoacPost(string? ten)
    {
        using var app = await TaoAppAsync();
        using var client = app.TaoClient();
        if (ten is not null) await DangNhapAsync(app, client, ten);
        var token = await TokenAsync(client, ten is null ? "/TaiKhoan/DangNhap" : "/TaiKhoan/ThongTin");
        foreach (var path in new[] { "DanhSach", "ChiTiet/1", "Tao", "Sua/1", "Xoa/1" })
        {
            var response = await client.GetAsync("/LoaiSan/" + path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(ten is null ? "DangNhap" : "TuChoiTruyCap", response.Headers.Location!.OriginalString);
        }
        foreach (var path in new[] { "Tao", "Sua/1", "Xoa/1" })
        {
            var form = Form(); form["__RequestVerificationToken"] = token;
            var response = await client.PostAsync("/LoaiSan/" + path, new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains(ten is null ? "DangNhap" : "TuChoiTruyCap", response.Headers.Location!.OriginalString);
        }
        var html = await client.GetStringAsync(ten is null ? "/Home/Index" : "/TaiKhoan/ThongTin");
        Assert.DoesNotContain("href=\"/LoaiSan", html);
        using var scope = app.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().LoaiSans.CountAsync());
    }

    [Fact]
    public async Task AdminTaoChiTietSuaTrangThaiVaXoaLoaiChuaThamChieu()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var form = Form(); form["TenLoai"] = "  Loai moi  "; form["MaLoaiSan"] = "1";
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, "/LoaiSan/Tao", form)).StatusCode);
        int id;
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loai = await db.LoaiSans.SingleAsync(x => x.TenLoai == "Loai moi");
            id = loai.MaLoaiSan; Assert.NotEqual(1, id);
        }
        var detail = await client.GetStringAsync($"/LoaiSan/ChiTiet/{id}");
        Assert.Contains("Loai moi", detail);
        Assert.Matches("nav-link active[^>]*aria-current=\"page\"[^>]*href=\"/LoaiSan/DanhSach\"", detail);
        // Decimal đọc từ DB có thể mang scale .0; jQuery step=1 cần giá được render không có phần lẻ.
        var editHtml = await client.GetStringAsync($"/LoaiSan/Sua/{id}");
        var priceInput = Regex.Match(editHtml, "<input[^>]*id=\"DonGiaTheoGio\"[^>]*>").Value;
        Assert.Contains("value=\"150000\"", priceInput);
        form = Form(); form["TrangThai"] = "0";
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, $"/LoaiSan/Sua/{id}", form)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/LoaiSan/Xoa/{id}")).StatusCode);
        using (var scope = app.Services.CreateScope())
            Assert.Equal(TrangThaiLoaiSan.NgungHoatDong, (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().LoaiSans.FindAsync(id))!.TrangThai);
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, $"/LoaiSan/Xoa/{id}", [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/LoaiSan/ChiTiet/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("TenLoai", "   ")]
    [InlineData("TenLoai", "LONG")]
    [InlineData("MoTa", "LONG")]
    [InlineData("SoNguoiToiDa", null)]
    [InlineData("SoNguoiToiDa", "0")]
    [InlineData("SoNguoiToiDa", "1.5")]
    [InlineData("DonGiaTheoGio", null)]
    [InlineData("DonGiaTheoGio", "-1")]
    [InlineData("DonGiaTheoGio", "1.5")]
    [InlineData("DonGiaTheoGio", "10000000000000000")]
    [InlineData("TrangThai", null)]
    [InlineData("TrangThai", "9")]
    public async Task TuChoiDuLieuKhongHopLeKhiTaoVaSua(string key, string? value)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        foreach (var path in new[] { "/LoaiSan/Tao", "/LoaiSan/Sua/2" })
        {
            var form = Form();
            if (value is null) form.Remove(key); else form[key] = value == "LONG" ? new string('x', key == "MoTa" ? 1001 : 101) : value;
            var response = await PostAsync(client, path, form);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        }
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.LoaiSans.CountAsync());
        Assert.Equal("Loai chua su dung", (await db.LoaiSans.FindAsync(2))!.TenLoai);
    }

    [Fact]
    public async Task ChanTrungTenSauTrimVaKhacHoaThuong()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        foreach (var path in new[] { "/LoaiSan/Tao", "/LoaiSan/Sua/2" })
        {
            var form = Form(); form["TenLoai"] = "  LOAI DA CO SAN  ";
            var response = await PostAsync(client, path, form);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Tên loại sân đã được sử dụng", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        }
    }

    [Fact]
    public async Task KhongXoaLoaiCoSanVaKhongDoiGiaSanKhiSuaDanhMuc()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var form = Form(); form["TenLoai"] = "Loai da co san"; form["TrangThai"] = "0"; form["DonGiaTheoGio"] = "0";
        Assert.Equal(HttpStatusCode.Redirect, (await PostAsync(client, "/LoaiSan/Sua/1", form)).StatusCode);
        var html = await client.GetStringAsync("/LoaiSan/Xoa/1");
        Assert.DoesNotContain("Xác nhận xóa", WebUtility.HtmlDecode(Regex.Replace(html, "aria-label=\"[^\"]*\"", "")));
        var response = await PostAsync(client, "/LoaiSan/Xoa/1", []);
        Assert.Equal("/LoaiSan/Xoa/1", response.Headers.Location!.OriginalString);
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(TrangThaiLoaiSan.NgungHoatDong, (await db.LoaiSans.FindAsync(1))!.TrangThai);
        var san = (await db.SanTheThaos.FindAsync(1))!;
        Assert.Equal(250000m, san.DonGia); Assert.Equal(1, san.MaLoaiSan); Assert.Equal(TrangThaiSan.NgungHoatDong, san.TrangThai);
    }

    [Fact]
    public async Task KiemTraLaiThamChieuSauManHinhXacNhan()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        var token = await TokenAsync(client, "/LoaiSan/Xoa/2");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.SanTheThaos.Add(new SanTheThao { MaLoaiSan = 2, TenSan = "Moi tham chieu", DiaChi = "Test", GioMoCua = new TimeOnly(6, 0), GioDongCua = new TimeOnly(22, 0) });
            await db.SaveChangesAsync();
        }
        var response = await client.PostAsync("/LoaiSan/Xoa/2", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal("/LoaiSan/Xoa/2", response.Headers.Location!.OriginalString);
        using var read = app.Services.CreateScope();
        Assert.NotNull(await read.ServiceProvider.GetRequiredService<ApplicationDbContext>().LoaiSans.FindAsync(2));
    }

    [Fact]
    public async Task ThieuTokenMaKhongTonTaiVaThamSoSai()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        foreach (var path in new[] { "Tao", "Sua/2", "Xoa/2" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/LoaiSan/" + path, new FormUrlEncodedContent(Form()))).StatusCode);
        foreach (var action in new[] { "ChiTiet", "Sua", "Xoa" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/LoaiSan/{action}/999")).StatusCode);
        foreach (var action in new[] { "Sua", "Xoa" })
            Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(client, $"/LoaiSan/{action}/999", Form())).StatusCode);
        foreach (var query in new[] { "trangThai=99", "trangThai=abc", "sapXep=khong_hop_le", "trang=abc" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/LoaiSan/DanhSach?" + query)).StatusCode);
    }

    [Fact]
    public async Task TimLocSapXepPhanTrangVaTrangRong()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient(); await DangNhapAsync(app, client);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 1; i <= 12; i++) db.LoaiSans.Add(new LoaiSan { TenLoai = $"Loc {i:00}", SoNguoiToiDa = 10, DonGiaTheoGio = 0 });
            db.LoaiSans.Add(new LoaiSan { TenLoai = "Loc ngung", SoNguoiToiDa = 10, TrangThai = TrangThaiLoaiSan.NgungHoatDong });
            await db.SaveChangesAsync();
        }
        var html = await client.GetStringAsync("/LoaiSan/DanhSach?tuKhoa=Loc&trangThai=1&sapXep=ten_giam");
        Assert.True(html.IndexOf("Loc 12", StringComparison.Ordinal) < html.IndexOf("Loc 11", StringComparison.Ordinal));
        Assert.DoesNotContain("Loc 02", html); Assert.DoesNotContain("Loc ngung", html);
        var next = Regex.Matches(WebUtility.HtmlDecode(html), "href=\"([^\"]*trang=2[^\"]*)\"").First().Groups[1].Value;
        Assert.Contains("tuKhoa=Loc", next); Assert.Contains("trangThai=", next); Assert.Contains("sapXep=ten_giam", next);
        html = await client.GetStringAsync(next); Assert.Contains("Loc 02", html); Assert.DoesNotContain("Loc 12", html);
        html = WebUtility.HtmlDecode(await client.GetStringAsync("/LoaiSan/DanhSach?tuKhoa=khongtimthay&trang=999"));
        Assert.Contains("Chưa có kết quả", html);
    }
}

// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm tra trang chủ công khai, nhóm sân và giới hạn dữ liệu.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class TrangChuTests
{
    private static async Task<UngDungKiemThu> TaoAppAsync()
    {
        var app = new UngDungKiemThu();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Chỉ các cột được truy vấn, trong SQLite RAM của fixture; không đụng database local.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE LoaiSan (MaLoaiSan INTEGER PRIMARY KEY, TenLoai TEXT, TrangThai INTEGER);
            CREATE TABLE SanTheThao (MaSan INTEGER PRIMARY KEY, MaLoaiSan INTEGER,
                TenSan TEXT, DiaChi TEXT, DonGia TEXT, GioMoCua TEXT, GioDongCua TEXT, TrangThai INTEGER);
            INSERT INTO LoaiSan VALUES (1, 'Bóng đá', 1), (2, 'Cầu lông', 1), (3, 'Loại ẩn', 0);
            """);
        return app;
    }

    [Fact]
    public async Task TrangRongVaDangKyChiNamSauDangNhapTrenHeader()
    {
        using var app = await TaoAppAsync();
        using var client = app.TaoClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        Assert.Contains("Chưa có sân để hiển thị", html);
        Assert.Single(Regex.Matches(html, "href=\"/KhachHang/DangKy\""));
        Assert.Single(Regex.Matches(html, "href=\"/TaiKhoan/DangNhap\""));
        var header = Regex.Match(html, "<header[\\s\\S]*?</header>").Value;
        Assert.True(header.IndexOf("/KhachHang/DangKy", StringComparison.Ordinal)
            > header.IndexOf("/TaiKhoan/DangNhap", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NhomSanCongKhaiAnSanKhongHoatDongVaGioiHanSauSanMoiLoai()
    {
        using var app = await TaoAppAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 1; i <= 7; i++)
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SanTheThao VALUES ({i}, 1, {"Sân " + i}, 'Hà Nội', '200000', '06:00:00', '22:00:00', 1)");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO SanTheThao VALUES (8, 2, 'Sân cầu lông', 'Hà Nội', '100000', '07:00:00', '21:00:00', 1),
                (9, 1, 'Sân bảo trì', 'Hà Nội', '200000', '06:00:00', '22:00:00', 2),
                (10, 3, 'Sân thuộc loại ẩn', 'Hà Nội', '200000', '06:00:00', '22:00:00', 1),
                (11, 1, 'Sân ngừng hoạt động', 'Hà Nội', '200000', '06:00:00', '22:00:00', 3);
                """);
            var service = scope.ServiceProvider.GetRequiredService<ITrangChuService>();
            var nhom = await service.LaySanTheoLoaiAsync(default);
            Assert.Equal(2, nhom.Count);
            Assert.Equal(6, nhom.Single(x => x.MaLoaiSan == 1).San.Count);
            Assert.Single(nhom.Single(x => x.MaLoaiSan == 2).San);
            Assert.DoesNotContain(nhom.SelectMany(x => x.San), x => x.MaSan >= 9);
        }
        using var client = app.TaoClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        Assert.Contains("Bóng đá", html);
        Assert.Contains("Sân cầu lông", html);
        Assert.Contains("200.000", html);
        Assert.Contains("/SanTheThao/Details/1", html);
        Assert.Contains("maLoaiSan=1", html);
        Assert.DoesNotContain("Sân bảo trì", html);
        Assert.DoesNotContain("Sân thuộc loại ẩn", html);
    }
}

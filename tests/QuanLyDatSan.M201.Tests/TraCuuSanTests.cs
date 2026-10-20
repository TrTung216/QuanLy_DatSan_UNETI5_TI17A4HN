// Phan Giang Tâm - 23103100196. M2: Kiểm tra tìm/lọc/sắp xếp, quyền và phân trang.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan.M103.Tests;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

public partial class TrangThaiSanTests
{
    private static async Task ThemDuLieuTraCuuAsync(UngDungKiemThu app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.LoaiSans.AddRange(
            new LoaiSan { MaLoaiSan = 2, TenLoai = "Cầu lông", SoNguoiToiDa = 4, DonGiaTheoGio = 80000 },
            new LoaiSan { MaLoaiSan = 3, TenLoai = "Loại kín", SoNguoiToiDa = 4, DonGiaTheoGio = 80000, TrangThai = TrangThaiLoaiSan.NgungHoatDong });
        await db.SaveChangesAsync();
        foreach (var (id, ten, loai, diaChi, gia, gio, trangThai) in new[]
        {
            (3, "Alpha", 1, "Hà Nội", 80000m, 8, TrangThaiSan.HoatDong),
            (4, "Beta", 1, "Đà Nẵng", 200000m, 6, TrangThaiSan.HoatDong),
            (5, "Gamma", 2, "Hà Nội", 150000m, 7, TrangThaiSan.HoatDong),
            (6, "Sân kín", 3, "Hà Nội", 200000m, 5, TrangThaiSan.HoatDong),
            (7, "Delta", 2, "Hà Nội", 100000m, 10, TrangThaiSan.BaoTri)
        })
            db.SanTheThaos.Add(new SanTheThao
            {
                MaSan = id, TenSan = ten, MaLoaiSan = loai, DiaChi = diaChi, DonGia = gia,
                DonGiaCaoDiem = 350000, DonGiaCuoiTuan = 450000, TrangThai = trangThai,
                GioMoCua = new(gio, 0), GioDongCua = new(22, 0)
            });
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData("  ALPHA  ", "Alpha")]
    [InlineData("Cầu lông", "Gamma")]
    // SQLite chỉ chuyển chữ ASCII sang chữ thường; chữ hoa tiếng Việt kiểm tra riêng trên SQL Server.
    [InlineData("Nẵng", "Beta")]
    public async Task TimTheoTenLoaiVaDiaChi(string tuKhoa, string ten)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app);
        var html = await client.GetStringAsync("/SanTheThao?tuKhoa=" + Uri.EscapeDataString(tuKhoa));
        Assert.Equal([ten], TenSanTrongBang(html));
    }

    [Theory]
    [InlineData("maLoaiSan=2", "Gamma")]
    [InlineData("giaTu=150000&giaDen=150000", "Gamma")]
    [InlineData("giaTu=200000", "Beta")]
    [InlineData("giaDen=80000", "Alpha")]
    [InlineData("tuKhoa=H%C3%A0%20N%E1%BB%99i&maLoaiSan=2&trangThai=1&giaTu=100000&giaDen=150000", "Gamma")]
    public async Task KetHopBoLocVaBaoGomGiaBien(string query, string ten)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app);
        Assert.Equal([ten], TenSanTrongBang(await client.GetStringAsync("/SanTheThao?" + query)));
    }

    [Theory]
    [InlineData("ten_tang", "Alpha,Beta,Gamma")]
    [InlineData("ten_giam", "Gamma,Beta,Alpha")]
    [InlineData("gio_tang", "Beta,Gamma,Alpha")]
    [InlineData("gio_giam", "Alpha,Gamma,Beta")]
    public async Task SapXepTenVaGioTaiDatabase(string sapXep, string ten)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app);
        Assert.Equal(ten.Split(','), TenSanTrongBang(await client.GetStringAsync("/SanTheThao?sapXep=" + sapXep)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("khachhang")]
    public async Task KhachKhongNangQuyenBangDieuKienLoc(string? taiKhoan)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app);
        if (taiKhoan is not null) await DangNhapAsync(app, client, taiKhoan);
        foreach (var query in new[] { "trangThai=2", "trangThai=3", "maLoaiSan=3", "tuKhoa=S%C3%A2n%20k%C3%ADn" })
        {
            var html = WebUtility.HtmlDecode(await client.GetStringAsync("/SanTheThao?" + query));
            Assert.Empty(TenSanTrongBang(html));
            Assert.DoesNotContain("value=\"3\">Loại kín", html);
            Assert.DoesNotContain("/SanTheThao/Edit/", html);
        }
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("nhanvien")]
    public async Task QuanLyLocDuocBaoTriVaLoaiNgungHoatDong(string taiKhoan)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app); await DangNhapAsync(app, client, taiKhoan);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/SanTheThao?maLoaiSan=2&trangThai=2"));
        Assert.Equal(["Delta"], TenSanTrongBang(html));
        Assert.Contains("Loại kín", html);
        Assert.Equal(["Sân kín"], TenSanTrongBang(await client.GetStringAsync("/SanTheThao?maLoaiSan=3")));
    }

    [Theory]
    [InlineData("giaTu=-1")]
    [InlineData("giaTu=100.5")]
    [InlineData("giaTu=abc")]
    [InlineData("giaTu=200000&giaDen=100000")]
    [InlineData("giaDen=10000000000000000")]
    [InlineData("maLoaiSan=0")]
    [InlineData("trangThai=99")]
    [InlineData("sapXep=khong_co")]
    [InlineData("trang=abc")]
    public async Task DieuKienSaiGiuFormVaBaoLoi(string query)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app);
        var response = await client.GetAsync("/SanTheThao?" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("validation-summary-errors", html);
        Assert.Contains("Vui lòng sửa điều kiện tra cứu", html);
        Assert.Empty(TenSanTrongBang(html));
        Assert.Contains("Cầu lông", html);
    }

    [Fact]
    public async Task TuKhoaDaiVaKyTuDacBietKhongLamHongTruyVan()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuTraCuuAsync(app);
        var dai = await client.GetStringAsync("/SanTheThao?tuKhoa=" + new string('a', 101));
        Assert.Contains("validation-summary-errors", dai);
        foreach (var tuKhoa in new[] { "không có", "%", "_", "' OR 1=1 --", "<script>alert(1)</script>" })
        {
            var phanHoi = await client.GetStringAsync("/SanTheThao?tuKhoa=" + Uri.EscapeDataString(tuKhoa));
            var html = WebUtility.HtmlDecode(phanHoi);
            Assert.Empty(TenSanTrongBang(html));
            Assert.Contains("Không có sân phù hợp", html);
            Assert.DoesNotContain("<script>alert(1)</script>", phanHoi);
        }
    }

    [Fact]
    public async Task ChuyenTrangGiuTatCaDieuKienVaLocMoiVeTrangMot()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await ThemDuLieuDanhSachAsync(app);
        const string query = "tuKhoa=ho%E1%BA%A1t%20%C4%91%E1%BB%99ng&maLoaiSan=1&trangThai=1&giaTu=200000&giaDen=200000&sapXep=gio_giam";
        var html = await client.GetStringAsync("/SanTheThao?" + query);
        Assert.Equal(10, TenSanTrongBang(html).Length);
        var match = Regex.Match(html, "href=\"([^\"]*)\">Sau</a>");
        Assert.True(match.Success);
        var url = WebUtility.HtmlDecode(match.Groups[1].Value);
        foreach (var thamSo in new[] { "tuKhoa=", "maLoaiSan=1", "trangThai=1", "giaTu=200000", "giaDen=200000", "sapXep=gio_giam", "trang=2" })
            Assert.Contains(thamSo, url);
        var trangHai = await client.GetStringAsync(url);
        Assert.Equal(2, TenSanTrongBang(trangHai).Length);
        Assert.Empty(TenSanTrongBang(html).Intersect(TenSanTrongBang(trangHai)));
        var form = Regex.Match(trangHai, "<form[^>]*method=\"get\"[\\s\\S]*?</form>", RegexOptions.IgnoreCase).Value;
        Assert.DoesNotContain("name=\"Trang\"", form);
        Assert.Contains("Trang 1 / 1", await client.GetStringAsync("/SanTheThao?tuKhoa=12"));
    }

    [Fact]
    public async Task ServiceTuChanDieuKienSaiVaTrangNgoaiPhamVi()
    {
        using var app = await TaoAppAsync(); await ThemDuLieuTraCuuAsync(app);
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISanTheThaoService>();
        var cacDieuKien = new[]
        {
            new TraCuuSan(TuKhoa: new string('a', 101)), new TraCuuSan(MaLoaiSan: 0),
            new TraCuuSan(TrangThai: (TrangThaiSan)99), new TraCuuSan(GiaTu: -1),
            new TraCuuSan(GiaDen: 0.5m), new TraCuuSan(GiaTu: 20, GiaDen: 10),
            new TraCuuSan(SapXep: "khong_co")
        };
        foreach (var dieuKien in cacDieuKien)
            Assert.Equal(MaLoiSan.DuLieuKhongHopLe, (await service.LayDanhSachAsync(dieuKien, default)).MaLoi);
        var dung = (await service.LayDanhSachAsync(new(Trang: 999), default)).DuLieu;
        Assert.Equal(1, dung.Trang);
        Assert.Equal(3, dung.TongSoSan);
    }
}

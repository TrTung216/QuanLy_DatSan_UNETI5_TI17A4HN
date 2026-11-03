// Phan Giang Tâm - 23103100196. M2: Tra cứu sân trống qua M4, lỗi form và phân trang.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan.M103.Tests;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

public partial class TrangThaiSanTests
{
    private static readonly DateOnly NgayTraCuu = new(2100, 1, 4);
    private const string GioTraCuu = "ngaySuDung=2100-01-04&gioBatDau=10:00&gioKetThuc=11:00";

    private static async Task<UngDungKiemThu> TaoAppKhaDungAsync()
    {
        var app = await TaoAppAsync();
        await ThemDuLieuTraCuuAsync(app);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Chỉ các cột đọc bởi EXISTS trong SQLite RAM; FK/transaction được test riêng trên SQL Server.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE DatSan (
                MaDatSan INTEGER PRIMARY KEY AUTOINCREMENT,
                MaSan INTEGER NOT NULL REFERENCES SanTheThao(MaSan),
                GioBatDau TEXT NOT NULL, GioKetThuc TEXT NOT NULL, TrangThai INTEGER NOT NULL
            );
            """);
        return app;
    }

    [Theory]
    [InlineData("ngaySuDung=2100-01-04")]
    [InlineData("gioBatDau=10:00")]
    [InlineData("ngaySuDung=2100-01-04&gioBatDau=10:00")]
    [InlineData("ngaySuDung=sai&gioBatDau=10:00&gioKetThuc=11:00")]
    [InlineData("ngaySuDung=2100-01-04&gioBatDau=11:00&gioKetThuc=10:00")]
    [InlineData("ngaySuDung=2100-01-04&gioBatDau=10:00&gioKetThuc=10:00")]
    public async Task KhungGioSaiBaoLoiVaKhongTraDanhSach(string query)
    {
        using var app = await TaoAppKhaDungAsync(); using var client = app.TaoClient();
        var html = await client.GetStringAsync("/SanTheThao?" + query);
        Assert.Contains("validation-summary-errors", html);
        Assert.Empty(TenSanTrongBang(html));
    }

    [Fact]
    public async Task QuaKhuVanBaoLoiKhiKhongCoUngVien()
    {
        using var app = await TaoAppKhaDungAsync(); using var client = app.TaoClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(
            "/SanTheThao?tuKhoa=khong-ton-tai&ngaySuDung=2000-01-01&gioBatDau=10:00&gioKetThuc=11:00"));
        Assert.Contains("trong tương lai", html);
        Assert.DoesNotContain("Không có sân phù hợp", html);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.ChoXuLy, true)]
    [InlineData(TrangThaiDatSan.DangXuLy, true)]
    [InlineData(TrangThaiDatSan.HoanThanh, true)]
    [InlineData(TrangThaiDatSan.DaHuy, false)]
    [InlineData(TrangThaiDatSan.TuChoi, false)]
    public async Task LichTrungVaLichLienKeTheoHopDongM4(TrangThaiDatSan trangThai, bool chan)
    {
        using var app = await TaoAppKhaDungAsync(); using var client = app.TaoClient();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO DatSan (MaSan, GioBatDau, GioKetThuc, TrangThai) VALUES ({3}, {NgayTraCuu.ToDateTime(new(10, 0))}, {NgayTraCuu.ToDateTime(new(11, 0))}, {(int)trangThai})");
        }
        var html = await client.GetStringAsync("/SanTheThao?tuKhoa=Alpha&" + GioTraCuu);
        Assert.Equal(!chan, TenSanTrongBang(html).Contains("Alpha"));
        var lienKe = await client.GetStringAsync("/SanTheThao?tuKhoa=Alpha&ngaySuDung=2100-01-04&gioBatDau=11:00&gioKetThuc=12:00");
        Assert.Equal(["Alpha"], TenSanTrongBang(lienKe));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("khachhang")]
    [InlineData("admin")]
    public async Task ChiTraSanNhanDatDungNgayVaTrongGio(string? taiKhoan)
    {
        using var app = await TaoAppKhaDungAsync(); using var client = app.TaoClient();
        if (taiKhoan is not null) await DangNhapAsync(app, client, taiKhoan);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var san = await db.SanTheThaos.SingleAsync(x => x.MaSan == 5);
            san.NgayBaoTri = NgayTraCuu;
            await db.SaveChangesAsync();
        }
        var html = await client.GetStringAsync("/SanTheThao?" + GioTraCuu);
        Assert.Equal(["Alpha", "Beta"], TenSanTrongBang(html));
        Assert.Empty(TenSanTrongBang(await client.GetStringAsync(
            "/SanTheThao?ngaySuDung=2100-01-04&gioBatDau=05:00&gioKetThuc=06:00")));
        // Bỏ ngày giờ vẫn giữ hành vi danh sách thông thường.
        Assert.Contains("Gamma", TenSanTrongBang(await client.GetStringAsync("/SanTheThao")));
    }

    [Fact]
    public async Task ChiaLoHon200SanLocTruocPhanTrangVaGiuNgayGio()
    {
        using var app = await TaoAppKhaDungAsync(); using var client = app.TaoClient();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 1; i <= 205; i++)
                db.SanTheThaos.Add(new SanTheThao
                {
                    TenSan = $"M2Lo {i:000}", MaLoaiSan = 1, DiaChi = "Hà Nội", DonGia = 200000,
                    GioMoCua = new(6, 0), GioDongCua = new(22, 0),
                    TrangThai = TrangThaiSan.HoatDong,
                    NgayBaoTri = i <= 194 ? NgayTraCuu : null
                });
            await db.SaveChangesAsync();
        }
        var html = await client.GetStringAsync("/SanTheThao?tuKhoa=" + Uri.EscapeDataString("M2Lo") + "&" + GioTraCuu);
        Assert.Equal(10, TenSanTrongBang(html).Length);
        Assert.Equal("M2Lo 195", TenSanTrongBang(html)[0]);
        var url = WebUtility.HtmlDecode(Regex.Match(html, "href=\"([^\"]*)\">Sau</a>").Groups[1].Value);
        Assert.Contains("ngaySuDung=2100-01-04", url);
        Assert.Contains("gioBatDau=10", url);
        Assert.Contains("gioKetThuc=11", url);
        var hai = await client.GetStringAsync(url);
        Assert.Equal(["M2Lo 205"], TenSanTrongBang(hai));
        Assert.Contains("Trang 2 / 2", hai);
        var xem = WebUtility.HtmlDecode(Regex.Match(html, "href=\"([^\"]*SanTheThao/Details[^\"]*)\"").Groups[1].Value);
        Assert.Contains("ngay=2100-01-04", xem);
        Assert.Contains("batDau=10", xem);
        Assert.Contains("ketThuc=11", xem);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(xem)).StatusCode);
    }

    [Fact]
    public async Task ServiceChanKhungGioThieuVaTruyenHuyYeuCau()
    {
        using var app = await TaoAppKhaDungAsync();
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISanTheThaoService>();
        var thieu = await service.LayDanhSachAsync(new(NgaySuDung: NgayTraCuu), default);
        Assert.Equal(MaLoiSan.DuLieuKhongHopLe, thieu.MaLoi);
        var dao = await service.LayDanhSachAsync(new(NgaySuDung: NgayTraCuu,
            GioBatDau: new(11, 0), GioKetThuc: new(10, 0)), default);
        Assert.Equal(MaLoiSan.KhoangGioKhongHopLe, dao.MaLoi);
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.LayDanhSachAsync(
            new(NgaySuDung: NgayTraCuu, GioBatDau: new(10, 0), GioKetThuc: new(11, 0)), cts.Token));
    }
}

[Trait("Category", "SqlServer")]
public class KhaDungSanSqlServerTests(TraCuuSanSqlServerFixture fixture) : IClassFixture<TraCuuSanSqlServerFixture>
{
    private sealed class KhachTraCuu : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => null;
        public string? HoTen => null;
        public VaiTro? VaiTro => null;
    }

    private static SanTheThaoService TaoService(ApplicationDbContext db)
    {
        var luu = new DonViCongViec(db);
        return new(new SanTheThaoRepository(db), luu, new KhachTraCuu(),
            new DatSanService(new DatSanRepository(db), luu, new DongHoHeThong(), new TinhTienService()));
    }

    [M2SqlServerTheory]
    [InlineData(TrangThaiDatSan.ChoXuLy, true)]
    [InlineData(TrangThaiDatSan.DangXuLy, true)]
    [InlineData(TrangThaiDatSan.HoanThanh, true)]
    [InlineData(TrangThaiDatSan.DaHuy, false)]
    [InlineData(TrangThaiDatSan.TuChoi, false)]
    public async Task SqlServerLocLichTrungVaDocLaiKhiLichThayDoi(TrangThaiDatSan trangThai, bool chan)
    {
        await using var db = fixture.TaoDb();
        var nhan = "KD" + Guid.NewGuid().ToString("N");
        var san = new SanTheThao
        {
            TenSan = nhan, MaLoaiSan = 1, DiaChi = "Hà Nội", DonGia = 200000,
            GioMoCua = new(6, 0), GioDongCua = new(22, 0), TrangThai = TrangThaiSan.HoatDong
        };
        db.SanTheThaos.Add(san); await db.SaveChangesAsync();
        var service = TaoService(db);
        var loc = new TraCuuSan(TuKhoa: nhan, MaLoaiSan: 1, GiaTu: 200000, GiaDen: 200000,
            NgaySuDung: new(2100, 1, 4), GioBatDau: new(10, 0), GioKetThuc: new(11, 0));
        Assert.Single((await service.LayDanhSachAsync(loc, default)).DuLieu.DanhSach);
        var khach = new KhachHang
        {
            TaiKhoan = new TaiKhoan { TenDangNhap = nhan, HoTen = "Thử", Email = "thu@example.test", MatKhau = "hash-chi-dung-test", VaiTro = VaiTro.KhachHang },
            HoTen = "Thử", Email = "thu@example.test", SoDienThoai = "0900000000", NgayDangKy = new(2099, 1, 1)
        };
        db.DatSans.Add(new DatSan
        {
            KhachHang = khach, MaSan = san.MaSan, NgayDat = new(2099, 1, 1),
            GioBatDau = new(2100, 1, 4, 10, 0, 0), GioKetThuc = new(2100, 1, 4, 11, 0, 0),
            DonGia = 200000, TrangThai = trangThai
        });
        await db.SaveChangesAsync();
        Assert.Equal(chan ? 0 : 1, (await service.LayDanhSachAsync(loc, default)).DuLieu.TongSoSan);
        Assert.Single((await service.LayDanhSachAsync(loc with { GioBatDau = new(11, 0), GioKetThuc = new(12, 0) }, default)).DuLieu.DanhSach);
    }

    [M2SqlServerFact]
    public async Task SqlServerChiaLoVaLocTruocCountSkipTake()
    {
        await using var db = fixture.TaoDb();
        var nhan = "LO" + Guid.NewGuid().ToString("N");
        var ngay = new DateOnly(2100, 1, 4);
        for (var i = 1; i <= 205; i++)
            db.SanTheThaos.Add(new SanTheThao
            {
                TenSan = $"{nhan} {i:000}", MaLoaiSan = 1, DiaChi = "Hà Nội", DonGia = 200000,
                GioMoCua = new(6, 0), GioDongCua = new(22, 0), TrangThai = TrangThaiSan.HoatDong,
                NgayBaoTri = i <= 194 ? ngay : null
            });
        await db.SaveChangesAsync();
        var service = TaoService(db);
        var loc = new TraCuuSan(TuKhoa: nhan, GiaTu: 200000, GiaDen: 200000, SapXep: "ten_tang",
            NgaySuDung: ngay, GioBatDau: new(10, 0), GioKetThuc: new(11, 0));
        var mot = (await service.LayDanhSachAsync(loc, default)).DuLieu;
        var hai = (await service.LayDanhSachAsync(loc with { Trang = 2 }, default)).DuLieu;
        Assert.Equal(11, mot.TongSoSan); Assert.Equal(2, mot.TongTrang);
        Assert.Equal(10, mot.DanhSach.Count); Assert.Single(hai.DanhSach);
        Assert.EndsWith("195", mot.DanhSach[0].TenSan);
        Assert.EndsWith("205", hai.DanhSach[0].TenSan);
        Assert.Empty(mot.DanhSach.Select(x => x.MaSan).Intersect(hai.DanhSach.Select(x => x.MaSan)));
    }
}

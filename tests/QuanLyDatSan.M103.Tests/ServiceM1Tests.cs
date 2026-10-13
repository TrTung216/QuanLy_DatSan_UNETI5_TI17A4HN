// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử ranh giới service và hồi quy xác thực.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class ServiceM1Tests
{
    [Theory]
    [InlineData(typeof(ILoaiSanService))]
    [InlineData(typeof(IQuanLyTaiKhoanService))]
    [InlineData(typeof(IXacThucTaiKhoanService))]
    [InlineData(typeof(IDichVuService))]
    public void DiUngDungDangKyMotLanVaDungVongDoiScoped(Type kieuService)
    {
        using var app = new UngDungKiemThu();
        using var scope1 = app.Services.CreateScope();
        using var scope2 = app.Services.CreateScope();
        var service = Assert.Single(scope1.ServiceProvider.GetServices(kieuService));
        Assert.NotNull(service);
        Assert.Same(service, scope1.ServiceProvider.GetRequiredService(kieuService));
        Assert.NotSame(service, scope2.ServiceProvider.GetRequiredService(kieuService));
    }

    private sealed class NguoiThaoTac(int? id) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => id;
        public string HoTen => "Kiểm thử";
        // Cố tình giữ claim Admin cũ để chứng minh service kiểm tra quyền thực tế trong DB.
        public VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.Admin;
    }

    [Theory]
    [InlineData(null)]
    [InlineData(2)]
    public async Task GoiTrucTiepServiceVanKiemTraQuyenTrongTransaction(int? maNguoiThaoTac)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = new QuanLyTaiKhoanService(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>(),
            new NguoiThaoTac(maNguoiThaoTac), scope.ServiceProvider.GetRequiredService<PhienBanQuyenTaiKhoan>());
        var ketQua = await service.SuaAsync(3, new("Không được lưu", "test@example.test", VaiTro.Admin, TrangThaiTaiKhoan.HoatDong), default);
        Assert.Contains(MaLoiTaiKhoan.KhongDuQuyen, ketQua.Loi);
        Assert.Equal(VaiTro.KhachHang, (await app.DocTaiKhoanAsync(3)).VaiTro);
        Assert.Null(db.Database.CurrentTransaction);
        var tao = await service.TaoAsync(new TaoTaiKhoan
        {
            TenDangNhap = "khong_duoc_tao", MatKhau = app.MatKhau,
            HoTen = "Không được lưu", Email = "test@example.test", VaiTro = VaiTro.Admin
        }, default);
        Assert.Contains(MaLoiTaiKhoan.KhongDuQuyen, tao.Loi);
        Assert.Equal(3, await db.TaiKhoans.CountAsync());
        Assert.Null(db.Database.CurrentTransaction);
    }

    [Fact]
    public async Task LoiSauSaveChangesRollbackVaKhongThuHoiPhien()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>(scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>())
            .AddInterceptors(new LoiSauKhiGhi()).Options;
        await using var db = new ApplicationDbContext(options);
        var phienBan = scope.ServiceProvider.GetRequiredService<PhienBanQuyenTaiKhoan>();
        var truoc = phienBan.Lay(2);
        var service = new QuanLyTaiKhoanService(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>(), new NguoiThaoTac(1), phienBan);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuaAsync(2,
            new("Không được lưu", "test@example.test", VaiTro.KhachHang, TrangThaiTaiKhoan.BiKhoa), default));
        Assert.Equal(truoc, phienBan.Lay(2));
        var taiKhoan = await app.DocTaiKhoanAsync(2);
        Assert.Equal("nhanvien", taiKhoan.HoTen);
        Assert.Equal(VaiTro.NhanVien, taiKhoan.VaiTro);
        Assert.Equal(TrangThaiTaiKhoan.HoatDong, taiKhoan.TrangThai);
        Assert.Null(db.Database.CurrentTransaction);
    }

    private sealed class LoiSauKhiGhi : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            Assert.NotNull(eventData.Context!.Database.CurrentTransaction);
            throw new InvalidOperationException("Lỗi kiểm thử sau SQL, trước commit.");
        }
    }

    [Fact]
    public async Task ServiceTuChanDuLieuTaoKhongHopLe()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IQuanLyTaiKhoanService>();
        var ketQua = await service.TaoAsync(new TaoTaiKhoan
        {
            TenDangNhap = "moi", MatKhau = "ngan", HoTen = "Họ tên", Email = "sai", VaiTro = (VaiTro)99
        }, default);
        Assert.Contains(MaLoiTaiKhoan.DuLieuKhongHopLe, ketQua.Loi);
        Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TaiKhoans.CountAsync());
    }

    [Theory]
    [InlineData("sai_mat_khau")]
    [InlineData("khong_ton_tai")]
    [InlineData("bi_khoa")]
    [InlineData("hash_hong")]
    [InlineData("vai_tro_sai")]
    public async Task XacThucThatBaiKhongTraThongTinTaiKhoan(string tinhHuong)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tk = await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2);
        if (tinhHuong == "bi_khoa") tk.TrangThai = TrangThaiTaiKhoan.BiKhoa;
        if (tinhHuong == "hash_hong") tk.MatKhau = "khong-phai-base64";
        await db.SaveChangesAsync();
        // Dữ liệu hỏng chỉ nằm trong tracked entity của test, không bỏ constraint database.
        if (tinhHuong == "vai_tro_sai") tk.VaiTro = (VaiTro)99;
        var ketQua = await scope.ServiceProvider.GetRequiredService<IXacThucTaiKhoanService>().XacThucAsync(new()
        {
            TenDangNhap = tinhHuong == "khong_ton_tai" ? "khongco" : "nhanvien",
            MatKhau = tinhHuong == "sai_mat_khau" ? "matkhau_sai" : app.MatKhau
        }, default);
        Assert.Equal(MaLoiXacThuc.ThongTinKhongHopLe, ketQua.MaLoi);
        Assert.Null(ketQua.TaiKhoan);
    }

    [Fact]
    public async Task XacThucNangCapHashCuVaTraDtoKhongCoMatKhau()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tk = await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2);
        tk.MatKhau = new PasswordHasher<TaiKhoan>(Options.Create(new PasswordHasherOptions
        { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 })).HashPassword(tk, app.MatKhau);
        await db.SaveChangesAsync();
        var hashCu = tk.MatKhau;
        var ketQua = await scope.ServiceProvider.GetRequiredService<IXacThucTaiKhoanService>().XacThucAsync(new()
        { TenDangNhap = " NHANVIEN ", MatKhau = app.MatKhau }, default);
        Assert.Equal(MaLoiXacThuc.ThanhCong, ketQua.MaLoi);
        Assert.Equal(2, ketQua.TaiKhoan!.MaTaiKhoan);
        var daLuu = await app.DocTaiKhoanAsync(2);
        Assert.NotEqual(hashCu, daLuu.MatKhau);
        Assert.Equal(PasswordVerificationResult.Success, scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>()
            .VerifyHashedPassword(daLuu, daLuu.MatKhau, app.MatKhau));
        Assert.DoesNotContain(app.MatKhau, ketQua.ToString());
        Assert.DoesNotContain(daLuu.MatKhau, ketQua.ToString());
    }

    [Theory]
    [InlineData("/QuanLyTaiKhoan/DanhSach", "/QuanLyTaiKhoan/DanhSach")]
    [InlineData("https://example.test/ngoai", "/TaiKhoan/ThongTin")]
    [InlineData("//example.test/ngoai", "/TaiKhoan/ThongTin")]
    public async Task DangNhapGiuReturnUrlNoiBoVaDangXuatXoaPhien(string returnUrl, string dich)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = "admin", ["MatKhau"] = app.MatKhau, ["ReturnUrl"] = returnUrl,
            ["__RequestVerificationToken"] = Token(await client.GetStringAsync("/TaiKhoan/DangNhap"))
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(dich, response.Headers.Location!.OriginalString);
        var html = await client.GetStringAsync("/TaiKhoan/ThongTin");
        Assert.Contains("admin", html);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/TaiKhoan/DangXuat", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        var logout = await client.PostAsync("/TaiKhoan/DangXuat", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = Token(html) }));
        Assert.Equal("/TaiKhoan/DangNhap", logout.Headers.Location!.OriginalString);
        Assert.Contains("/TaiKhoan/DangNhap", (await client.GetAsync("/TaiKhoan/ThongTin")).Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("sai_mat_khau")]
    [InlineData("khong_ton_tai")]
    [InlineData("bi_khoa")]
    [InlineData("hash_hong")]
    public async Task DangNhapHttpGiuLoiChungVaFormKhongTaoPhien(string tinhHuong)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tk = await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 2);
            if (tinhHuong == "bi_khoa") tk.TrangThai = TrangThaiTaiKhoan.BiKhoa;
            if (tinhHuong == "hash_hong") tk.MatKhau = "khong-phai-base64";
            await db.SaveChangesAsync();
        }
        var hashTruoc = (await app.DocTaiKhoanAsync(2)).MatKhau;
        using var client = app.TaoClient();
        var ten = tinhHuong == "khong_ton_tai" ? "khongco" : "nhanvien";
        var matKhau = tinhHuong == "sai_mat_khau" ? app.MatKhau + "sai" : app.MatKhau;
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = ten, ["MatKhau"] = matKhau, ["ReturnUrl"] = "/LoaiSan/DanhSach",
            ["__RequestVerificationToken"] = Token(await client.GetStringAsync("/TaiKhoan/DangNhap"))
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa.", html);
        Assert.Contains($"value=\"{ten}\"", html);
        Assert.Contains("value=\"/LoaiSan/DanhSach\"", html);
        Assert.DoesNotContain(matKhau, html);
        Assert.DoesNotContain(hashTruoc, html);
        Assert.Equal(hashTruoc, (await app.DocTaiKhoanAsync(2)).MatKhau);
        var thongTin = await client.GetAsync("/TaiKhoan/ThongTin");
        Assert.Equal(HttpStatusCode.Redirect, thongTin.StatusCode);
        Assert.Contains("/TaiKhoan/DangNhap", thongTin.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task DangNhapHttpThieuAntiforgeryKhongTaoPhien()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["TenDangNhap"] = "admin", ["MatKhau"] = app.MatKhau }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var thongTin = await client.GetAsync("/TaiKhoan/ThongTin");
        Assert.Equal(HttpStatusCode.Redirect, thongTin.StatusCode);
        Assert.Contains("/TaiKhoan/DangNhap", thongTin.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task DangNhapHttpGiuGioiHanMuoiLanThu()
    {
        using var app = new UngDungKiemThu();
        using var client = app.TaoClient();
        var form = new Dictionary<string, string>
        {
            ["TenDangNhap"] = "khongco", ["MatKhau"] = app.MatKhau,
            ["__RequestVerificationToken"] = Token(await client.GetStringAsync("/TaiKhoan/DangNhap"))
        };
        for (var lan = 0; lan < 10; lan++)
        {
            using var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var biChan = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.TooManyRequests, biChan.StatusCode);
        Assert.Equal("Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau một phút.", await biChan.Content.ReadAsStringAsync());
    }

    private static string Token(string html) => WebUtility.HtmlDecode(
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
}

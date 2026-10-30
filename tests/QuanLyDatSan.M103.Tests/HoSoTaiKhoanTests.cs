using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;
// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm thử tạo/bổ sung hồ sơ từ quản lý tài khoản và đồng bộ dữ liệu M1–M3.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class HoSoTaiKhoanTests
{
    private static async Task<HttpResponseMessage> Gui(HttpClient client, string path, Dictionary<string, string> form)
    {
        var html = await client.GetStringAsync(path);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        form["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    private static async Task DangNhap(UngDungKiemThu app, HttpClient client, string ten = "admin")
        => Assert.Equal(HttpStatusCode.Redirect, (await Gui(client, "/TaiKhoan/DangNhap", new()
        { ["TenDangNhap"] = ten, ["MatKhau"] = app.MatKhau })).StatusCode);

    private static Dictionary<string, string> TaoForm(UngDungKiemThu app) => new()
    {
        ["TenDangNhap"] = "khach_moi", ["HoTen"] = "  Khách mới  ", ["Email"] = "khachmoi@example.test",
        ["VaiTro"] = "3", ["MatKhau"] = app.MatKhau, ["XacNhanMatKhau"] = app.MatKhau,
        ["SoDienThoai"] = " 0912345678 "
    };

    private static Dictionary<string, string> SuaForm() => new()
    {
        ["HoTen"] = "Tên cập nhật", ["Email"] = "capnhat@example.test", ["VaiTro"] = "3", ["TrangThai"] = "1"
    };

    [Fact]
    public async Task AdminTaoKhachKemHoSoDungThongTinVaKhongNhanTruongHeThong()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var client = app.TaoClient(); await DangNhap(app, client);
        using var scope = app.Services.CreateScope();
        var clock = scope.ServiceProvider.GetRequiredService<IDongHo>();
        var truoc = clock.BayGio;
        var form = TaoForm(app);
        form["MaTaiKhoan"] = "1"; form["DiemTichLuy"] = "999"; form["TrangThai"] = "0";
        form["NgayDangKy"] = "2000-01-01";
        Assert.Equal(HttpStatusCode.Redirect, (await Gui(client, "/QuanLyTaiKhoan/Tao", form)).StatusCode);
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tk = await db.TaiKhoans.AsNoTracking().Include(x => x.KhachHang).SingleAsync(x => x.TenDangNhap == "khach_moi");
        var kh = Assert.IsType<KhachHang>(tk.KhachHang);
        Assert.Equal("Khách mới", kh.HoTen); Assert.Equal(tk.HoTen, kh.HoTen); Assert.Equal(tk.Email, kh.Email);
        Assert.Equal("0912345678", kh.SoDienThoai); Assert.Equal(tk.MaTaiKhoan, kh.MaTaiKhoan);
        Assert.Equal(0, kh.DiemTichLuy); Assert.Equal(TrangThaiKhachHang.HoatDong, kh.TrangThai);
        Assert.Equal(TrangThaiTaiKhoan.HoatDong, tk.TrangThai);
        Assert.InRange(kh.NgayDangKy, truoc, clock.BayGio);
        // Màn hình hồ sơ M3 đọc được ngay dữ liệu tạo từ M1.
        using var khachClient = app.TaoClient(); await DangNhap(app, khachClient, "khach_moi");
        Assert.Contains("0912345678", await khachClient.GetStringAsync("/HoSo"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("0912345678901234567890")]
    public async Task TaoKhachThieuHoacSaiSoDienThoaiKhongLuuTaiKhoanRiengLe(string? soDienThoai)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var client = app.TaoClient(); await DangNhap(app, client);
        var form = TaoForm(app); form.Remove("SoDienThoai");
        if (soDienThoai is not null) form["SoDienThoai"] = soDienThoai;
        var response = await Gui(client, "/QuanLyTaiKhoan/Tao", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("field-validation-error", await response.Content.ReadAsStringAsync());
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(3, await db.TaiKhoans.CountAsync()); Assert.Empty(await db.KhachHangs.ToListAsync());
    }

    [Theory]
    [InlineData(3)] // Khách cũ chưa có hồ sơ.
    [InlineData(2)] // Nhân viên được chuyển vai trò thành khách.
    public async Task SuaBoSungHoSoConThieuCanSoDienThoaiVaKhongTaoTrung(int id)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var client = app.TaoClient(); await DangNhap(app, client);
        var path = "/QuanLyTaiKhoan/Sua/" + id;
        var form = SuaForm();
        Assert.Contains("data-ho-so-khach", await client.GetStringAsync(path));
        Assert.Equal(HttpStatusCode.OK, (await Gui(client, path, form)).StatusCode);
        Assert.Equal(id == 3 ? "khachhang" : "nhanvien", (await app.DocTaiKhoanAsync(id)).HoTen);
        form["SoDienThoai"] = "+84912345678";
        Assert.Equal(HttpStatusCode.Redirect, (await Gui(client, path, form)).StatusCode);
        Assert.DoesNotContain("data-ho-so-khach", await client.GetStringAsync(path));
        form["SoDienThoai"] = "0987654321";
        Assert.Equal(HttpStatusCode.Redirect, (await Gui(client, path, form)).StatusCode);
        using var scope = app.Services.CreateScope();
        var kh = Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().KhachHangs.ToListAsync());
        Assert.Equal(id, kh.MaTaiKhoan); Assert.Equal("+84912345678", kh.SoDienThoai);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("2")]
    public async Task SuaTaiKhoanDongBoHoTenEmailVaGiuNguyenHoSoDaCo(string vaiTro)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        var ngayDangKy = new DateTime(2020, 1, 2, 9, 0, 0);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.KhachHangs.Add(new KhachHang { MaTaiKhoan = 3, HoTen = "khachhang", Email = "khachhang@example.test",
                SoDienThoai = "0912345678", TrangThai = TrangThaiKhachHang.BiKhoa, DiemTichLuy = 25,
                NgayDangKy = ngayDangKy, DiaChi = "Địa chỉ cũ", GhiChu = "Giữ lịch sử" });
            await db.SaveChangesAsync();
        }
        using var client = app.TaoClient(); await DangNhap(app, client);
        var form = SuaForm(); form["VaiTro"] = vaiTro;
        form["SoDienThoai"] = "0987654321"; form["DiemTichLuy"] = "999"; form["MaKhachHang"] = "999";
        Assert.Equal(HttpStatusCode.Redirect, (await Gui(client, "/QuanLyTaiKhoan/Sua/3", form)).StatusCode);
        using var readScope = app.Services.CreateScope();
        var tk = await readScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().TaiKhoans
            .AsNoTracking().Include(x => x.KhachHang).SingleAsync(x => x.MaTaiKhoan == 3);
        var kh = Assert.IsType<KhachHang>(tk.KhachHang);
        Assert.Equal("Tên cập nhật", tk.HoTen); Assert.Equal(tk.HoTen, kh.HoTen); Assert.Equal(tk.Email, kh.Email);
        Assert.Equal("0912345678", kh.SoDienThoai); Assert.Equal(TrangThaiKhachHang.BiKhoa, kh.TrangThai);
        Assert.Equal(25, kh.DiemTichLuy); Assert.Equal(ngayDangKy, kh.NgayDangKy);
        Assert.Equal("Địa chỉ cũ", kh.DiaChi); Assert.Equal("Giữ lịch sử", kh.GhiChu);
    }

    [Fact]
    public async Task ServiceChanThieuDienThoaiVaRollbackCaTaiKhoanHoSoNeuLoiSauGhi()
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>(scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>())
            .AddInterceptors(new LoiSauGhi()).Options;
        await using var db = new ApplicationDbContext(options);
        var service = new QuanLyTaiKhoanService(new TaiKhoanRepository(db), new DonViCongViec(db, NullLogger<DonViCongViec>.Instance), scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>(),
            new AdminTest(), scope.ServiceProvider.GetRequiredService<PhienBanQuyenTaiKhoan>(), scope.ServiceProvider.GetRequiredService<IDongHo>());
        var request = new TaoTaiKhoan { TenDangNhap = "rollback", HoTen = "Khách mới", Email = "moi@example.test",
            MatKhau = app.MatKhau, VaiTro = VaiTro.KhachHang };
        Assert.Contains(MaLoiTaiKhoan.SoDienThoaiHoSoKhongHopLe, (await service.TaoAsync(request, default)).Loi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TaoAsync(new TaoTaiKhoan
        { TenDangNhap = request.TenDangNhap, HoTen = request.HoTen, Email = request.Email,
            MatKhau = request.MatKhau, VaiTro = request.VaiTro, SoDienThoai = "0912345678" }, default));
        using var checkScope = app.Services.CreateScope();
        var check = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(3, await check.TaiKhoans.CountAsync()); Assert.Empty(await check.KhachHangs.ToListAsync());
    }

    [Fact]
    public void DongHoChiDangKyMotSingleton()
    {
        using var app = new UngDungKiemThu(); using var a = app.Services.CreateScope(); using var b = app.Services.CreateScope();
        var clock = Assert.Single(a.ServiceProvider.GetServices<IDongHo>());
        Assert.Same(clock, b.ServiceProvider.GetRequiredService<IDongHo>());
    }

    private sealed class AdminTest : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => 1;
        public string? HoTen => "Admin";
        public VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.Admin;
    }
    private sealed class LoiSauGhi : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Lỗi kiểm thử sau SaveChanges, trước commit.");
    }
}

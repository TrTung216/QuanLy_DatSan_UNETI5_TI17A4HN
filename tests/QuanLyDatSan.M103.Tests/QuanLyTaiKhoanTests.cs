// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ kiểm thử nghiệm thu M1-03.
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class QuanLyTaiKhoanTests
{
    private const string Goc = "/QuanLyTaiKhoan";

    private static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "Không tìm thấy anti-forgery token.");
        return WebUtility.HtmlDecode(token.Groups[1].Value);
    }

    private static async Task DangNhapAsync(UngDungKiemThu app, HttpClient client, string ten)
    {
        var token = await TokenAsync(client, "/TaiKhoan/DangNhap");
        var response = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["TenDangNhap"] = ten, ["MatKhau"] = app.MatKhau, ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/TaiKhoan/ThongTin", response.Headers.Location?.OriginalString);
    }

    private static Dictionary<string, string> TaoForm(UngDungKiemThu app, string ten = "taikhoan_moi") => new()
    {
        ["TenDangNhap"] = ten, ["HoTen"] = "Người dùng mới", ["Email"] = "moi@example.test",
        ["VaiTro"] = "2", ["MatKhau"] = app.MatKhau, ["XacNhanMatKhau"] = app.MatKhau
    };

    private static Dictionary<string, string> SuaForm(string vaiTro = "2", string trangThai = "1") => new()
    {
        ["HoTen"] = "Họ tên cập nhật", ["Email"] = "capnhat@example.test", ["VaiTro"] = vaiTro, ["TrangThai"] = trangThai
    };

    private static async Task<HttpResponseMessage> GuiAsync(HttpClient client, string path, Dictionary<string, string> form)
    {
        form["__RequestVerificationToken"] = await TokenAsync(client, path);
        return await client.PostAsync(path, new FormUrlEncodedContent(form));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("nhanvien")]
    [InlineData("khachhang")]
    public async Task ChanTruyCapTrucTiepVaPostCuaNguoiKhongDuQuyen(string? ten)
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        if (ten is not null) await DangNhapAsync(app, client, ten);
        var token = await TokenAsync(client, ten is null ? "/TaiKhoan/DangNhap" : "/TaiKhoan/ThongTin");
        foreach (var path in new[] { Goc + "/DanhSach", Goc + "/Tao", Goc + "/Sua/2" })
        {
            var get = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
            Assert.StartsWith(ten is null ? "https://localhost/TaiKhoan/DangNhap" : "https://localhost/TaiKhoan/TuChoiTruyCap", get.Headers.Location!.OriginalString);
        }
        foreach (var path in new[] { Goc + "/Tao", Goc + "/Sua/2" })
        {
            var form = path.EndsWith("Tao") ? TaoForm(app) : SuaForm("1");
            form["__RequestVerificationToken"] = token;
            var post = await client.PostAsync(path, new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        }
        var home = await client.GetStringAsync("/");
        Assert.DoesNotContain("/QuanLyTaiKhoan/", home);
        Assert.Equal(VaiTro.NhanVien, (await app.DocTaiKhoanAsync(2)).VaiTro);
        if (ten is not null)
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/TaiKhoan/TuChoiTruyCap")).StatusCode);
    }

    [Fact]
    public async Task AdminTaoSuaVaLocTaiKhoanKhongLoMatKhau()
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "admin");
        var form = TaoForm(app, "  Moi  ");
        form["TrangThai"] = "0"; // Trường ngoài ViewModel không được quyết định trạng thái lúc tạo.
        form["MaTaiKhoan"] = "1";
        form["HoTen"] = "  Tên mới  ";
        var tao = await GuiAsync(client, Goc + "/Tao", form);
        Assert.Equal(HttpStatusCode.Redirect, tao.StatusCode);
        var tk = await app.DocTaiKhoanAsync(4);
        Assert.Equal("Moi", tk.TenDangNhap);
        Assert.Equal("Tên mới", tk.HoTen);
        Assert.Equal(TrangThaiTaiKhoan.HoatDong, tk.TrangThai);
        using var scope = app.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(tk, tk.MatKhau, app.MatKhau));
        var list = WebUtility.HtmlDecode(await client.GetStringAsync(Goc + "/DanhSach?tuKhoa=Moi&vaiTro=2&trangThai=1&trang=999"));
        Assert.Contains("Moi", list);
        Assert.Contains("1 tài khoản phù hợp", list);
        Assert.DoesNotContain(tk.MatKhau, list);
        Assert.DoesNotContain(app.MatKhau, list);
        var sua = SuaForm("3", "0");
        sua["TenDangNhap"] = "doi_ten_trai_phep";
        sua["MatKhau"] = "khong-duoc-luu";
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(client, Goc + "/Sua/4", sua)).StatusCode);
        var updated = await app.DocTaiKhoanAsync(4);
        Assert.Equal(VaiTro.KhachHang, updated.VaiTro);
        Assert.Equal(TrangThaiTaiKhoan.BiKhoa, updated.TrangThai);
        Assert.Equal("Moi", updated.TenDangNhap);
        Assert.Equal(tk.MatKhau, updated.MatKhau);
        var queryEdit = await client.GetStringAsync(Goc + "/Sua?id=4");
        Assert.Contains("action=\"/QuanLyTaiKhoan/Sua/4\"", queryEdit);
        Assert.Contains("aria-current=\"page\" href=\"/QuanLyTaiKhoan/DanhSach\"", list);
    }

    [Theory]
    [InlineData("TenDangNhap", "   ")]
    [InlineData("HoTen", "   ")]
    [InlineData("Email", "khong-phai-email")]
    [InlineData("VaiTro", "999")]
    [InlineData("VaiTro", "")]
    [InlineData("MatKhau", "ngan")]
    [InlineData("XacNhanMatKhau", "khong-khop")]
    public async Task TuChoiDuLieuTaoKhongHopLe(string key, string value)
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "admin");
        var form = TaoForm(app);
        form[key] = value;
        var response = await GuiAsync(client, Goc + "/Tao", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("field-validation-error", html);
        Assert.DoesNotContain(app.MatKhau, html);
        Assert.Contains("3 tài khoản phù hợp", WebUtility.HtmlDecode(await client.GetStringAsync(Goc + "/DanhSach")));
    }

    [Fact]
    public async Task TuChoiTrungTenVaThieuAntiForgery()
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "admin");
        var trung = await GuiAsync(client, Goc + "/Tao", TaoForm(app, " ADMIN "));
        Assert.Contains("Tên đăng nhập đã được sử dụng", WebUtility.HtmlDecode(await trung.Content.ReadAsStringAsync()));
        foreach (var path in new[] { Goc + "/Tao", Goc + "/Sua/2" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, new FormUrlEncodedContent(TaoForm(app)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(Goc + "/Sua/999")).StatusCode);
        var token = await TokenAsync(client, Goc + "/Tao");
        var sua = SuaForm();
        sua["__RequestVerificationToken"] = token;
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync(Goc + "/Sua/999", new FormUrlEncodedContent(sua))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(Goc + "/DanhSach?vaiTro=999")).StatusCode);
    }

    [Theory]
    [InlineData("2", "1")]
    [InlineData("1", "0")]
    public async Task KhongTuKhoaHaQuyenHoacMatAdminCuoi(string vaiTro, string trangThai)
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "admin");
        var response = await GuiAsync(client, Goc + "/Sua/1", SuaForm(vaiTro, trangThai));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Admin hoạt động cuối cùng", html);
        var tk = await app.DocTaiKhoanAsync(1);
        Assert.Equal(VaiTro.Admin, tk.VaiTro);
        Assert.Equal(TrangThaiTaiKhoan.HoatDong, tk.TrangThai);
    }

    [Theory]
    [InlineData("VaiTro", "0")]
    [InlineData("TrangThai", "9")]
    [InlineData("TrangThai", "")]
    public async Task TuChoiEnumSuaKhongHopLe(string key, string value)
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "admin");
        var form = SuaForm();
        form[key] = value;
        var response = await GuiAsync(client, Goc + "/Sua/2", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("field-validation-error", await response.Content.ReadAsStringAsync());
        Assert.Equal("nhanvien", (await app.DocTaiKhoanAsync(2)).HoTen);
    }

    [Theory]
    [InlineData("3", "1")]
    [InlineData("2", "0")]
    public async Task ThuHoiMoiPhienCuKeCaKhiKhoiPhucQuyenTruocRequestTiepTheo(string vaiTro, string trangThai)
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var admin = app.TaoClient();
        using var nhanVien1 = app.TaoClient();
        using var nhanVien2 = app.TaoClient();
        await DangNhapAsync(app, admin, "admin");
        await DangNhapAsync(app, nhanVien1, "nhanvien");
        await DangNhapAsync(app, nhanVien2, "nhanvien");
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(admin, Goc + "/Sua/2", SuaForm(vaiTro, trangThai))).StatusCode);
        // Khôi phục trước khi hai phiên cũ gửi bất kỳ request nào.
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(admin, Goc + "/Sua/2", SuaForm())).StatusCode);
        foreach (var client in new[] { nhanVien1, nhanVien2 })
        {
            var response = await client.GetAsync("/TaiKhoan/ThongTin");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/TaiKhoan/DangNhap", response.Headers.Location!.OriginalString);
        }
        await DangNhapAsync(app, nhanVien1, "nhanvien");
        Assert.Equal(HttpStatusCode.OK, (await nhanVien1.GetAsync("/TaiKhoan/ThongTin")).StatusCode);
    }

    [Fact]
    public async Task PhanTrangGiuBoLocVaHienThiTrangRong()
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        await DangNhapAsync(app, client, "admin");
        for (var i = 0; i < 11; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(client, Goc + "/Tao", TaoForm(app, $"loc_{i:00}"))).StatusCode);
        var trang1 = WebUtility.HtmlDecode(await client.GetStringAsync(Goc + "/DanhSach?tuKhoa=loc_&vaiTro=2&trangThai=1"));
        Assert.Contains("11 tài khoản phù hợp", trang1);
        Assert.Contains("loc_00", trang1);
        Assert.DoesNotContain("loc_10", trang1);
        Assert.Contains("tuKhoa=loc_", trang1);
        Assert.Contains("vaiTro=NhanVien", trang1);
        Assert.Contains("trangThai=HoatDong", trang1);
        var trang2 = await client.GetStringAsync(Goc + "/DanhSach?tuKhoa=loc_&vaiTro=2&trangThai=1&trang=2");
        Assert.Contains("loc_10", trang2);
        Assert.DoesNotContain("loc_00", trang2);
        var rong = WebUtility.HtmlDecode(await client.GetStringAsync(Goc + "/DanhSach?tuKhoa=khong_co"));
        Assert.Contains("Chưa có kết quả", rong);
    }

    [Fact]
    public async Task AdminBiHaQuyenMatQuyenQuanTriVaNhanVaiTroMoiSauDangNhapLai()
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var admin = app.TaoClient();
        using var adminMoi = app.TaoClient();
        await DangNhapAsync(app, admin, "admin");
        var form = TaoForm(app, "admin_moi");
        form["VaiTro"] = "1";
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(admin, Goc + "/Tao", form)).StatusCode);
        await DangNhapAsync(app, adminMoi, "admin_moi");
        // Hai Admin tồn tại: vẫn không được tự hạ quyền.
        var tuSua = await GuiAsync(adminMoi, Goc + "/Sua/4", SuaForm());
        Assert.Contains("không thể tự khóa hoặc hạ quyền", WebUtility.HtmlDecode(await tuSua.Content.ReadAsStringAsync()));
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(adminMoi, Goc + "/Sua/1", SuaForm())).StatusCode);
        var hetPhien = await admin.GetAsync(Goc + "/DanhSach");
        Assert.Contains("/TaiKhoan/DangNhap", hetPhien.Headers.Location!.OriginalString);
        await DangNhapAsync(app, admin, "admin");
        var cam = await admin.GetAsync(Goc + "/DanhSach");
        Assert.Contains("/TaiKhoan/TuChoiTruyCap", cam.Headers.Location!.OriginalString);
        Assert.DoesNotContain("/QuanLyTaiKhoan/", await admin.GetStringAsync("/"));
        var token = await TokenAsync(admin, "/TaiKhoan/ThongTin");
        var result = await admin.PostAsync("/TaiKhoan/DangXuat", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Contains("/TaiKhoan/DangNhap", (await admin.GetAsync("/TaiKhoan/ThongTin")).Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task DoiHoTenKhongDangXuatVaKhoaChanDangNhap()
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var admin = app.TaoClient();
        using var nhanVien = app.TaoClient();
        await DangNhapAsync(app, admin, "admin");
        await DangNhapAsync(app, nhanVien, "nhanvien");
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(admin, Goc + "/Sua/2", SuaForm())).StatusCode);
        Assert.Contains("Họ tên cập nhật", WebUtility.HtmlDecode(await nhanVien.GetStringAsync("/TaiKhoan/ThongTin")));
        Assert.Equal(HttpStatusCode.Redirect, (await GuiAsync(admin, Goc + "/Sua/2", SuaForm("2", "0"))).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await nhanVien.GetAsync("/TaiKhoan/ThongTin")).StatusCode);
        var token = await TokenAsync(nhanVien, "/TaiKhoan/DangNhap");
        var response = await nhanVien.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["TenDangNhap"] = "nhanvien", ["MatKhau"] = app.MatKhau, ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tài khoản đã bị khóa", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
    }
}

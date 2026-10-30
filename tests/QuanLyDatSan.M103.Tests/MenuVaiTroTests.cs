// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm thử hợp đồng menu, route/quyền và đăng xuất qua HTTP thật.
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class MenuVaiTroTests
{
    private static ClaimsPrincipal NguoiDung(string? vaiTro, bool xacThuc = true) =>
        new(new ClaimsIdentity(vaiTro is null ? [] : [new Claim(ClaimTypes.Role, vaiTro)],
            vaiTro is not null && xacThuc ? "KiemThu" : null));

    [Theory]
    [InlineData(null)]
    [InlineData("KhachHang")]
    [InlineData("NhanVien")]
    [InlineData("Admin")]
    public async Task MoiMucMenuCoActionGetVaDungQuyenServer(string? vaiTro)
    {
        using var app = new UngDungKiemThu();
        using var scope = app.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var actions = provider.GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToArray();
        var user = NguoiDung(vaiTro);
        var menu = vaiTro is "Admin" or "NhanVien"
            ? MenuQuanTri.Lay(user).SelectMany(x => x.DanhSachMuc)
            : MenuKhachHang.Lay(user);
        var links = menu.Concat(vaiTro is null
            ? [new MucMenu("Đăng nhập", "TaiKhoan", "DangNhap"), new MucMenu("Đăng ký", "KhachHang", "DangKy")]
            : new[] { new MucMenu("Tài khoản", "TaiKhoan", "ThongTin") });
        foreach (var muc in links)
        {
            var action = Assert.Single(actions, a => a.ControllerName == muc.Controller && a.ActionName == muc.Action
                && !(a.ActionConstraints?.OfType<HttpMethodActionConstraint>().Any(c => !c.HttpMethods.Contains("GET")) ?? false));
            Assert.DoesNotContain(action.Parameters, p => p.ParameterType == typeof(int) && !((ControllerParameterDescriptor)p).ParameterInfo.HasDefaultValue);
            if (action.EndpointMetadata.OfType<IAllowAnonymous>().Any()) continue;
            var policy = await AuthorizationPolicy.CombineAsync(provider.GetRequiredService<IAuthorizationPolicyProvider>(),
                action.EndpointMetadata.OfType<IAuthorizeData>());
            if (policy is not null)
                Assert.True((await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user, null, policy)).Succeeded,
                    $"{vaiTro ?? "Khách"} không có quyền vào {muc.Controller}/{muc.Action}");
        }
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("NhanVien")]
    [InlineData("KhachHang")]
    public void ClaimVaiTroChuaXacThucKhongLamLoMenuRieng(string vaiTro)
    {
        var user = NguoiDung(vaiTro, false);
        Assert.Empty(MenuQuanTri.Lay(user));
        Assert.DoesNotContain(MenuKhachHang.Lay(user), x => x.Controller is "DonDat" or "HoSo" or "KhachHang");
        Assert.Empty(MenuQuanTri.Lay(NguoiDung("KhachHang")));
    }

    [Theory]
    [InlineData("ChiTiet")]
    [InlineData("XacNhan")]
    [InlineData("TuChoi")]
    [InlineData("Huy")]
    public void TrangChiTietVaLoiXuLyDonGiuDungMotMucActive(string action)
    {
        var menu = MenuQuanTri.Lay(NguoiDung("NhanVien")).SelectMany(x => x.DanhSachMuc).ToArray();
        var active = Assert.Single(menu, x => x.KhopTrang("quanlydondat", action.ToLowerInvariant()));
        Assert.Equal("DanhSach", active.Action); // POST là trang con, không phải liên kết GET.
        Assert.DoesNotContain(menu, x => x.KhopTrang("DonDat", action));
        Assert.DoesNotContain(menu, x => x.KhopTrang("QuanLyDonDat", "KhongTonTai"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("khachhang")]
    [InlineData("nhanvien")]
    [InlineData("admin")]
    public async Task LayoutHienDungMenuVaDangXuatBaoVeBangAntiforgery(string? ten)
    {
        using var app = new UngDungKiemThu();
        await app.KhoiTaoAsync();
        using var client = app.TaoClient();
        var html = await client.GetStringAsync("/TaiKhoan/DangNhap");
        if (ten is not null)
        {
            var login = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["TenDangNhap"] = ten, ["MatKhau"] = app.MatKhau, ["__RequestVerificationToken"] = Token(html)
            }));
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            html = await client.GetStringAsync("/TaiKhoan/ThongTin");
        }
        html = WebUtility.HtmlDecode(html);
        var quanTri = ten is "admin" or "nhanvien";
        var nav = Regex.Match(html, quanTri ? "<nav aria-label=\"Menu quản trị\">([\\s\\S]*?)</nav>" : "<ul class=\"navbar-nav[^\"]*\">([\\s\\S]*?)</ul>").Value;
        Assert.NotEmpty(nav);
        Assert.Contains("Danh sách sân", nav);
        Assert.Equal(quanTri, nav.Contains("Đơn đặt sân"));
        Assert.Equal(ten == "admin", nav.Contains("Quản lý tài khoản"));
        Assert.Equal(ten == "admin", nav.Contains("Loại sân"));
        Assert.Equal(ten == "admin", nav.Contains("Dịch vụ"));
        Assert.Equal(ten == "khachhang", nav.Contains("Lịch của tôi"));
        Assert.Equal(ten == "khachhang", nav.Contains("Hồ sơ"));
        Assert.Equal(ten == "khachhang", nav.Contains("Đặt sân"));
        Assert.Single(Regex.Matches(html, "<header[ >]"));
        Assert.Single(Regex.Matches(html, "<main[ >]"));
        Assert.Single(Regex.Matches(html, "<footer[ >]"));
        if (ten is null)
        {
            Assert.DoesNotContain("id=\"dang-xuat\"", html);
            var account = Regex.Match(html, "<div class=\"ql-account\">([\\s\\S]*?)</div>").Value;
            Assert.True(account.IndexOf("/TaiKhoan/DangNhap", StringComparison.Ordinal) < account.IndexOf("/KhachHang/DangKy", StringComparison.Ordinal));
            Assert.Contains("/KhachHang/DangKy", account);
            Assert.Contains("aria-current=\"page\"", account);
            return;
        }
        var logout = Regex.Match(html, "<form id=\"dang-xuat\"([\\s\\S]*?)</form>").Value;
        Assert.Contains("method=\"post\"", logout);
        Assert.Contains("action=\"/TaiKhoan/DangXuat\"", logout);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/TaiKhoan/DangXuat", new FormUrlEncodedContent([]))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/TaiKhoan/ThongTin")).StatusCode);
        var response = await client.PostAsync("/TaiKhoan/DangXuat", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(logout)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var protectedPage = await client.GetAsync("/TaiKhoan/ThongTin");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("/TaiKhoan/DangNhap", protectedPage.Headers.Location!.OriginalString);
    }

    private static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}

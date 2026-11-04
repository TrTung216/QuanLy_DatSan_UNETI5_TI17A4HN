// M1: Trần Trọng Tùng; MSSV: 23103100202. Hồi quy caller M4: lỗi cọc trả thông báo, lỗi quyền trả Forbid.
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed partial class QuanLyDonDatTests
{
    [Fact]
    public async Task XacNhanCuChoDonChinhSachMoiHienThiLoiDoiSoat()
    {
        var don = await f.Booking();
        await using (var db = f.Db())
        {
            var saved = await db.DatSans.FindAsync(don.MaDatSan);
            saved!.PhienBanChinhSachCoc = 1; saved.TienCocYeuCau = 100000m;
            await db.SaveChangesAsync();
        }
        await using var app = new App(f); using var client = app.Client(); await Login(client, "admin");
        var response = await PostFromDetail(client, don.MaDatSan, "XacNhan");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cần đối soát giao dịch", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(don.TrangThai, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Theory]
    [InlineData("Huy")]
    [InlineData("TuChoi")]
    public async Task GiaoDichChoDoiSoatTraThongBaoThayViLoi500(string action)
    {
        var don = await f.Booking();
        await using (var db = f.Db())
            Assert.True((await f.Service(db).TaoGiaoDichMoPhongAsync(
                new(Guid.NewGuid().ToString("N"), $"COC {don.MaDatSan}", 1m, f.Clock.BayGio), default)).ThanhCong);
        await using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var response = await PostFromDetail(client, don.MaDatSan, action, "LyDo", "Test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cần đối soát trước khi hủy hoặc từ chối", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(don.TrangThai, (await f.Read(don.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task CallerTraForbidKhiServiceKhongConQuyen()
    {
        var don = await f.Booking();
        await using var db = f.Db();
        // Authorize ở HTTP không thay việc xử lý quyền do service trả về sau khi nạp lại DB.
        var controller = new QuanLyDonDatController(f.Service(db, f.CustomerAccountId), f.Clock);
        var result = await controller.XacNhan(new XacNhanDonViewModel
        {
            MaDon = don.MaDatSan, PhienBan = Convert.ToBase64String(don.RowVersion)
        }, default);
        Assert.IsType<ForbidResult>(result);
        Assert.Equal(don.TrangThai, (await f.Read(don.MaDatSan)).TrangThai);
    }
}

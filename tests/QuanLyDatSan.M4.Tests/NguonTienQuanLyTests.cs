// M1: Trần Trọng Tùng; MSSV: 23103100202. Hồi quy nguồn tiền M5 → DTO → Controller/View và form quản lý độc lập.
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed partial class QuanLyDonDatTests
{
    [Fact]
    public async Task ChiTietQuanLyTinhCaDichVuVaDungGiaDaChup()
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, deposit: 150000m);
        await using var db = f.Db();
        var don = await db.DatSans.Include(x => x.SanTheThao).SingleAsync(x => x.MaDatSan == order.MaDatSan);
        don.GioKetThuc = don.GioBatDau.AddMinutes(90);
        don.SanTheThao.DonGia = 900000m;
        var dv = new DichVu { TenDichVu = "Nước", DonViTinh = "Chai", DonGia = 30000m };
        db.Add(dv);
        await db.SaveChangesAsync();
        Assert.True((await DichVuService(db).ThemAsync(don.MaDatSan, dv.MaDichVu, 2, default)).ThanhCong);
        dv.DonGia = 99000m;
        await db.SaveChangesAsync();
        var before = await f.Read(order.MaDatSan);
        var detail = (await f.Service(db).LayChiTietQuanLyAsync(order.MaDatSan, default))!;
        Assert.Null(detail.LoiTien);
        Assert.Equal(300000m, detail.Don.TienSanDuKien);
        Assert.Equal(60000m, detail.Don.TienDichVuDuKien);
        Assert.Equal(360000m, detail.Don.TongTienDuKien);
        Assert.Equal(150000m, detail.TienCocToiThieu);
        Assert.Equal(210000m, detail.ConPhaiThu);
        Assert.Equal(before.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Fact]
    public async Task ControllerVaViewKhongTinhLaiKetQuaTuServiceTien()
    {
        var order = await f.Booking();
        using var original = new App(f);
        using var app = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITinhTienService>();
            services.AddSingleton<ITinhTienService, NguonTienKiemThu>();
        }));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        await Login(client, "staff");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}"));
        Assert.Contains("54.321", html); // Giá trị service cung cấp, không phải 50% tính lại tại Controller.
        await using (var db = f.Db())
        {
            var don = await db.DatSans.FindAsync(order.MaDatSan);
            don!.TrangThai = TrangThaiDatSan.DangXuLy;
            await db.SaveChangesAsync();
        }
        html = WebUtility.HtmlDecode(await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}"));
        Assert.Contains("12.345", html); // Không lấy tổng trừ cọc tại ViewModel.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CocVuotTongTraLoiVaKhongHienCongNoBangKhong(bool daChot)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1, 250000m);
        await using var db = f.Db();
        if (daChot)
        {
            var don = await db.DatSans.FindAsync(order.MaDatSan);
            don!.TongTien = 200000m; don.TienSan = 200000m; don.TienDichVu = 0m;
            await db.SaveChangesAsync();
        }
        var detail = (await f.Service(db).LayChiTietQuanLyAsync(order.MaDatSan, default))!;
        Assert.Equal(MaLoiCoc.DuLieuTienKhongHopLe, detail.LoiTien);
        Assert.Null(detail.ConPhaiThu);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}"));
        Assert.Contains("Chưa xác định", html);
        Assert.Contains("Dữ liệu tiền của đơn không hợp lệ", html);
        Assert.DoesNotContain("action=\"/QuanLyDonDat/GhiCoc\"", html);
        Assert.DoesNotContain("action=\"/QuanLyDonDat/HoanThanh\"", html);
    }

    [Fact]
    public async Task DonHoanThanhDocSnapshotKhongTinhLaiGiaVaKhongThuLanHai()
    {
        var order = await f.Booking(TrangThaiDatSan.HoanThanh, -1, 150000m);
        await using var db = f.Db();
        var don = await db.DatSans.Include(x => x.SanTheThao).SingleAsync(x => x.MaDatSan == order.MaDatSan);
        don.TienSan = 300000m; don.TienDichVu = 60000m; don.TongTien = 360000m;
        don.SanTheThao.DonGia = 990000m;
        await db.SaveChangesAsync();
        var detail = (await f.Service(db).LayChiTietQuanLyAsync(order.MaDatSan, default))!;
        Assert.Null(detail.LoiTien);
        Assert.Equal(300000m, detail.Don.TienSanDuKien); // Đơn gốc 1 giờ x 200.000, không được tính lại.
        Assert.Equal(60000m, detail.Don.TienDichVuDuKien); // Không có dòng dịch vụ, snapshot vẫn giữ nguyên.
        Assert.Equal(360000m, detail.Don.TongTienDuKien);
        Assert.Equal(0m, detail.ConPhaiThu);
        using var app = new App(f); using var client = app.Client(); await Login(client, "admin");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}"));
        Assert.Contains("360.000", html);
        Assert.DoesNotContain("action=\"/QuanLyDonDat/HoanThanh\"", html);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SnapshotThieuHoacMauThuanKhongTinhBuTuDuLieuHienTai(bool mauThuan)
    {
        var order = await f.Booking(TrangThaiDatSan.HoanThanh, -1);
        await using var db = f.Db();
        if (mauThuan)
        {
            var don = await db.DatSans.FindAsync(order.MaDatSan);
            don!.TienSan = 200000m; don.TienDichVu = 50000m; don.TongTien = 200000m;
            await db.SaveChangesAsync();
        }
        var detail = (await f.Service(db).LayChiTietQuanLyAsync(order.MaDatSan, default))!;
        Assert.Equal(MaLoiCoc.DuLieuTienKhongHopLe, detail.LoiTien);
        Assert.Null(detail.ConPhaiThu);
        Assert.Null(detail.TienCocToiThieu);
    }

    [Theory]
    [InlineData("GhiCoc")]
    [InlineData("HoanThanh")]
    public async Task FormThanhToanKhongKeThuaTruongLuaChonCocCuaXacNhan(string action)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        using var app = new App(f); using var client = app.Client(); await Login(client, "staff");
        var html = await client.GetStringAsync($"/QuanLyDonDat/ChiTiet/{order.MaDatSan}");
        var response = await PostPayment(client, order.MaDatSan, action, Token(html), Hidden(html, "PhienBan"),
            new() { ["TongCoc"] = "50000", ["DaThuDu"] = "true", ["TinhTrangCoc"] = "khong-phai-lua-chon" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var saved = await f.Read(order.MaDatSan);
        if (action == "GhiCoc") Assert.Equal(50000m, saved.TienCoc);
        else Assert.Equal(TrangThaiDatSan.HoanThanh, saved.TrangThai);
    }

    private sealed class NguonTienKiemThu : ITinhTienService
    {
        private readonly TinhTienService goc = new();
        public decimal TinhTienSan(decimal donGia, DateTime batDau, DateTime ketThuc) => goc.TinhTienSan(donGia, batDau, ketThuc);
        public decimal TinhTienDichVu(IEnumerable<DongDichVu> cacDong) => goc.TinhTienDichVu(cacDong);
        public decimal TinhTongTien(decimal tienSan, decimal tienDichVu) => goc.TinhTongTien(tienSan, tienDichVu);
        public decimal TinhTienCocYeuCau(decimal tienSan) => 54321m;
        public decimal TinhConPhaiThu(decimal tongTien, decimal tienCoc) => 12345m;
    }
}

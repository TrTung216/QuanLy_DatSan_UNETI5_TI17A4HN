// M1: Trần Trọng Tùng; MSSV: 23103100202. Nghiệm thu cọc mới, dịch vụ cá nhân và bảo toàn tiền.
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed partial class QuanLyDonDatTests
{
    [Fact]
    public async Task TaoDonCocDoiSoatQuaHttpKhongTinTienClientVaKhongGhiHaiLan()
    {
        var seed = await f.Booking(TrangThaiDatSan.DaHuy);
        await using var original = new App(f);
        using var app = original.WithWebHostBuilder(b => b.ConfigureServices(s => s.Configure<CocMoPhongOptions>(o => o.Bat = true)));
        using var customer = CocClient(app); await Login(customer, "customer");
        var html = await customer.GetStringAsync("/DonDat/Tao");
        var created = await customer.PostAsync("/DonDat/Tao", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html), ["MaSan"] = seed.MaSan.ToString(),
            ["NgaySuDung"] = "2030-01-02", ["GioBatDau"] = "10:00", ["GioKetThuc"] = "11:30",
            ["PhienBanChinhSachCoc"] = "0", ["TienCocYeuCau"] = "1", ["TienCoc"] = "999999", ["DonGia"] = "1"
        }));
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var id = int.Parse(created.Headers.Location!.OriginalString.Split('/').Last());
        var order = await f.Read(id);
        Assert.Equal(1, order.PhienBanChinhSachCoc); Assert.Equal(150000m, order.TienCocYeuCau);
        Assert.Equal(0m, order.TienCoc); Assert.Equal(TrangThaiDatSan.ChoXuLy, order.TrangThai);
        var qr = WebUtility.HtmlDecode(await customer.GetStringAsync($"/DonDat/Coc/{id}"));
        Assert.Contains($"DEMO:ORDER:{id}", qr); Assert.Contains("150.000", qr);
        // Nút Đã chuyển chỉ GET chi tiết, không tạo giao dịch hoặc xác nhận tiền.
        await customer.GetStringAsync($"/DonDat/ChiTiet/{id}");
        Assert.Equal(order.RowVersion, (await f.Read(id)).RowVersion);
        using var other = CocClient(app); await Login(other, "other");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/DonDat/Coc/{id}")).StatusCode);

        using var staff = CocClient(app); await Login(staff, "staff");
        html = await staff.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{id}");
        var simulated = await staff.PostAsync("/QuanLyDonDat/TaoGiaoDichMoPhong", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(html), ["MaDon"] = id.ToString(),
            ["MaGiaoDichNgoai"] = Guid.NewGuid().ToString("N"), ["NoiDung"] = $"COC {id}",
            ["SoTien"] = "150000", ["NgayNhan"] = "2030-01-02T08:00"
        }));
        Assert.Equal(HttpStatusCode.Redirect, simulated.StatusCode);
        var pending = await f.Read(id);
        Assert.Equal(0m, pending.TienCoc); Assert.Equal(TrangThaiDatSan.ChoXuLy, pending.TrangThai);
        Assert.Contains("chờ nhân viên đối soát", WebUtility.HtmlDecode(await customer.GetStringAsync($"/DonDat/Coc/{id}")));
        await using var db = f.Db();
        Assert.Equal(MaLoiDatSan.YeuCauDoiSoatCoc,
            (await f.Service(db).XacNhanDonAsync(new(id, pending.RowVersion), default)).Loi);
        Assert.Equal(MaLoiDatSan.YeuCauDoiSoatCoc,
            (await f.Service(db).XacNhanDonKemCocAsync(new(id, pending.RowVersion), default)).Loi);
        var gd = await db.GiaoDichCocs.SingleAsync(x => x.NoiDung == $"COC {id}");
        html = await staff.GetStringAsync($"/QuanLyDonDat/DoiSoatCoc/{id}");
        var form = FormDoiSoat(html, id, gd.MaGiaoDich);
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.Redirect, (await staff.PostAsync("/QuanLyDonDat/XacNhanCoc", new FormUrlEncodedContent(form))).StatusCode);
        var confirmed = await f.Read(id);
        Assert.Equal(150000m, confirmed.TienCoc); Assert.Equal(TrangThaiDatSan.DangXuLy, confirmed.TrangThai);
        Assert.Equal(1, await db.BienNhanCocs.CountAsync(x => x.MaDatSan == id));
        Assert.Contains("Biên nhận", WebUtility.HtmlDecode(await customer.GetStringAsync($"/DonDat/Coc/{id}")));

        // Hoàn tất cùng đơn: thêm dịch vụ, thay giá danh mục, chốt tiền và khách đọc số đã chốt.
        // Mỗi thao tác web có scope riêng; không dùng context đã đọc đơn trước đối soát HTTP.
        await using var settlement = f.Db();
        var dv = new DichVu { TenDichVu = Guid.NewGuid().ToString("N"), DonViTinh = "Chai", DonGia = 30000m };
        settlement.Add(dv); await settlement.SaveChangesAsync();
        Assert.True((await DichVuService(settlement).ThemAsync(id, dv.MaDichVu, 2,
            Convert.ToBase64String(confirmed.RowVersion), default)).ThanhCong);
        dv.DonGia = 90000m;
        (await settlement.SanTheThaos.FindAsync(seed.MaSan))!.DonGia = 900000m;
        await settlement.SaveChangesAsync();
        var beforeComplete = await f.Read(id);
        Assert.True((await f.Service(settlement, clock: new DongHoSauDat(beforeComplete.GioKetThuc))
            .HoanThanhDonAsync(new(id, true, beforeComplete.RowVersion), default)).ThanhCong);
        var completed = await f.Read(id);
        Assert.Equal(300000m, completed.TienSan); Assert.Equal(60000m, completed.TienDichVu);
        Assert.Equal(360000m, completed.TongTien); Assert.Equal(150000m, completed.TienCoc);
        var customerDetail = WebUtility.HtmlDecode(await customer.GetStringAsync($"/DonDat/ChiTiet/{id}"));
        Assert.Contains("360.000", customerDetail); Assert.Contains("60.000", customerDetail);
        Assert.Contains("Có hiệu lực", customerDetail);
    }

    [Theory]
    [InlineData(false, "Development", 101, 51)]
    [InlineData(true, "Production", 200000, 100000)]
    [InlineData(true, "Development", 0, 0)]
    public async Task ChinhSachMoiKhongPhuThuocCoMoPhongVaDonMienPhiKhongCanBienNhan(bool enabled, string environment, int price, int deposit)
    {
        var seed = await f.Booking(TrangThaiDatSan.DaHuy);
        await using (var db = f.Db())
        {
            (await db.SanTheThaos.FindAsync(seed.MaSan))!.DonGia = price;
            await db.SaveChangesAsync();
        }
        await using var act = f.Db();
        var service = f.Service(act, f.CustomerAccountId, enabled: enabled, environment: environment);
        var result = await service.TaoDonAsync(new(f.CustomerAccountId, seed.MaSan, f.Clock.HomNay, new(10, 0), new(11, 0)), default);
        Assert.True(result.ThanhCong);
        var order = await f.Read(result.MaDatSan!.Value);
        Assert.Equal(1, order.PhienBanChinhSachCoc); Assert.Equal((decimal)deposit, order.TienCocYeuCau);
        var info = (await service.LayThongTinCocAsync(order.MaDatSan, default)).DuLieu!;
        Assert.Equal(deposit == 0 ? TrangThaiCocHienThi.KhongApDung : TrangThaiCocHienThi.KhongSanSang, info.TrangThaiCoc);
        Assert.Null(info.NoiDungQrMoPhong);
        await using var managerDb = f.Db();
        var confirm = await f.Service(managerDb).XacNhanDonAsync(new(order.MaDatSan, order.RowVersion), default);
        Assert.Equal(deposit == 0, confirm.ThanhCong);
        Assert.False(await managerDb.BienNhanCocs.AnyAsync(x => x.MaDatSan == order.MaDatSan));
        Assert.Equal(0, (await f.Read(seed.MaDatSan)).PhienBanChinhSachCoc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GiamHoacHuyDichVuKhongDuocLamTongThapHonCoc(bool huy)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, deposit: 250000m);
        int lineId;
        await using (var seed = f.Db())
        {
            var line = new ChiTietDatSan { MaDatSan = order.MaDatSan, SoLuong = 2, DonGia = 30000m,
                DichVu = new DichVu { TenDichVu = Guid.NewGuid().ToString("N"), DonViTinh = "Chai", DonGia = 30000m } };
            seed.Add(line); (await seed.DatSans.FindAsync(order.MaDatSan))!.TienDichVu = 60000m;
            await seed.SaveChangesAsync(); lineId = line.MaChiTiet;
        }
        var before = await f.Read(order.MaDatSan);
        await using var act = f.Db(); var service = DichVuService(act);
        var result = huy ? await service.HuyAsync(order.MaDatSan, lineId, Convert.ToBase64String(before.RowVersion), default)
            : await service.SuaSoLuongAsync(order.MaDatSan, lineId, 1, Convert.ToBase64String(before.RowVersion), default);
        Assert.False(result.ThanhCong);
        Assert.Equal(before.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
        Assert.Equal(2, (await act.ChiTietDatSans.AsNoTracking().SingleAsync(x => x.MaChiTiet == lineId)).SoLuong);
        var detail = await service.LayTheoDonAsync(order.MaDatSan, default);
        Assert.DoesNotContain(detail!.Dong, x => x.DaHuy);
    }

    [Fact]
    public async Task KhachXemGiaDaLuuVaLichSuHuyNhungKhongXemDonNguoiKhac()
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy);
        int serviceId;
        var name = "Dịch vụ cá nhân " + Guid.NewGuid().ToString("N");
        await using (var seed = f.Db())
        {
            var dv = new DichVu { TenDichVu = name, DonViTinh = "Chai", DonGia = 30000m };
            seed.Add(dv); await seed.SaveChangesAsync(); serviceId = dv.MaDichVu;
        }
        await using (var act = f.Db())
        {
            var m5 = DichVuService(act);
            Assert.True((await m5.ThemAsync(order.MaDatSan, serviceId, 2, Convert.ToBase64String(order.RowVersion), default)).ThanhCong);
        }
        var updated = await f.Read(order.MaDatSan);
        await using (var act = f.Db())
        {
            var line = await act.ChiTietDatSans.SingleAsync(x => x.MaDatSan == order.MaDatSan);
            Assert.True((await DichVuService(act).HuyAsync(order.MaDatSan, line.MaChiTiet, Convert.ToBase64String(updated.RowVersion), default)).ThanhCong);
        }
        await using (var seed = f.Db())
        {
            (await seed.DichVus.FindAsync(serviceId))!.DonGia = 40000m;
            await seed.SaveChangesAsync();
        }
        updated = await f.Read(order.MaDatSan);
        await using (var act = f.Db())
            Assert.True((await DichVuService(act).ThemAsync(order.MaDatSan, serviceId, 1, Convert.ToBase64String(updated.RowVersion), default)).ThanhCong);
        await using (var read = f.Db())
        {
            var detail = await f.Service(read, f.CustomerAccountId).LayChiTietCaNhanAsync(f.CustomerAccountId, order.MaDatSan, default);
            Assert.Equal(2, detail!.DichVu.Count);
            Assert.Equal(60000m, Assert.Single(detail.DichVu, x => x.DaHuy).ThanhTien);
            Assert.Equal(40000m, Assert.Single(detail.DichVu, x => !x.DaHuy).ThanhTien);
        }
        await using var app = new App(f); using var customer = app.Client(); await Login(customer, "customer");
        var html = WebUtility.HtmlDecode(await customer.GetStringAsync($"/DonDat/ChiTiet/{order.MaDatSan}"));
        Assert.Contains(name, html); Assert.Contains("Đã hủy", html); Assert.Contains("Có hiệu lực", html);
        Assert.Contains("60.000", html); Assert.Contains("40.000", html);
        Assert.DoesNotContain("MaTaiKhoanHuy", html);
        using var other = app.Client(); await Login(other, "other");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/DonDat/ChiTiet/{order.MaDatSan}")).StatusCode);
    }

    private sealed class DongHoSauDat(DateTime bayGio) : IDongHo
    {
        public DateTime BayGio => bayGio;
        public DateOnly HomNay => DateOnly.FromDateTime(BayGio);
    }

    [Fact]
    public async Task TaoDonVuotGioiHanTienKhongLuuDonMotPhan()
    {
        var seed = await f.Booking(TrangThaiDatSan.DaHuy);
        await using var db = f.Db();
        (await db.SanTheThaos.FindAsync(seed.MaSan))!.DonGia = 9999999999999999m;
        await db.SaveChangesAsync();
        var before = await db.DatSans.CountAsync(x => x.MaSan == seed.MaSan);
        var result = await f.Service(db, f.CustomerAccountId).TaoDonAsync(
            new(f.CustomerAccountId, seed.MaSan, f.Clock.HomNay, new(10, 0), new(12, 0)), default);
        Assert.False(result.ThanhCong);
        Assert.Equal(MaLoiDatSan.TienVuotGioiHan, result.Loi!.Ma);
        Assert.Equal(before, await db.DatSans.CountAsync(x => x.MaSan == seed.MaSan));
    }
}

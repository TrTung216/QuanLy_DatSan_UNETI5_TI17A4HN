// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm thử tích hợp cọc M4 cạnh tranh với hủy, dùng database SQL Server riêng.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class CocCanhTranhTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    private async Task<DatSan> DonMoi()
    {
        var don = await f.Booking();
        await using var db = f.Db();
        var saved = await db.DatSans.FindAsync(don.MaDatSan);
        saved!.PhienBanChinhSachCoc = 1; saved.TienCocYeuCau = 100000m;
        await db.SaveChangesAsync();
        return await f.Read(don.MaDatSan);
    }

    private async Task<KetQuaCoc<GiaoDichCocDto>> TaoGiaoDich(DatSan don)
    {
        await using var db = f.Db();
        return await f.Service(db).TaoGiaoDichMoPhongAsync(new(Guid.NewGuid().ToString("N"),
            $"COC {don.MaDatSan}", don.TienCocYeuCau, f.Clock.BayGio), default);
    }

    private async Task<KetQuaXuLyDon> Huy(DatSan don)
    {
        await using var db = f.Db();
        return await f.Service(db, f.CustomerAccountId).KhachHangHuyDonAsync(
            new(don.MaDatSan, "Đổi lịch", don.RowVersion), f.CustomerAccountId, default);
    }

    [Fact]
    public async Task TaoGiaoDichCungLucHuyKhongLamMatTienDenMuon()
    {
        var don = await DonMoi();
        var tao = TaoGiaoDich(don); var huy = Huy(don);
        await Task.WhenAll(tao, huy);
        var ketQuaTao = await tao;
        var ketQuaHuy = await huy;
        Assert.True(ketQuaTao.ThanhCong);
        var saved = await f.Read(don.MaDatSan);
        if (ketQuaHuy.ThanhCong)
        {
            Assert.Equal(TrangThaiDatSan.DaHuy, saved.TrangThai);
            Assert.Equal(TrangThaiGiaoDichCoc.CanXuLyNgoaiLe, ketQuaTao.DuLieu!.TrangThai);
        }
        else
        {
            Assert.Equal(MaLoiDatSan.GiaoDichChoDoiSoat, ketQuaHuy.Loi);
            Assert.Equal(TrangThaiDatSan.ChoXuLy, saved.TrangThai);
            Assert.Equal(TrangThaiGiaoDichCoc.ChuaDoiSoat, ketQuaTao.DuLieu!.TrangThai);
        }
        Assert.Equal(0m, saved.TienCoc);
        await using var read = f.Db();
        Assert.False(await read.BienNhanCocs.AnyAsync(x => x.MaDatSan == don.MaDatSan));
    }

    [Fact]
    public async Task XacNhanCanhTranhHuyGiuBienNhanVaTrangThaiDongNhat()
    {
        var don = await DonMoi();
        var gd = await TaoGiaoDich(don);
        Assert.True(gd.ThanhCong);
        await using var db = f.Db();
        var xacNhan = f.Service(db).XacNhanCocVaDatSanAsync(
            new(don.MaDatSan, gd.DuLieu!.MaGiaoDich, don.RowVersion, true), default);
        var huy = Huy(don);
        await Task.WhenAll(xacNhan, huy);
        var ketQuaXacNhan = await xacNhan;
        var ketQuaHuy = await huy;
        Assert.True(ketQuaXacNhan.ThanhCong);
        Assert.False(ketQuaHuy.ThanhCong);
        Assert.Contains(ketQuaHuy.Loi, new MaLoiDatSan?[] { MaLoiDatSan.GiaoDichChoDoiSoat, MaLoiDatSan.PhienBanDaCu });
        var saved = await f.Read(don.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai);
        Assert.Equal(don.TienCocYeuCau, saved.TienCoc);
        await using var read = f.Db();
        Assert.Single(await read.BienNhanCocs.Where(x => x.MaDatSan == don.MaDatSan).ToListAsync());
        Assert.Equal(TrangThaiGiaoDichCoc.DaGanBienNhan,
            (await read.GiaoDichCocs.SingleAsync(x => x.MaGiaoDich == gd.DuLieu.MaGiaoDich)).TrangThai);
    }
}

// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm thử ràng buộc SQL Server và lỗi lưu trữ cho hợp đồng cọc M4.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class CocHaTangTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    private GiaoDichCoc GiaoDich() => new()
    {
        MaGiaoDichNgoai = Guid.NewGuid().ToString("N"), NoiDung = "COC 1", TaiKhoanNhan = "DEMO-UNETI",
        SoTien = 100000m, NgayNhan = f.Clock.BayGio, NgayGhi = f.Clock.BayGio, MaNguoiGhi = f.AdminAccountId
    };

    private BienNhanCoc BienNhan(int maDon, GiaoDichCoc gd) => new()
    {
        MaDatSan = maDon, MaGiaoDich = gd.MaGiaoDich, SoTien = gd.SoTien,
        Nguon = gd.Nguon, TaiKhoanNhan = gd.TaiKhoanNhan, MaGiaoDichNgoai = gd.MaGiaoDichNgoai,
        NoiDung = gd.NoiDung, NgayNhan = gd.NgayNhan, NgayXacNhan = f.Clock.BayGio,
        MaNguoiXacNhan = f.AdminAccountId
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UniqueBienNhanTheoDonVaGiaoDichDuocAnhXaChinhXac(bool trungDon)
    {
        var don1 = await f.Booking(); var don2 = await f.Booking();
        await using var db = f.Db();
        var gd1 = GiaoDich(); var gd2 = GiaoDich();
        db.AddRange(gd1, gd2); await db.SaveChangesAsync();
        db.Add(BienNhan(don1.MaDatSan, gd1)); await db.SaveChangesAsync();
        var bnTrung = BienNhan(trungDon ? don1.MaDatSan : don2.MaDatSan, trungDon ? gd2 : gd1);
        db.Add(bnTrung);
        var uow = new DonViCongViec(db);
        var ex = await Assert.ThrowsAsync<LoiLuuTruException>(() => uow.LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.TrungBienNhanCoc, ex.MaLoi);
        Assert.Equal(EntityState.Detached, db.Entry(bnTrung).State);
        Assert.Null(db.Database.CurrentTransaction);
        await uow.LuuAsync(default);
        Assert.Single(await db.BienNhanCocs.Where(x => x.MaDatSan == don1.MaDatSan || x.MaDatSan == don2.MaDatSan).ToListAsync());
    }

    [Theory]
    [InlineData("don")]
    [InlineData("giaoDich")]
    [InlineData("nguoiXacNhan")]
    public async Task KhongCascadeXoaLichSuBienNhan(string doiTuong)
    {
        var don = await f.Booking();
        var gd = GiaoDich();
        await using (var seed = f.Db())
        {
            seed.Add(gd); await seed.SaveChangesAsync();
            seed.Add(BienNhan(don.MaDatSan, gd)); await seed.SaveChangesAsync();
        }
        await using var db = f.Db();
        if (doiTuong == "don") db.Remove((await db.DatSans.FindAsync(don.MaDatSan))!);
        else if (doiTuong == "giaoDich") db.Remove((await db.GiaoDichCocs.FindAsync(gd.MaGiaoDich))!);
        else db.Remove((await db.TaiKhoans.FindAsync(f.AdminAccountId))!);
        // Không ánh xạ nhầm FK cọc thành lỗi loại sân/trùng giao dịch.
        await Assert.ThrowsAsync<DbUpdateException>(() => new DonViCongViec(db).LuuAsync(default));
        Assert.True(await db.BienNhanCocs.AnyAsync(x => x.MaDatSan == don.MaDatSan));
        Assert.True(await db.GiaoDichCocs.AnyAsync(x => x.MaGiaoDich == gd.MaGiaoDich));
        Assert.True(await db.DatSans.AnyAsync(x => x.MaDatSan == don.MaDatSan));
        Assert.True(await db.TaiKhoans.AnyAsync(x => x.MaTaiKhoan == f.AdminAccountId));
    }

    [Fact]
    public async Task RowVersionGiaoDichChanGhiDeNgoaiLeCu()
    {
        var gd = GiaoDich();
        await using var db1 = f.Db();
        db1.Add(gd); await db1.SaveChangesAsync();
        Assert.Equal(8, gd.RowVersion.Length);
        await using var db2 = f.Db();
        var cu = await db2.GiaoDichCocs.SingleAsync(x => x.MaGiaoDich == gd.MaGiaoDich);
        gd.TrangThai = TrangThaiGiaoDichCoc.CanXuLyNgoaiLe;
        gd.LoiNgoaiLe = MaLoiCoc.SoTienKhongKhop;
        await new DonViCongViec(db1).LuuAsync(default);
        cu.TrangThai = TrangThaiGiaoDichCoc.CanXuLyNgoaiLe;
        cu.LoiNgoaiLe = MaLoiCoc.ThoiDiemNhanKhongHopLe;
        var ex = await Assert.ThrowsAsync<LoiLuuTruException>(() => new DonViCongViec(db2).LuuAsync(default));
        Assert.Equal(MaLoiLuuTru.XungDotPhienBan, ex.MaLoi);
        Assert.Equal(MaLoiCoc.SoTienKhongKhop,
            (await db2.GiaoDichCocs.AsNoTracking().SingleAsync(x => x.MaGiaoDich == gd.MaGiaoDich)).LoiNgoaiLe);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, -1)]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    public async Task TienKhongDuongBiChanTaiDatabaseKhongBiBaoTrung(bool bienNhan, int soTien)
    {
        var don = await f.Booking();
        await using var db = f.Db();
        var gd = GiaoDich();
        if (bienNhan)
        {
            db.Add(gd); await db.SaveChangesAsync();
            var bn = BienNhan(don.MaDatSan, gd); bn.SoTien = soTien; db.Add(bn);
        }
        else { gd.SoTien = soTien; db.Add(gd); }
        await Assert.ThrowsAsync<DbUpdateException>(() => new DonViCongViec(db).LuuAsync(default));
        Assert.False(db.ChangeTracker.HasChanges());
    }
}

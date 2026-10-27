// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-04 - biên tiền, rollback và dịch vụ M5 thay đổi ngay trước khi ghi tiền.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed partial class QuanLyDonDatTests
{
    [Theory]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("0.5")]
    [InlineData("10000000000000000")]
    public async Task ServiceGhiCocTuKiemTraTienKhongPhuThuocMvc(string value)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy);
        await using var db = f.Db();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(MaLoiDatSan.TienCocKhongHopLe,
            (await f.Service(db).GhiCocAsync(new(order.MaDatSan, amount, order.RowVersion), default)).Loi);
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Fact]
    public async Task CocBangTongTienVanCanXacNhanThuDuVaChanHuy()
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        await using var db = f.Db();
        Assert.True((await f.Service(db).GhiCocAsync(new(order.MaDatSan, 200000m, order.RowVersion), default)).ThanhCong);
        var paid = await f.Read(order.MaDatSan);
        Assert.Equal(MaLoiDatSan.ChuaThuDu,
            (await f.Service(db).HoanThanhDonAsync(new(order.MaDatSan, false, paid.RowVersion), default)).Loi);
        Assert.True((await f.Service(db).HoanThanhDonAsync(new(order.MaDatSan, true, paid.RowVersion), default)).ThanhCong);
        Assert.Equal(200000m, (await f.Read(order.MaDatSan)).TienCoc);

        var future = await f.Booking(TrangThaiDatSan.DangXuLy);
        await using var act = f.Db();
        Assert.True((await f.Service(act).GhiCocAsync(new(future.MaDatSan, 50000m, future.RowVersion), default)).ThanhCong);
        var deposited = await f.Read(future.MaDatSan);
        Assert.Equal(MaLoiDatSan.DaNhanCoc,
            (await f.Service(act).NhanVienHuyDonAsync(new(future.MaDatSan, "Hủy", deposited.RowVersion), default)).Loi);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TongTienVuotDecimalSqlTraLoiKhongGhi(bool complete)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -2);
        await using (var seed = f.Db())
        {
            var don = await seed.DatSans.SingleAsync(x => x.MaDatSan == order.MaDatSan);
            don.DonGia = 9999999999999999m;
            don.GioKetThuc = don.GioBatDau.AddHours(2);
            await seed.SaveChangesAsync();
        }
        order = await f.Read(order.MaDatSan);
        await using var db = f.Db();
        var result = complete
            ? await f.Service(db).HoanThanhDonAsync(new(order.MaDatSan, true, order.RowVersion), default)
            : await f.Service(db).GhiCocAsync(new(order.MaDatSan, 1m, order.RowVersion), default);
        Assert.Equal(MaLoiDatSan.TienVuotGioiHan, result.Loi);
        Assert.Equal(order.RowVersion, (await f.Read(order.MaDatSan)).RowVersion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DichVuThayDoiNgayTruocKhiGhiTienKhongBiChotDe(bool complete)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        int serviceId;
        await using (var seed = f.Db())
        {
            var dv = new DichVu { TenDichVu = "Nước", DonViTinh = "Chai", DonGia = 10000 };
            seed.Add(dv); await seed.SaveChangesAsync();
            serviceId = dv.MaDichVu;
        }
        // SQL M5 chạy thật ở context khác sau khi M4 đã tính tiền nhưng trước UPDATE.
        var race = new BeforePaymentWrite(async () =>
        {
            await using var other = f.Db();
            Assert.True((await DichVuService(other).ThemAsync(order.MaDatSan, serviceId, 2, default)).ThanhCong);
        });
        await using var db = f.Db(race);
        var result = complete
            ? await f.Service(db).HoanThanhDonAsync(new(order.MaDatSan, true, order.RowVersion), default)
            : await f.Service(db).GhiCocAsync(new(order.MaDatSan, 50000m, order.RowVersion), default);
        Assert.Equal(MaLoiDatSan.XungDotDuLieu, result.Loi);
        await db.SaveChangesAsync();
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai);
        Assert.Null(saved.TongTien); Assert.Null(saved.NgayHoanThanh);
        Assert.Equal(20000m, saved.TienDichVu); Assert.Equal(0m, saved.TienCoc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoiSauGhiTienRollbackCaCocVaTrangThai(bool complete)
    {
        var order = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        await using var db = f.Db(new FailPaymentAfterWrite());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (complete)
                await f.Service(db).HoanThanhDonAsync(new(order.MaDatSan, true, order.RowVersion), default);
            else
                await f.Service(db).GhiCocAsync(new(order.MaDatSan, 50000m, order.RowVersion), default);
        });
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(order.RowVersion, saved.RowVersion);
        Assert.Null(saved.TongTien); Assert.Null(saved.NgayHoanThanh); Assert.Equal(0m, saved.TienCoc);
    }

    private sealed class BeforePaymentWrite(Func<Task> action) : SaveChangesInterceptor
    {
        private bool called;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!called) { called = true; await action(); }
            return result;
        }
    }

    private sealed class FailPaymentAfterWrite : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Test rollback tiền");
    }
}

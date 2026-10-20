// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: kiểm thử hợp đồng khả dụng M2/M4 trên SQL Server, biên giờ và trạng thái chiếm chỗ.
// Đối chiếu tạo/xác nhận, dữ liệu sân đã thay đổi, truy vấn theo lô và kiểm tra lại sau tra cứu.
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class KhaDungSanTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    private KhoangGioTraCuuSan Gio(int start = 10, int end = 11) => new(f.Clock.HomNay, new(start, 0), new(end, 0));

    [Theory]
    [InlineData(TrangThaiDatSan.ChoXuLy, 0, true)]
    [InlineData(TrangThaiDatSan.DangXuLy, 0, true)]
    [InlineData(TrangThaiDatSan.HoanThanh, 0, true)]
    [InlineData(TrangThaiDatSan.DaHuy, 0, false)]
    [InlineData(TrangThaiDatSan.TuChoi, 0, false)]
    [InlineData(TrangThaiDatSan.ChoXuLy, -60, false)]
    [InlineData(TrangThaiDatSan.ChoXuLy, 60, false)]
    [InlineData(TrangThaiDatSan.ChoXuLy, -30, true)]
    [InlineData(TrangThaiDatSan.DangXuLy, 30, true)]
    public async Task TraCuuVaTaoDonCungQuyTacGiaoLich(TrangThaiDatSan state, int offset, bool conflict)
    {
        var order = await f.Booking(state);
        var start = order.GioBatDau.AddMinutes(offset);
        var end = order.GioKetThuc.AddMinutes(offset);
        var gio = new KhoangGioTraCuuSan(f.Clock.HomNay, TimeOnly.FromDateTime(start), TimeOnly.FromDateTime(end));
        await using var db = f.Db();
        var service = f.Service(db);
        var read = await service.KiemTraKhaDungAsync([order.MaSan], gio, default);
        Assert.Null(read.Loi);
        var san = Assert.Single(read.DanhSach);
        Assert.Equal(!conflict, san.KhaDung);
        Assert.Equal(conflict ? MaLoiKhaDungSan.TrungLich : (MaLoiKhaDungSan?)null, san.Loi);
        var created = await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
            gio.NgaySuDung, gio.GioBatDau, gio.GioKetThuc), default);
        Assert.Equal(!conflict, created.ThanhCong);
        if (conflict) Assert.Equal(MaLoiDatSan.TrungLichKhiTao, created.Loi?.Ma);
    }

    [Theory]
    [InlineData("inactive", MaLoiKhaDungSan.SanKhongNhanDat, MaLoiDatSan.SanKhongNhanDat, MaLoiDatSan.SanKhongHoatDong)]
    [InlineData("maintenance-status", MaLoiKhaDungSan.SanKhongNhanDat, MaLoiDatSan.SanKhongNhanDat, MaLoiDatSan.SanKhongHoatDong)]
    [InlineData("inactive-type", MaLoiKhaDungSan.SanKhongNhanDat, MaLoiDatSan.SanKhongNhanDat, MaLoiDatSan.SanKhongHoatDong)]
    [InlineData("maintenance-date", MaLoiKhaDungSan.SanBaoTri, MaLoiDatSan.SanBaoTriKhiTao, MaLoiDatSan.SanBaoTri)]
    [InlineData("opening", MaLoiKhaDungSan.NgoaiGioHoatDong, MaLoiDatSan.NgoaiGioKhiTao, MaLoiDatSan.NgoaiGioHoatDong)]
    [InlineData("closing", MaLoiKhaDungSan.NgoaiGioHoatDong, MaLoiDatSan.NgoaiGioKhiTao, MaLoiDatSan.NgoaiGioHoatDong)]
    public async Task TraCuuTaoXacNhanCungDieuKienSan(string scenario,
        MaLoiKhaDungSan reason, MaLoiDatSan createError, MaLoiDatSan confirmError)
    {
        var order = await f.Booking();
        await using (var setup = f.Db())
        {
            var court = await setup.SanTheThaos.SingleAsync(x => x.MaSan == order.MaSan);
            if (scenario == "inactive") court.TrangThai = TrangThaiSan.NgungHoatDong;
            if (scenario == "maintenance-status") court.TrangThai = TrangThaiSan.BaoTri;
            if (scenario == "inactive-type") court.LoaiSan = new LoaiSan
            {
                TenLoai = "KhaDung " + Guid.NewGuid().ToString("N"),
                DonGiaTheoGio = 200000m, SoNguoiToiDa = 10, TrangThai = TrangThaiLoaiSan.NgungHoatDong
            };
            if (scenario == "maintenance-date") court.NgayBaoTri = f.Clock.HomNay;
            if (scenario == "opening") court.GioMoCua = new(10, 30);
            if (scenario == "closing") court.GioDongCua = new(10, 30);
            await setup.SaveChangesAsync();
        }
        await using var db = f.Db();
        var service = f.Service(db);
        var read = await service.KiemTraKhaDungAsync([order.MaSan], Gio(), default);
        Assert.Equal(reason, Assert.Single(read.DanhSach).Loi);
        var create = await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
            f.Clock.HomNay, new(10, 0), new(11, 0)), default);
        Assert.Equal(createError, create.Loi?.Ma);
        if (createError == MaLoiDatSan.NgoaiGioKhiTao)
        {
            Assert.NotNull(create.Loi!.GioMoCua);
            Assert.NotNull(create.Loi.GioDongCua);
        }
        var confirm = await service.XacNhanDonAsync(new(order.MaDatSan, order.RowVersion), default);
        Assert.Equal(confirmError, confirm.Loi);
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(order.MaDatSan)).TrangThai);
    }

    [Theory]
    [InlineData(8, 9, MaLoiKhaDungSan.BatDauKhongTuongLai, MaLoiDatSan.BatDauKhongTuongLai)]
    [InlineData(7, 8, MaLoiKhaDungSan.BatDauKhongTuongLai, MaLoiDatSan.BatDauKhongTuongLai)]
    [InlineData(10, 10, MaLoiKhaDungSan.KhoangGioKhongHopLe, MaLoiDatSan.KetThucKhongHopLe)]
    [InlineData(23, 1, MaLoiKhaDungSan.KhoangGioKhongHopLe, MaLoiDatSan.KetThucKhongHopLe)]
    public async Task LoiKhoangGioKhongTruyVanSan(int start, int end, MaLoiKhaDungSan reason, MaLoiDatSan createError)
    {
        var order = await f.Booking(TrangThaiDatSan.DaHuy);
        var count = new DemTruyVan();
        await using var db = f.Db(count);
        var service = f.Service(db);
        var result = await service.KiemTraKhaDungAsync([order.MaSan], Gio(start, end), default);
        Assert.Equal(reason, result.Loi);
        Assert.Empty(result.DanhSach);
        Assert.Equal(0, count.SoLan);
        var created = await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
            f.Clock.HomNay, new(start, 0), new(end, 0)), default);
        Assert.Equal(createError, created.Loi?.Ma);
    }

    [Fact]
    public async Task BienMoDongCuaVaChuanHoaPhutGiongTaoDon()
    {
        var order = await f.Booking(TrangThaiDatSan.DaHuy);
        await using (var setup = f.Db())
        {
            var court = await setup.SanTheThaos.FindAsync(order.MaSan);
            court!.GioMoCua = new(10, 0);
            court.GioDongCua = new(11, 0);
            court.NgayBaoTri = f.Clock.HomNay.AddDays(1);
            await setup.SaveChangesAsync();
        }
        await using var db = f.Db();
        var service = f.Service(db);
        var gio = new KhoangGioTraCuuSan(f.Clock.HomNay, new(10, 0, 59), new(11, 0, 59));
        var result = await service.KiemTraKhaDungAsync([order.MaSan], gio, default);
        Assert.True(Assert.Single(result.DanhSach).KhaDung);
        var created = await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
            gio.NgaySuDung, gio.GioBatDau, gio.GioKetThuc), default);
        Assert.True(created.ThanhCong);
        var saved = await f.Read(created.MaDatSan!.Value);
        Assert.Equal(result.BatDau, saved.GioBatDau);
        Assert.Equal(result.KetThuc, saved.GioKetThuc);
        Assert.Equal(0, saved.GioBatDau.Second);
    }

    [Fact]
    public async Task KhoangGioSauChuanHoaRongThiBiTuChoi()
    {
        await using var db = f.Db();
        var result = await f.Service(db).KiemTraKhaDungAsync([1],
            new(f.Clock.HomNay, new(10, 0, 1), new(10, 0, 59)), default);
        Assert.Equal(MaLoiKhaDungSan.KhoangGioKhongHopLe, result.Loi);
    }

    [Fact]
    public async Task TraCuuTheoLoMotLenhSqlKhongTrackVaBaoMaKhongTonTai()
    {
        var free = await f.Booking(TrangThaiDatSan.DaHuy);
        var busy = await f.Booking();
        var ids = new[] { busy.MaSan, free.MaSan, free.MaSan }
            .Concat(Enumerable.Range(int.MaxValue - 196, 197)).ToArray();
        var count = new DemTruyVan();
        await using var db = f.Db(count);
        var result = await f.Service(db).KiemTraKhaDungAsync(ids, Gio(), default);
        Assert.Null(result.Loi);
        Assert.Equal(ids.Distinct(), result.DanhSach.Select(x => x.MaSan));
        Assert.Equal(MaLoiKhaDungSan.TrungLich, result.DanhSach[0].Loi);
        Assert.True(result.DanhSach[1].KhaDung);
        Assert.All(result.DanhSach.Skip(2), x => Assert.Equal(MaLoiKhaDungSan.SanKhongTonTai, x.Loi));
        Assert.Equal(1, count.SoLan);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task DanhSachRongDuLieuSaiVaHuyTacVuKhongTruyVan()
    {
        var count = new DemTruyVan();
        await using var db = f.Db(count);
        var service = f.Service(db);
        Assert.Empty((await service.KiemTraKhaDungAsync([], Gio(), default)).DanhSach);
        await Assert.ThrowsAsync<ArgumentException>(() => service.KiemTraKhaDungAsync([0], Gio(), default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.KiemTraKhaDungAsync([-1], Gio(), default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.KiemTraKhaDungAsync(
            Enumerable.Range(1, 201).ToArray(), Gio(), default));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.KiemTraKhaDungAsync(null!, Gio(), default));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.KiemTraKhaDungAsync([1], null!, default));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.KiemTraKhaDungAsync([1], Gio(), new CancellationToken(true)));
        Assert.Equal(0, count.SoLan);
    }

    [Fact]
    public async Task TraCuuKhongGiuChoTaoDonVanKiemTraLai()
    {
        var order = await f.Booking(TrangThaiDatSan.DaHuy);
        await using var db = f.Db();
        var service = f.Service(db);
        Assert.True(Assert.Single((await service.KiemTraKhaDungAsync([order.MaSan], Gio(), default)).DanhSach).KhaDung);
        await using (var otherDb = f.Db())
        {
            var other = await f.Service(otherDb).TaoDonAsync(new(f.OtherCustomerAccountId, order.MaSan,
                f.Clock.HomNay, new(10, 0), new(11, 0)), default);
            Assert.True(other.ThanhCong);
        }
        var mine = await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
            f.Clock.HomNay, new(10, 0), new(11, 0)), default);
        Assert.Equal(MaLoiDatSan.TrungLichKhiTao, mine.Loi?.Ma);
        Assert.Equal(MaLoiKhaDungSan.TrungLich,
            Assert.Single((await service.KiemTraKhaDungAsync([order.MaSan], Gio(), default)).DanhSach).Loi);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KhongDungTrangThaiSanDaTrackTruocKhiThayDoi(bool confirm)
    {
        var order = await f.Booking(confirm ? TrangThaiDatSan.ChoXuLy : TrangThaiDatSan.DaHuy);
        await using var db = f.Db();
        await db.SanTheThaos.Include(x => x.LoaiSan).SingleAsync(x => x.MaSan == order.MaSan);
        await using (var update = f.Db())
        {
            var court = await update.SanTheThaos.FindAsync(order.MaSan);
            court!.TrangThai = TrangThaiSan.NgungHoatDong;
            await update.SaveChangesAsync();
        }
        var service = f.Service(db);
        Assert.Equal(MaLoiKhaDungSan.SanKhongNhanDat,
            Assert.Single((await service.KiemTraKhaDungAsync([order.MaSan], Gio(), default)).DanhSach).Loi);
        if (confirm)
            Assert.Equal(MaLoiDatSan.SanKhongHoatDong,
                (await service.XacNhanDonAsync(new(order.MaDatSan, order.RowVersion), default)).Loi);
        else
            Assert.Equal(MaLoiDatSan.SanKhongNhanDat,
                (await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
                    f.Clock.HomNay, new(10, 0), new(11, 0)), default)).Loi?.Ma);
    }

    private sealed class DemTruyVan : DbCommandInterceptor
    {
        public int SoLan { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            SoLan++;
            return ValueTask.FromResult(result);
        }
    }
}

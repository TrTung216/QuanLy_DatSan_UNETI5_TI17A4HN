// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: kiểm thử SQL Server cho quy tắc đặt sân, xác nhận/từ chối/hủy/hoàn thành, cọc và quyền sở hữu.
// Kiểm tra thời điểm, lý do, RowVersion, thao tác lặp, khóa đồng thời và rollback transaction.
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class DatSanTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    [Fact]
    public void KetQuaKhongTheThatBaiRongHoacThanhCongKemLoi()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KetQuaTaoDon.DaTao(0));
        Assert.Throws<ArgumentNullException>(() => KetQuaTaoDon.ThatBai(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => KetQuaXuLyDon.ThatBai((MaLoiDatSan)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => KetQuaTaoDon.ThatBai(new((MaLoiDatSan)999)));
        Assert.Null(KetQuaTaoDon.DaTao(1).Loi);
        Assert.Null(KetQuaXuLyDon.ThanhCongResult().Loi);
        foreach (var code in Enum.GetValues<MaLoiDatSan>())
        {
            var error = new LoiDatSan(code, new(6,0), new(22,0));
            Assert.False(KetQuaTaoDon.ThatBai(error).ThanhCong);
            Assert.NotNull(KetQuaTaoDon.ThatBai(error).Loi);
            Assert.False(KetQuaXuLyDon.ThatBai(code).ThanhCong);
            Assert.False(string.IsNullOrWhiteSpace(ThongBaoLoiDatSanViewModel.AnhXa(error).ThongBao));
        }
        Assert.Throws<InvalidOperationException>(() => ThongBaoLoiDatSanViewModel.AnhXa(new((MaLoiDatSan)999)));
        Assert.Throws<InvalidOperationException>(() => ThongBaoLoiDatSanViewModel.AnhXa(new(MaLoiDatSan.NgoaiGioKhiTao)));
    }

    [Fact]
    public async Task XacNhanLuuTrangThaiThoiDiemVaKhongTuTrungLich()
    {
        var order = await f.Booking();
        await using var db = f.Db();
        Assert.True((await f.Service(db).XacNhanDonAsync(new(order.MaDatSan, order.RowVersion), default)).ThanhCong);
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai);
        Assert.Equal(f.Clock.BayGio, saved.NgayXacNhan);
        Assert.Equal(order.DonGia, saved.DonGia);
        Assert.False(order.RowVersion.SequenceEqual(saved.RowVersion));
    }

    [Theory]
    [InlineData(TrangThaiDatSan.DangXuLy)]
    [InlineData(TrangThaiDatSan.HoanThanh)]
    [InlineData(TrangThaiDatSan.DaHuy)]
    [InlineData(TrangThaiDatSan.TuChoi)]
    public async Task KhongXacNhanLaiDonDaXuLy(TrangThaiDatSan state)
    {
        var o = await f.Booking(state);
        await using var db = f.Db();
        Assert.Equal(MaLoiDatSan.ChiXacNhanDonCho,
            (await f.Service(db).XacNhanDonAsync(new(o.MaDatSan, o.RowVersion), default)).Loi);
        Assert.Equal(state, (await f.Read(o.MaDatSan)).TrangThai);
    }

    [Theory]
    [InlineData("inactive", MaLoiDatSan.SanKhongHoatDong)]
    [InlineData("inactive-type", MaLoiDatSan.SanKhongHoatDong)]
    [InlineData("maintenance", MaLoiDatSan.SanBaoTri)]
    [InlineData("hours", MaLoiDatSan.NgoaiGioHoatDong)]
    [InlineData("past", MaLoiDatSan.QuaGioXacNhan)]
    public async Task XacNhanKiemTraLaiSanVaThoiGian(string scenario, MaLoiDatSan expected)
    {
        var o = await f.Booking(startHours: scenario == "past" ? 0 : 2);
        await using (var db = f.Db())
        {
            var san = await db.SanTheThaos.FindAsync(o.MaSan);
            if (scenario == "inactive") san!.TrangThai = TrangThaiSan.NgungHoatDong;
            if (scenario == "inactive-type") san!.LoaiSan = new LoaiSan
            {
                TenLoai = "M4 ngừng " + Guid.NewGuid().ToString("N"),
                TrangThai = TrangThaiLoaiSan.NgungHoatDong,
                DonGiaTheoGio = 200000m, SoNguoiToiDa = 10
            };
            if (scenario == "maintenance") san!.NgayBaoTri = f.Clock.HomNay;
            if (scenario == "hours") san!.GioMoCua = new(12,0);
            await db.SaveChangesAsync();
        }
        await using var act = f.Db();
        Assert.Equal(expected, (await f.Service(act).XacNhanDonAsync(new(o.MaDatSan, o.RowVersion), default)).Loi);
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(o.MaDatSan)).TrangThai);
    }

    [Theory]
    [InlineData(-60, TrangThaiDatSan.ChoXuLy, false)]
    [InlineData(60, TrangThaiDatSan.ChoXuLy, false)]
    [InlineData(-30, TrangThaiDatSan.ChoXuLy, true)]
    [InlineData(30, TrangThaiDatSan.DangXuLy, true)]
    [InlineData(0, TrangThaiDatSan.HoanThanh, true)]
    [InlineData(0, TrangThaiDatSan.DaHuy, false)]
    [InlineData(0, TrangThaiDatSan.TuChoi, false)]
    public async Task TrungLichTheoKhoangVaTrangThai(int offset, TrangThaiDatSan state, bool conflict)
    {
        var o = await f.Booking();
        await using (var db = f.Db())
        {
            db.DatSans.Add(new DatSan { MaKhachHang = f.CustomerId, MaSan = o.MaSan, DonGia = o.DonGia,
                NgayDat = f.Clock.BayGio, GioBatDau = o.GioBatDau.AddMinutes(offset),
                GioKetThuc = o.GioKetThuc.AddMinutes(offset), TrangThai = state });
            await db.SaveChangesAsync();
        }
        await using var act = f.Db();
        var result = await f.Service(act).XacNhanDonAsync(new(o.MaDatSan, o.RowVersion), default);
        Assert.Equal(!conflict, result.ThanhCong);
        if (conflict) Assert.Equal(MaLoiDatSan.TrungLich, result.Loi);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.ChoXuLy, 2, 0, true)]
    [InlineData(TrangThaiDatSan.DangXuLy, 2, 0, true)]
    [InlineData(TrangThaiDatSan.DangXuLy, 2, 100, false)]
    [InlineData(TrangThaiDatSan.ChoXuLy, 2, 100, false)]
    [InlineData(TrangThaiDatSan.ChoXuLy, 0, 0, false)]
    [InlineData(TrangThaiDatSan.HoanThanh, 2, 0, false)]
    [InlineData(TrangThaiDatSan.DaHuy, 2, 0, false)]
    [InlineData(TrangThaiDatSan.TuChoi, 2, 0, false)]
    public async Task NhanVienHuyTheoTrangThaiGioVaCoc(TrangThaiDatSan state, int hours, int deposit, bool allowed)
    {
        var o = await f.Booking(state, hours, deposit);
        await using var db = f.Db();
        var result = await f.Service(db).NhanVienHuyDonAsync(new(o.MaDatSan, "  Đổi kế hoạch  ", o.RowVersion), default);
        Assert.Equal(allowed, result.ThanhCong);
        var saved = await f.Read(o.MaDatSan);
        Assert.Equal(allowed ? TrangThaiDatSan.DaHuy : state, saved.TrangThai);
        if (allowed) { Assert.Equal("Đổi kế hoạch", saved.LyDoHuy); Assert.Equal(f.Clock.BayGio, saved.NgayHuy); }
        else Assert.Null(saved.NgayHuy);
    }

    [Theory]
    [InlineData("TuChoi")]
    [InlineData("KhachHuy")]
    public async Task DonChoCoCocKhongBiTuChoiHoacKhachHuy(string action)
    {
        var order = await f.Booking(deposit: 50000m);
        await using var db = f.Db();
        var service = f.Service(db);
        var request = new HuyDonYeuCau(order.MaDatSan, "Lý do", order.RowVersion);
        var result = action == "TuChoi"
            ? await service.TuChoiDonAsync(request, default)
            : await service.KhachHangHuyDonAsync(request, f.CustomerAccountId, default);
        Assert.Equal(MaLoiDatSan.DaNhanCoc, result.Loi);
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(order.RowVersion, saved.RowVersion);
        Assert.Equal(50000m, saved.TienCoc);
        Assert.Null(saved.NgayHuy);
    }

    [Fact]
    public async Task M3DocPhienBanVaHuyDonQuaHopDongDungChung()
    {
        var order = await f.Booking();
        await using var db = f.Db();
        IDatSanService service = f.Service(db);
        Assert.Null(await service.LayChiTietCaNhanAsync(f.OtherCustomerAccountId, order.MaDatSan, default));
        var detail = await service.LayChiTietCaNhanAsync(f.CustomerAccountId, order.MaDatSan, default);
        Assert.NotNull(detail);
        Assert.Equal(order.RowVersion, detail.PhienBan);
        var request = new HuyDonYeuCau(detail.MaDatSan, "Đổi kế hoạch", detail.PhienBan);
        Assert.True((await service.KhachHangHuyDonAsync(request, f.CustomerAccountId, default)).ThanhCong);
        var saved = await f.Read(order.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DaHuy, saved.TrangThai);
        Assert.Equal(f.Clock.BayGio, saved.NgayHuy);
        Assert.Equal("Đổi kế hoạch", saved.LyDoHuy);
        await using var retry = f.Db();
        Assert.Equal(MaLoiDatSan.PhienBanDaCu,
            (await f.Service(retry).KhachHangHuyDonAsync(request, f.CustomerAccountId, default)).Loi);
    }

    [Fact]
    public async Task HuyKhachKhongLoDonNguoiKhacQuaPhienBan()
    {
        var o = await f.Booking();
        foreach (var rv in new[] { o.RowVersion, new byte[8] })
        {
            await using var db = f.Db();
            Assert.Equal(MaLoiDatSan.KhongTonTai, (await f.Service(db).KhachHangHuyDonAsync(
                new(o.MaDatSan, "Hủy", rv), f.OtherCustomerAccountId, default)).Loi);
        }
        await using var act = f.Db();
        Assert.True((await f.Service(act).KhachHangHuyDonAsync(new(o.MaDatSan, "Hủy", o.RowVersion),
            f.CustomerAccountId, default)).ThanhCong);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.DangXuLy, 2)]
    [InlineData(TrangThaiDatSan.ChoXuLy, 0)]
    public async Task KhachKhongHuyDonXacNhanHoacQuaGio(TrangThaiDatSan state, int hours)
    {
        var o = await f.Booking(state, hours);
        await using var db = f.Db();
        Assert.False((await f.Service(db).KhachHangHuyDonAsync(new(o.MaDatSan, "Hủy", o.RowVersion),
            f.CustomerAccountId, default)).ThanhCong);
        Assert.Equal(state, (await f.Read(o.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task TuChoiCanLyDoVaKhongMoLaiDon()
    {
        var o = await f.Booking();
        foreach (var reason in new[] { " ", new string('a',1001) })
        {
            await using var db = f.Db();
            Assert.Equal(MaLoiDatSan.LyDoTuChoiKhongHopLe,
                (await f.Service(db).TuChoiDonAsync(new(o.MaDatSan, reason, o.RowVersion), default)).Loi);
            Assert.Equal(MaLoiDatSan.LyDoHuyKhongHopLe,
                (await f.Service(db).NhanVienHuyDonAsync(new(o.MaDatSan, reason, o.RowVersion), default)).Loi);
        }
        await using (var db = f.Db())
            Assert.True((await f.Service(db).TuChoiDonAsync(new(o.MaDatSan, "  Bảo trì  ", o.RowVersion), default)).ThanhCong);
        var saved = await f.Read(o.MaDatSan);
        Assert.Equal(TrangThaiDatSan.TuChoi, saved.TrangThai);
        Assert.Equal("Bảo trì", saved.LyDoHuy);
        Assert.Equal(f.Clock.BayGio, saved.NgayHuy);
        await using var retry = f.Db();
        Assert.Equal(MaLoiDatSan.ChiTuChoiDonCho,
            (await f.Service(retry).TuChoiDonAsync(new(o.MaDatSan, "Lại", saved.RowVersion), default)).Loi);
    }

    [Theory]
    [InlineData(-1, true, 200000, 20000, 0, true)]
    [InlineData(0, true, 200000, 0, 0, false)]
    [InlineData(-1, false, 200000, 0, 0, false)]
    [InlineData(-1, true, 0, 0, 0, true)]
    [InlineData(-1, true, 100, 0, 200, false)]
    public async Task HoanThanhLuuTienVaThoiDiemCungGiaoDich(int hours, bool paid, int court, int service, int deposit, bool allowed)
    {
        var o = await f.Booking(TrangThaiDatSan.DangXuLy, hours, deposit);
        await using (var seed = f.Db())
        {
            var don = await seed.DatSans.SingleAsync(x => x.MaDatSan == o.MaDatSan);
            don.DonGia = court;
            if (service > 0)
                don.ChiTietDatSans.Add(new ChiTietDatSan
                {
                    DichVu = new DichVu { TenDichVu = "Nước", DonViTinh = "Chai", DonGia = 99999 },
                    SoLuong = 1, DonGia = service
                });
            await seed.SaveChangesAsync();
        }
        o = await f.Read(o.MaDatSan);
        await using var db = f.Db();
        Assert.Equal(allowed, (await f.Service(db).HoanThanhDonAsync(
            new(o.MaDatSan, paid, o.RowVersion), default)).ThanhCong);
        var saved = await f.Read(o.MaDatSan);
        if (allowed)
        {
            Assert.Equal(TrangThaiDatSan.HoanThanh, saved.TrangThai);
            Assert.Equal(court + service, saved.TongTien);
            Assert.Equal(f.Clock.BayGio, saved.NgayHoanThanh);
        }
        else { Assert.Null(saved.TongTien); Assert.Null(saved.NgayHoanThanh); }
    }

    [Fact]
    public async Task PhienBanCuKhongGhiDeDuLieu()
    {
        var o = await f.Booking();
        await using (var db = f.Db())
            Assert.True((await f.Service(db).XacNhanDonAsync(new(o.MaDatSan, o.RowVersion), default)).ThanhCong);
        await using var act = f.Db();
        Assert.Equal(MaLoiDatSan.PhienBanDaCu,
            (await f.Service(act).NhanVienHuyDonAsync(new(o.MaDatSan, "Hủy", o.RowVersion), default)).Loi);
        Assert.Equal(TrangThaiDatSan.DangXuLy, (await f.Read(o.MaDatSan)).TrangThai);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.ChoXuLy)]
    [InlineData(TrangThaiDatSan.HoanThanh)]
    [InlineData(TrangThaiDatSan.DaHuy)]
    [InlineData(TrangThaiDatSan.TuChoi)]
    public async Task HoanThanhChiChapNhanDonDangXuLy(TrangThaiDatSan state)
    {
        var o = await f.Booking(state, -1);
        await using var db = f.Db();
        Assert.Equal(MaLoiDatSan.ChiHoanThanhDonXacNhan,
            (await f.Service(db).HoanThanhDonAsync(new(o.MaDatSan, true, o.RowVersion), default)).Loi);
        var saved = await f.Read(o.MaDatSan);
        Assert.Equal(state, saved.TrangThai);
        Assert.Null(saved.TongTien);
        Assert.Equal(o.RowVersion, saved.RowVersion);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.DangXuLy)]
    [InlineData(TrangThaiDatSan.HoanThanh)]
    [InlineData(TrangThaiDatSan.DaHuy)]
    [InlineData(TrangThaiDatSan.TuChoi)]
    public async Task TuChoiChiChapNhanDonChoXuLy(TrangThaiDatSan state)
    {
        var o = await f.Booking(state);
        await using var db = f.Db();
        Assert.Equal(MaLoiDatSan.ChiTuChoiDonCho,
            (await f.Service(db).TuChoiDonAsync(new(o.MaDatSan, "Từ chối", o.RowVersion), default)).Loi);
        var saved = await f.Read(o.MaDatSan);
        Assert.Equal(state, saved.TrangThai);
        Assert.Equal(o.RowVersion, saved.RowVersion);
    }

    [Fact]
    public async Task HaiYeuCauTaoCungKhungGioChiMotDonDuocLuu()
    {
        var seed = await f.Booking(TrangThaiDatSan.DaHuy);
        await using var blocker = f.Db();
        await using var tx = await blocker.Database.BeginTransactionAsync();
        await f.Service(blocker).KhoaSanAsync(seed.MaSan, default);
        var observer = new LockObserver();
        async Task<KetQuaTaoDon> Create()
        {
            await using var db = f.Db(observer);
            return await f.Service(db).TaoDonAsync(new(f.CustomerAccountId, seed.MaSan,
                DateOnly.FromDateTime(seed.GioBatDau), TimeOnly.FromDateTime(seed.GioBatDau),
                TimeOnly.FromDateTime(seed.GioKetThuc)), default);
        }
        var first = Create(); var second = Create();
        await observer.BothWaiting.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await tx.CommitAsync();
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Single(results, x => x.ThanhCong);
        Assert.Equal(MaLoiDatSan.TrungLichKhiTao, results.Single(x => !x.ThanhCong).Loi!.Ma);
        await using var check = f.Db();
        Assert.Equal(1, await check.DatSans.CountAsync(x => x.MaSan == seed.MaSan && x.TrangThai == TrangThaiDatSan.ChoXuLy));
    }

    [Fact]
    public async Task HaiNhanVienXacNhanCungDonChiMotYeuCauThanhCong()
    {
        var o = await f.Booking();
        await using var blocker = f.Db();
        await using var tx = await blocker.Database.BeginTransactionAsync();
        await f.Service(blocker).KhoaSanAsync(o.MaSan, default);
        var observer = new LockObserver();
        async Task<KetQuaXuLyDon> Confirm()
        {
            await using var db = f.Db(observer);
            return await f.Service(db).XacNhanDonAsync(new(o.MaDatSan, o.RowVersion), default);
        }
        var first = Confirm(); var second = Confirm();
        await observer.BothWaiting.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await tx.CommitAsync();
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Single(results, x => x.ThanhCong);
        Assert.Equal(MaLoiDatSan.PhienBanDaCu, results.Single(x => !x.ThanhCong).Loi);
    }

    [Fact]
    public async Task XungDotKhongDeLaiThayDoiChoLanSaveSau()
    {
        var o = await f.Booking();
        await using var db = f.Db(new ConflictOnce());
        Assert.Equal(MaLoiDatSan.XungDotDuLieu,
            (await f.Service(db).XacNhanDonAsync(new(o.MaDatSan, o.RowVersion), default)).Loi);
        await db.SaveChangesAsync();
        Assert.Equal(TrangThaiDatSan.ChoXuLy, (await f.Read(o.MaDatSan)).TrangThai);
    }

    [Fact]
    public async Task LoiSauKhiGhiPhaiRollbackCaTienVaTrangThai()
    {
        var o = await f.Booking(TrangThaiDatSan.DangXuLy, -1);
        await using var db = f.Db(new ThrowAfterWrite());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(db).HoanThanhDonAsync(
            new(o.MaDatSan, true, o.RowVersion), default));
        var saved = await f.Read(o.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai);
        Assert.Null(saved.TongTien); Assert.Null(saved.TienSan); Assert.Null(saved.TienDichVu);
        Assert.Null(saved.NgayHoanThanh);
    }

    [Theory]
    [InlineData(0,8,MaLoiDatSan.MaDonKhongHopLe)]
    [InlineData(1,0,MaLoiDatSan.PhienBanKhongHopLe)]
    [InlineData(1,7,MaLoiDatSan.PhienBanKhongHopLe)]
    [InlineData(int.MaxValue,8,MaLoiDatSan.KhongTonTai)]
    public async Task YeuCauKhongHopLeTraMaLoi(int id,int versionLength,MaLoiDatSan expected)
    {
        await using var db=f.Db();
        Assert.Equal(expected,(await f.Service(db).XacNhanDonAsync(new(id,new byte[versionLength]),default)).Loi);
    }

    private sealed class ConflictOnce : SaveChangesInterceptor
    {
        private bool thrown;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result,CancellationToken cancellationToken=default)
        {
            if(!thrown) { thrown=true; throw new DbUpdateConcurrencyException("Simulated race at write"); }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowAfterWrite : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData,int result,
            CancellationToken cancellationToken=default) => throw new InvalidOperationException("Failure after SQL write, before commit");
    }

    private sealed class LockObserver : DbCommandInterceptor
    {
        private int attempts;
        public TaskCompletionSource BothWaiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK") && Interlocked.Increment(ref attempts) == 2)
                BothWaiting.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
}

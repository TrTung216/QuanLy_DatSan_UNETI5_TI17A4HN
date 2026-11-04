// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Kiểm thử đối soát cọc, phân quyền, chống trùng và rollback.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class CocTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    private async Task<DatSan> DonMoi()
    {
        // Dữ liệu hợp đồng M4 sau tích hợp; không kích hoạt luồng tạo đơn của M3.
        var seed = await f.Booking();
        await using var db = f.Db();
        var don = await db.DatSans.FindAsync(seed.MaDatSan);
        don!.PhienBanChinhSachCoc = 1;
        don.TienCocYeuCau = 100000m;
        await db.SaveChangesAsync();
        return await f.Read(seed.MaDatSan);
    }
    private async Task<GiaoDichCocDto> GiaoDich(DatSan don, decimal? soTien = null, string? noiDung = null, DateTime? ngay = null)
    {
        await using var db = f.Db();
        var result = await f.Service(db).TaoGiaoDichMoPhongAsync(new(Guid.NewGuid().ToString("N"),
            noiDung ?? $"  coc   {don.MaDatSan}  ", soTien ?? don.TienCocYeuCau, ngay ?? f.Clock.BayGio), default);
        Assert.True(result.ThanhCong);
        return result.DuLieu!;
    }

    [Fact]
    public async Task DocSnapshotCocVaChanDuongXacNhanCu()
    {
        var don = await DonMoi();
        Assert.Equal(1, don.PhienBanChinhSachCoc);
        Assert.Equal(100000m, don.TienCocYeuCau);
        await using var db = f.Db();
        Assert.Equal(MaLoiDatSan.YeuCauDoiSoatCoc, (await f.Service(db).XacNhanDonAsync(new(don.MaDatSan, don.RowVersion), default)).Loi);
        Assert.Equal(MaLoiDatSan.YeuCauDoiSoatCoc, (await f.Service(db).GhiCocAsync(new(don.MaDatSan, 1, don.RowVersion), default)).Loi);
        var san = await db.SanTheThaos.SingleAsync(x => x.MaSan == don.MaSan);
        san.DonGia = 900000m;
        await db.SaveChangesAsync();
        var info = await f.Service(db, f.CustomerAccountId).LayThongTinCocAsync(don.MaDatSan, default);
        Assert.Equal(200000m, info.DuLieu!.TienSanDuKien);
        Assert.Equal(100000m, info.DuLieu.TienCocYeuCau);
    }

    [Fact]
    public async Task XacNhanNguyenTuVaRetryPhienBanCuKhongGhiHaiLan()
    {
        var don = await DonMoi();
        var gd = await GiaoDich(don);
        await using var db = f.Db();
        var svc = f.Service(db);
        var yc = new XacNhanCocYeuCau(don.MaDatSan, gd.MaGiaoDich, don.RowVersion, true);
        var result = await svc.XacNhanCocVaDatSanAsync(yc, default);
        Assert.True(result.ThanhCong);
        Assert.False(result.DuLieu!.DaXuLyTruocDo);
        Assert.True(result.DuLieu.BienNhan.MaBienNhan > 0);
        Assert.NotEqual(don.RowVersion, result.DuLieu.PhienBanMoi);
        var retry = await svc.XacNhanCocVaDatSanAsync(yc, default);
        Assert.True(retry.DuLieu!.DaXuLyTruocDo);
        Assert.Equal(result.DuLieu.BienNhan, retry.DuLieu.BienNhan);
        Assert.Single(await db.Set<BienNhanCoc>().Where(x => x.MaDatSan == don.MaDatSan).ToListAsync());
        var saved = await f.Read(don.MaDatSan);
        Assert.Equal(TrangThaiDatSan.DangXuLy, saved.TrangThai);
        Assert.Equal(don.TienCocYeuCau, saved.TienCoc);
        Assert.Equal(f.Clock.BayGio, saved.NgayXacNhan);
    }

    [Theory]
    [InlineData("thieu", MaLoiCoc.SoTienKhongKhop)]
    [InlineData("thua", MaLoiCoc.SoTienKhongKhop)]
    [InlineData("ngay", MaLoiCoc.ThoiDiemNhanKhongHopLe)]
    [InlineData("noidung", MaLoiCoc.GiaoDichKhongKhop)]
    [InlineData("phienban", MaLoiCoc.PhienBanDaCu)]
    [InlineData("checkbox", MaLoiCoc.ChuaKiemTraGiaoDich)]
    public async Task TuChoiSaiDuLieuKhongGhiCoc(string scenario, MaLoiCoc expected)
    {
        var don = await DonMoi();
        var gd = await GiaoDich(don, scenario == "thieu" ? 99999m : scenario == "thua" ? 100001m : null,
            scenario == "noidung" ? $"COC {don.MaDatSan}0" : null,
            scenario == "ngay" ? don.NgayDat.AddMinutes(-1) : null);
        await using var db = f.Db();
        var result = await f.Service(db).XacNhanCocVaDatSanAsync(new(don.MaDatSan, gd.MaGiaoDich,
            scenario == "phienban" ? new byte[8] : don.RowVersion, scenario != "checkbox"), default);
        Assert.Equal(expected, result.Loi);
        Assert.Equal(0m, (await f.Read(don.MaDatSan)).TienCoc);
        Assert.False(await db.Set<BienNhanCoc>().AnyAsync(x => x.MaDatSan == don.MaDatSan));
        if (scenario is "thieu" or "thua" or "ngay")
            Assert.Equal(TrangThaiGiaoDichCoc.CanXuLyNgoaiLe,
                (await db.Set<GiaoDichCoc>().AsNoTracking().SingleAsync(x => x.MaGiaoDich == gd.MaGiaoDich)).TrangThai);
    }

    [Fact]
    public async Task GiaoDichSaiTienVanChanHuyVaTuChoi()
    {
        var don = await DonMoi();
        await GiaoDich(don, 1m);
        await using var db = f.Db();
        var request = new HuyDonYeuCau(don.MaDatSan, "Đổi lịch", don.RowVersion);
        Assert.Equal(MaLoiDatSan.GiaoDichChoDoiSoat, (await f.Service(db).TuChoiDonAsync(request, default)).Loi);
        Assert.Equal(MaLoiDatSan.GiaoDichChoDoiSoat, (await f.Service(db).NhanVienHuyDonAsync(request, default)).Loi);
        Assert.Equal(MaLoiDatSan.GiaoDichChoDoiSoat, (await f.Service(db, f.CustomerAccountId).KhachHangHuyDonAsync(request, f.CustomerAccountId, default)).Loi);
    }

    [Fact]
    public async Task QuyenSoHuuVaQuyenQuanLyDuocKiemTraTaiService()
    {
        var don = await DonMoi();
        var gd = await GiaoDich(don);
        await using var db = f.Db();
        Assert.Equal(MaLoiCoc.KhongTonTai, (await f.Service(db, f.OtherCustomerAccountId).LayThongTinCocAsync(don.MaDatSan, default)).Loi);
        Assert.Equal(MaLoiCoc.KhongDuQuyen, (await f.Service(db, f.CustomerAccountId).LayDoiSoatCocAsync(don.MaDatSan, default)).Loi);
        Assert.Equal(MaLoiCoc.KhongDuQuyen, (await f.Service(db, f.CustomerAccountId).XacNhanCocVaDatSanAsync(new(don.MaDatSan, gd.MaGiaoDich, don.RowVersion, true), default)).Loi);
        Assert.Equal(MaLoiCoc.KhongDuQuyen, (await f.Service(db, int.MaxValue).LayDoiSoatCocAsync(don.MaDatSan, default)).Loi);
    }

    [Theory]
    [InlineData(false, "Development")]
    [InlineData(true, "Production")]
    public async Task TatMoPhongKhongHaChinhSachCoc(bool enabled, string environment)
    {
        var don = await DonMoi();
        await using var db = f.Db();
        var svc = f.Service(db, enabled: enabled, environment: environment);
        Assert.Equal(MaLoiCoc.MoPhongKhongDuocPhep, (await svc.TaoGiaoDichMoPhongAsync(new("TEST", $"COC {don.MaDatSan}", 100000m, f.Clock.BayGio), default)).Loi);
        var info = await f.Service(db, f.CustomerAccountId, enabled: enabled, environment: environment).LayThongTinCocAsync(don.MaDatSan, default);
        Assert.Equal(TrangThaiCocHienThi.KhongSanSang, info.DuLieu!.TrangThaiCoc);
        Assert.False(info.DuLieu.CoTheChuyen);
        Assert.Null(info.DuLieu.NoiDungQrMoPhong);
        Assert.Equal(1, info.DuLieu.PhienBanChinhSachCoc);
    }

    [Fact]
    public async Task HaiNhanVienCungXacNhanChiMotBienNhan()
    {
        var don = await DonMoi();
        var gd = await GiaoDich(don);
        async Task<KetQuaCoc<XacNhanCocDto>> Run()
        {
            await using var db = f.Db();
            return await f.Service(db).XacNhanCocVaDatSanAsync(new(don.MaDatSan, gd.MaGiaoDich, don.RowVersion, true), default);
        }
        var results = await Task.WhenAll(Run(), Run());
        Assert.All(results, r => Assert.True(r.ThanhCong));
        Assert.Single(results, r => !r.DuLieu!.DaXuLyTruocDo);
    }

    [Fact]
    public async Task LoiSauSaveChangesRollbackBienNhanTienVaTrangThai()
    {
        var don = await DonMoi();
        var gd = await GiaoDich(don);
        await using var db = f.Db(new LoiSauLuu());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service(db).XacNhanCocVaDatSanAsync(new(don.MaDatSan, gd.MaGiaoDich, don.RowVersion, true), default));
        await using var read = f.Db();
        Assert.Equal(0m, (await f.Read(don.MaDatSan)).TienCoc);
        Assert.False(await read.Set<BienNhanCoc>().AnyAsync(x => x.MaDatSan == don.MaDatSan));
        Assert.Equal(TrangThaiGiaoDichCoc.ChuaDoiSoat, (await read.Set<GiaoDichCoc>().SingleAsync(x => x.MaGiaoDich == gd.MaGiaoDich)).TrangThai);
        Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified);
    }
    [Fact]
    public async Task TrungMaNgoaiKhongTaoThemGiaoDich()
    {
        var don = await DonMoi();
        var gd = await GiaoDich(don);
        await using var db = f.Db();
        var result = await f.Service(db).TaoGiaoDichMoPhongAsync(new(gd.MaGiaoDichNgoai.ToLowerInvariant(), gd.NoiDung, gd.SoTien, gd.NgayNhan), default);
        Assert.Equal(MaLoiCoc.TrungMaGiaoDich, result.Loi);
        Assert.Single(await db.Set<GiaoDichCoc>().Where(x => x.NoiDung == gd.NoiDung).ToListAsync());
    }

    [Fact]
    public async Task TienDenSauHuyDuocLuuNgoaiLeKhongMoLaiDon()
    {
        var don = await DonMoi();
        await using var db = f.Db();
        Assert.True((await f.Service(db, f.CustomerAccountId).KhachHangHuyDonAsync(new(don.MaDatSan, "Hủy", don.RowVersion), f.CustomerAccountId, default)).ThanhCong);
        var gd = await GiaoDich(don);
        Assert.Equal(TrangThaiGiaoDichCoc.CanXuLyNgoaiLe, gd.TrangThai);
        Assert.Equal(TrangThaiDatSan.DaHuy, (await f.Read(don.MaDatSan)).TrangThai);
        var saved = await f.Read(don.MaDatSan);
        Assert.Equal(MaLoiCoc.DieuKienDonKhongHopLe,
            (await f.Service(db).XacNhanCocVaDatSanAsync(new(don.MaDatSan, gd.MaGiaoDich, saved.RowVersion, true), default)).Loi);
    }

    [Fact]
    public async Task MotGiaoDichKhongTheDungChoHaiDonKhacSan()
    {
        var don = await DonMoi(); var other = await DonMoi();
        var gd = await GiaoDich(don);
        async Task<KetQuaCoc<XacNhanCocDto>> Run(DatSan d)
        {
            await using var db = f.Db();
            return await f.Service(db).XacNhanCocVaDatSanAsync(new(d.MaDatSan, gd.MaGiaoDich, d.RowVersion, true), default);
        }
        var results = await Task.WhenAll(Run(don), Run(other));
        Assert.True(results[0].ThanhCong);
        Assert.Contains(results[1].Loi, new MaLoiCoc?[] { MaLoiCoc.GiaoDichKhongKhop, MaLoiCoc.GiaoDichDaSuDung });
        Assert.Equal(0m, (await f.Read(other.MaDatSan)).TienCoc);
    }

    [Fact]
    public async Task DonMienPhiXacNhanThuongKhongTaoBienNhan()
    {
        var don = await DonMoi();
        await using var db = f.Db();
        var d = await db.DatSans.FindAsync(don.MaDatSan);
        d!.DonGia = 0; d.TienCocYeuCau = 0;
        await db.SaveChangesAsync();
        Assert.True((await f.Service(db).XacNhanDonAsync(new(d.MaDatSan, d.RowVersion), default)).ThanhCong);
        Assert.False(await db.Set<BienNhanCoc>().AnyAsync(x => x.MaDatSan == don.MaDatSan));
    }

    [Fact]
    public async Task KhoaTaiKhoanSauNhanCocVanChanRetry()
    {
        var don = await DonMoi(); var gd = await GiaoDich(don);
        await using var db = f.Db();
        var yc = new XacNhanCocYeuCau(don.MaDatSan, gd.MaGiaoDich, don.RowVersion, true);
        Assert.True((await f.Service(db).XacNhanCocVaDatSanAsync(yc, default)).ThanhCong);
        var actor = await db.TaiKhoans.FindAsync(f.AdminAccountId);
        try
        {
            actor!.TrangThai = TrangThaiTaiKhoan.BiKhoa; await db.SaveChangesAsync();
            Assert.Equal(MaLoiCoc.KhongDuQuyen, (await f.Service(db).XacNhanCocVaDatSanAsync(yc, default)).Loi);
        }
        finally { actor!.TrangThai = TrangThaiTaiKhoan.HoatDong; await db.SaveChangesAsync(); }
    }

    private sealed class LoiSauLuu : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Lỗi thử nghiệm sau ghi.");
    }
}

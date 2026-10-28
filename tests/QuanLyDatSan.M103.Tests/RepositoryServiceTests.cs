// Codex: kiểm thử service bằng repository giả, không khởi tạo EF/SQL.
using System.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public sealed class RepositoryServiceTests
{
    private static GhiLoaiSan HopLe => new("  Loại mới  ", null, 10, 100000, TrangThaiLoaiSan.HoatDong);

    [Fact]
    public async Task DuLieuSaiKhongGoiRepositoryHoacLuu()
    {
        var repo = new KhoGia(); var uow = new LuuGia();
        var result = await new LoaiSanService(repo, uow).TaoAsync(HopLe with { TenLoai = "" }, default);
        Assert.Equal(MaLoiLoaiSan.DuLieuKhongHopLe, result.MaLoi);
        Assert.Equal(0, repo.SoLanKiemTra);
        Assert.Equal(0, uow.SoLanLuu);
    }

    [Fact]
    public async Task TrungTenTraLoiNghiepVuVaKhongThem()
    {
        var repo = new KhoGia { TrungTen = true }; var uow = new LuuGia();
        var result = await new LoaiSanService(repo, uow).TaoAsync(HopLe, default);
        Assert.Equal(MaLoiLoaiSan.TrungTen, result.MaLoi);
        Assert.Equal("Loại mới", repo.TenDaKiemTra);
        Assert.Null(repo.DaThem);
        Assert.Equal(0, uow.SoLanLuu);
    }

    [Theory]
    [InlineData(MaLoiLuuTru.TrungTenLoaiSan, MaLoiLoaiSan.TrungTen)]
    [InlineData(MaLoiLuuTru.XungDotPhienBan, MaLoiLoaiSan.XungDotDuLieu)]
    public async Task LoiLuuDaBietGiuMaLoiHienHanh(MaLoiLuuTru loi, MaLoiLoaiSan mongDoi)
    {
        var repo = new KhoGia();
        var uow = new LuuGia { Loi = new LoiLuuTruException(loi, new Exception("Giả lập")) };
        var result = await new LoaiSanService(repo, uow).TaoAsync(HopLe, default);
        Assert.Equal(mongDoi, result.MaLoi);
        Assert.Equal("Loại mới", repo.DaThem!.TenLoai);
    }

    [Fact]
    public async Task KhongNhanNhamLoiUniqueCuaModuleKhac()
    {
        var uow = new LuuGia { Loi = new LoiLuuTruException(MaLoiLuuTru.TrungTenTaiKhoan, new Exception()) };
        await Assert.ThrowsAsync<LoiLuuTruException>(() => new LoaiSanService(new KhoGia(), uow).TaoAsync(HopLe, default));
    }

    [Fact]
    public async Task TokenDuocTruyenDenRepositoryVaUnitOfWork()
    {
        using var cts = new CancellationTokenSource();
        var repo = new KhoGia(); var uow = new LuuGia();
        await new LoaiSanService(repo, uow).TaoAsync(HopLe, cts.Token);
        Assert.Equal(cts.Token, repo.Token);
        Assert.Equal(cts.Token, uow.Token);
    }

    private sealed class LuuGia : IDonViCongViec
    {
        public int SoLanLuu { get; private set; }
        public Exception? Loi { get; init; }
        public CancellationToken Token { get; private set; }
        public Task LuuAsync(CancellationToken cancellationToken)
        {
            SoLanLuu++; Token = cancellationToken;
            return Loi is null ? Task.CompletedTask : Task.FromException(Loi);
        }
        public Task<T> TrongGiaoDichAsync<T>(Func<CancellationToken, Task<T>> xuLy, Func<T, bool> duocLuu,
            IsolationLevel isolationLevel, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class KhoGia : ILoaiSanRepository
    {
        public bool TrungTen { get; init; }
        public int SoLanKiemTra { get; private set; }
        public string? TenDaKiemTra { get; private set; }
        public LoaiSan? DaThem { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<bool> TrungTenAsync(string ten, int? id, CancellationToken cancellationToken)
        {
            SoLanKiemTra++; TenDaKiemTra = ten; Token = cancellationToken;
            return Task.FromResult(TrungTen);
        }
        public void Them(LoaiSan loaiSan) => DaThem = loaiSan;
        public void Xoa(LoaiSan loaiSan) => throw new NotSupportedException();
        public Task<bool> CoSanThamChieuAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LoaiSan?> LayDeCapNhatAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TrangLoaiSan> LayDanhSachAsync(string? tuKhoa, TrangThaiLoaiSan? trangThai, string sapXep,
            int trang, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}

// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ unit test nghiệp vụ với repository giả, không cần DB.
using System.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class LoaiSanServiceRepositoryTests
{
    [Fact]
    public async Task DuLieuSaiBiChanTruocKhiTruyCapRepository()
    {
        var repo = new RepositoryGia(); var uow = new DonViGia();
        var result = await new LoaiSanService(repo, uow).TaoAsync(new(" ", null, 0, -1, TrangThaiLoaiSan.HoatDong), default);
        Assert.Equal(MaLoiLoaiSan.DuLieuKhongHopLe, result.MaLoi);
        Assert.Equal(0, repo.SoLanTruyVan); Assert.False(uow.DaLuu);
    }

    [Fact]
    public async Task CoThamChieuKhongXoaVaKhongLuu()
    {
        var repo = new RepositoryGia { CoThamChieu = true }; var uow = new DonViGia();
        var result = await new LoaiSanService(repo, uow).XoaAsync(1, default);
        Assert.Equal(MaLoiLoaiSan.DangDuocThamChieu, result.MaLoi);
        Assert.False(repo.DaXoa); Assert.False(uow.DaLuu);
    }

    [Theory]
    [InlineData(MaLoiLuuTru.TrungTenLoaiSan, MaLoiLoaiSan.TrungTen)]
    [InlineData(MaLoiLuuTru.XungDotPhienBan, MaLoiLoaiSan.XungDotDuLieu)]
    public async Task LoiLuuDaBietGiuMaNghiepVu(MaLoiLuuTru loi, MaLoiLoaiSan mongDoi)
    {
        var repo = new RepositoryGia(); var uow = new DonViGia { Loi = loi };
        var result = await new LoaiSanService(repo, uow).TaoAsync(new("San", null, 10, 100000, TrangThaiLoaiSan.HoatDong), default);
        Assert.Equal(mongDoi, result.MaLoi); Assert.True(uow.DaLuu);
    }

    [Fact]
    public async Task LoiKhongThuocLoaiSanKhongBiCheThanhTrungTen()
    {
        var service = new LoaiSanService(new RepositoryGia(), new DonViGia { Loi = MaLoiLuuTru.TrungTenDangNhap });
        await Assert.ThrowsAsync<LoiLuuTruException>(() => service.TaoAsync(new("San", null, 10, 100000, TrangThaiLoaiSan.HoatDong), default));
    }

    private sealed class DonViGia : IDonViCongViec
    {
        public bool DaLuu { get; private set; }
        public MaLoiLuuTru? Loi { get; init; }
        public Task LuuAsync(CancellationToken ct)
        {
            DaLuu = true;
            if (Loi.HasValue) throw new LoiLuuTruException(Loi.Value, new Exception("TEST"));
            return Task.CompletedTask;
        }
        public Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> congViec, IsolationLevel mucCoLap, CancellationToken ct) =>
            throw new NotSupportedException("Test này không gọi transaction nhiều bước.");
    }

    private sealed class RepositoryGia : ILoaiSanRepository
    {
        public bool CoThamChieu { get; init; }
        public int SoLanTruyVan { get; private set; }
        public bool DaXoa { get; private set; }
        public Task<LoaiSan?> LayDeCapNhatAsync(int id, CancellationToken ct)
        { SoLanTruyVan++; return Task.FromResult<LoaiSan?>(new LoaiSan { MaLoaiSan = id }); }
        public Task<bool> TrungTenAsync(string ten, int? id, CancellationToken ct)
        { SoLanTruyVan++; return Task.FromResult(false); }
        public Task<bool> CoSanThamChieuAsync(int id, CancellationToken ct)
        { SoLanTruyVan++; return Task.FromResult(CoThamChieu); }
        public void Them(LoaiSan loaiSan) { }
        public void Xoa(LoaiSan loaiSan) => DaXoa = true;
        public Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<TrangLoaiSan> LayDanhSachAsync(string? tuKhoa, TrangThaiLoaiSan? trangThai, string sapXep, int trang, CancellationToken ct) => throw new NotSupportedException();
    }
}

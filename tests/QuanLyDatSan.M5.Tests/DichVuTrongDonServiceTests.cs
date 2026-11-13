using System.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan.M5.Tests;

public class DichVuTrongDonServiceTests
{
    private static readonly byte[] RowVersion = [1, 2, 3, 4, 5, 6, 7, 8];
    private const int MaTaiKhoan = 21;

    [Fact]
    public async Task SuaSoLuong_DonChoXuLy_BiTuChoi()
    {
        var (service, repository, uow) = TaoService(TrangThaiDatSan.ChoXuLy);

        var result = await service.SuaSoLuongAsync(1, 11, 3, Convert.ToBase64String(RowVersion), default);

        Assert.False(result.ThanhCong);
        Assert.Equal(2, repository.Don.ChiTietDatSans.Single().SoLuong);
        Assert.Equal(0, uow.SoLanLuu);
    }

    [Fact]
    public async Task Huy_DonDangXuLy_GiuLaiLichSuVaTinhLaiTongTien()
    {
        var (service, repository, uow) = TaoService(TrangThaiDatSan.DangXuLy);

        var result = await service.HuyAsync(1, 11, Convert.ToBase64String(RowVersion), default);

        Assert.True(result.ThanhCong);
        Assert.Empty(repository.Don.ChiTietDatSans);
        var history = Assert.Single(repository.LichSuHuy);
        Assert.Equal(11, history.MaChiTiet);
        Assert.Equal(2, history.SoLuong);
        Assert.Equal(15000m, history.DonGia);
        Assert.Equal(MaTaiKhoan, history.MaTaiKhoanHuy);
        Assert.True(history.DaHuy);
        Assert.NotNull(history.NgayHuy);
        Assert.Equal(0m, repository.Don.TienDichVu);
        Assert.Equal(1, uow.SoLanLuu);
        Assert.Equal(1, repository.SoLanKhoaSan);
    }

    [Fact]
    public async Task Huy_SauDoThemLaiCungDichVu_GiuLaiBanGhiHuyVaThemDongMoi()
    {
        var (service, repository, _) = TaoService(TrangThaiDatSan.DangXuLy);

        var cancel = await service.HuyAsync(1, 11, Convert.ToBase64String(RowVersion), default);
        var add = await service.ThemAsync(1, 7, 1, Convert.ToBase64String(repository.Don.RowVersion), default);

        Assert.True(cancel.ThanhCong);
        Assert.True(add.ThanhCong);
        Assert.Single(repository.LichSuHuy);
        Assert.Equal(1, Assert.Single(repository.Don.ChiTietDatSans).SoLuong);
    }

    [Fact]
    public async Task SuaSoLuong_RowVersionCu_BiTuChoiTruocKhiThayDoi()
    {
        var (service, repository, uow) = TaoService(TrangThaiDatSan.DangXuLy);
        var oldVersion = Convert.ToBase64String([8, 7, 6, 5, 4, 3, 2, 1]);

        var result = await service.SuaSoLuongAsync(1, 11, 4, oldVersion, default);

        Assert.False(result.ThanhCong);
        Assert.Contains("cập nhật", result.Loi, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, repository.Don.ChiTietDatSans.Single().SoLuong);
        Assert.Equal(0, uow.SoLanLuu);
    }

    [Fact]
    public async Task Them_KhongCoQuyenQuanLy_ThatBaiTruocKhiMoTransaction()
    {
        var (service, repository, uow) = TaoService(TrangThaiDatSan.DangXuLy, coQuyen: false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ThemAsync(1, 7, 1, Convert.ToBase64String(RowVersion), default));

        Assert.Equal(0, repository.SoLanKhoaSan);
        Assert.Equal(0, uow.SoLanLuu);
    }

    [Fact]
    public async Task LayTheoDon_DangChoXuLy_KhongChoSua()
    {
        var (service, _, _) = TaoService(TrangThaiDatSan.ChoXuLy);

        var result = await service.LayTheoDonAsync(1, default);

        Assert.NotNull(result);
        Assert.False(result.ChoPhepSua);
    }

    private static (DichVuTrongDonService Service, DichVuTrongDonRepositoryFake Repository,
        DonViCongViecFake UnitOfWork) TaoService(TrangThaiDatSan trangThai, bool coQuyen = true)
    {
        var repository = new DichVuTrongDonRepositoryFake
        {
            CoQuyen = coQuyen,
            Don = new DatSan
            {
                MaDatSan = 1,
                MaSan = 3,
                TrangThai = trangThai,
                RowVersion = [.. RowVersion],
                ChiTietDatSans =
                [
                    new ChiTietDatSan
                    {
                        MaChiTiet = 11,
                        MaDatSan = 1,
                        MaDichVu = 7,
                        SoLuong = 2,
                        DonGia = 15000m,
                        DichVu = new DichVu
                        {
                            MaDichVu = 7,
                            TenDichVu = "Nước",
                            DonViTinh = "chai",
                            DonGia = 15000m,
                            TrangThai = TrangThaiDichVu.HoatDong
                        }
                    }
                ]
            },
            DichVu = new DichVu
            {
                MaDichVu = 7,
                TenDichVu = "Nước",
                DonViTinh = "chai",
                DonGia = 15000m,
                TrangThai = TrangThaiDichVu.HoatDong
            }
        };
        var uow = new DonViCongViecFake { Don = repository.Don };
        return (new DichVuTrongDonService(repository, uow, new TinhTienService(),
            new TaiKhoanHienTaiFake(coQuyen ? VaiTro.Admin : VaiTro.KhachHang)), repository, uow);
    }

    private sealed class TaiKhoanHienTaiFake(VaiTro vaiTro) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => DichVuTrongDonServiceTests.MaTaiKhoan;
        public string? HoTen => "Test";
        public VaiTro? VaiTro => vaiTro;
    }

    private sealed class DichVuTrongDonRepositoryFake : IDichVuTrongDonRepository
    {
        public required DatSan Don { get; init; }
        public required DichVu DichVu { get; init; }
        public bool CoQuyen { get; init; }
        public int SoLanKhoaSan { get; private set; }
        public List<DongDichVuTrongDon> LichSuHuy { get; } = [];

        public Task<TrangThaiDatSan?> LayTrangThaiAsync(int maDatSan, CancellationToken cancellationToken) =>
            Task.FromResult<TrangThaiDatSan?>(Don.MaDatSan == maDatSan ? Don.TrangThai : null);

        public Task<List<DongDichVuTrongDon>> LayDongAsync(int maDatSan, CancellationToken cancellationToken) =>
            Task.FromResult(Don.ChiTietDatSans.Select(x => new DongDichVuTrongDon(x.MaChiTiet, x.MaDichVu,
                x.DichVu.TenDichVu, x.DichVu.DonViTinh, x.SoLuong, x.DonGia, false, null, null)).ToList());

        public Task<List<DichVuDoc>> LayDichVuHoatDongAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new List<DichVuDoc>());

        public Task<DichVu?> LayDichVuAsync(int maDichVu, CancellationToken cancellationToken) =>
            Task.FromResult<DichVu?>(DichVu.MaDichVu == maDichVu ? DichVu : null);

        public Task<bool> CoQuyenQuanLyAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
            Task.FromResult(CoQuyen && maTaiKhoan == MaTaiKhoan);

        public Task<byte[]?> LayRowVersionAsync(int maDatSan, CancellationToken cancellationToken) =>
            Task.FromResult<byte[]?>(Don.MaDatSan == maDatSan ? Don.RowVersion : null);

        public Task<int?> LayMaSanAsync(int maDatSan, CancellationToken cancellationToken) =>
            Task.FromResult<int?>(Don.MaDatSan == maDatSan ? Don.MaSan : null);

        public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken)
        {
            SoLanKhoaSan++;
            return Task.CompletedTask;
        }

        public Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, CancellationToken cancellationToken) =>
            Task.FromResult<DatSan?>(Don.MaDatSan == maDatSan ? Don : null);

        public Task<List<DongDichVuTrongDon>> LayLichSuHuyAsync(int maDatSan, CancellationToken cancellationToken) =>
            Task.FromResult(LichSuHuy.ToList());

        public Task GhiLichSuHuyVaXoaDongAsync(ChiTietDatSan dong, int maTaiKhoan,
            CancellationToken cancellationToken)
        {
            LichSuHuy.Add(new DongDichVuTrongDon(dong.MaChiTiet, dong.MaDichVu, dong.DichVu.TenDichVu,
                dong.DichVu.DonViTinh, dong.SoLuong, dong.DonGia, true, DateTime.UtcNow, maTaiKhoan));
            Don.ChiTietDatSans.Remove(dong);
            return Task.CompletedTask;
        }

        public IEnumerable<DongDichVu> LayDongTinhTien(DatSan don) =>
            don.ChiTietDatSans.Select(x => new DongDichVu(x.SoLuong, x.DonGia)).ToArray();

        public void DanhDauCapNhatTienDichVu(DatSan don) { }

        public void DatRowVersionGoc(DatSan don, byte[] phienBan) { }
    }

    private sealed class DonViCongViecFake : IDonViCongViec
    {
        public int SoLanLuu { get; private set; }
        public required DatSan Don { get; init; }

        public Task LuuAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public async Task<T> ThucHienTrongGiaoDichAsync<T>(
            Func<CancellationToken, Task<QuyetDinhLuu<T>>> congViec,
            IsolationLevel mucCoLap, CancellationToken cancellationToken)
        {
            var decision = await congViec(cancellationToken);
            if (decision.DuocLuu)
            {
                SoLanLuu++;
                Don.RowVersion[^1]++;
            }
            return decision.KetQua;
        }
    }
}

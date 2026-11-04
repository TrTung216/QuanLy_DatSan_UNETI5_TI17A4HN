// M3: Nguyễn Văn Quý; MSSV: 23103100181. Kiểm thử trang cọc của khách: ánh xạ DTO, mã lỗi và nhãn hiển thị.
// Lưu ý: IDatSanService ở đây theo hợp đồng M4 đã có trên main (LayThongTinCocAsync...).
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class DonDatCocControllerTests
{
    private sealed class TaiKhoanGia(int? ma) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => ma;
        public string? HoTen => null;
        public QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.KhachHang;
    }

    private sealed class DongGia : IDongHo
    {
        public DateTime BayGio => new(2026, 11, 4, 9, 0, 0);
        public DateOnly HomNay => new(2026, 11, 4);
    }

    private sealed class DichVuGia(KetQuaCoc<ThongTinCocDto> ketQua) : IDatSanService
    {
        public int SoLanGoiCoc { get; private set; }
        public Task<KetQuaCoc<ThongTinCocDto>> LayThongTinCocAsync(int maDatSan, CancellationToken ct)
        { SoLanGoiCoc++; return Task.FromResult(ketQua); }
        public Task<KetQuaCoc<DoiSoatCocDto>> LayDoiSoatCocAsync(int maDatSan, CancellationToken ct) => throw new NotSupportedException();
        public Task<KetQuaCoc<GiaoDichCocDto>> TaoGiaoDichMoPhongAsync(TaoGiaoDichMoPhongYeuCau yc, CancellationToken ct) => throw new NotSupportedException();
        public Task<KetQuaCoc<XacNhanCocDto>> XacNhanCocVaDatSanAsync(XacNhanCocYeuCau yc, CancellationToken ct) => throw new NotSupportedException();
        public Task<KetQuaKhaDungSan> KiemTraKhaDungAsync(IReadOnlyCollection<int> maSans, KhoangGioTraCuuSan khoangGio, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaTaoDon> TaoDonAsync(TaoDonYeuCau yeuCau, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaXuLyDon> XacNhanDonAsync(XuLyDonYeuCau yeuCau, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaXuLyDon> TuChoiDonAsync(HuyDonYeuCau yeuCau, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaXuLyDon> KhachHangHuyDonAsync(HuyDonYeuCau yeuCau, int maTaiKhoan, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaXuLyDon> NhanVienHuyDonAsync(HuyDonYeuCau yeuCau, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaXuLyDon> GhiCocAsync(GhiCocYeuCau yeuCau, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<KetQuaXuLyDon> HoanThanhDonAsync(HoanThanhDonYeuCau yeuCau, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai, int trang, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<SanChonDto>> LayLuaChonSanAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DanhSachDonQuanLyDto> LayDanhSachQuanLyAsync(LocDonQuanLy loc, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ChiTietDonQuanLyDto?> LayChiTietQuanLyAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static ThongTinCocDto Thong(TrangThaiCocHienThi trangThai, bool coTheChuyen = false,
        decimal cocDaNhan = 0m, BienNhanCocDto? bienNhan = null) =>
        new(7, TrangThaiDatSan.ChoXuLy, 1, 300_000m, 150_000m, cocDaNhan, "COC 7", trangThai, true,
            coTheChuyen, coTheChuyen ? "DEMO:ORDER:7" : null, bienNhan);

    private static DonDatController TaoController(DichVuGia dichVu, int? maTaiKhoan = 5) =>
        new(new TaiKhoanGia(maTaiKhoan), dichVu, new DongGia())
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider())
        };

    private sealed class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    [Fact]
    public async Task ChuaDangNhapThiThachThuc()
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.ChuaNhan)));
        var kq = await TaoController(dv, maTaiKhoan: null).Coc(7, default);
        Assert.IsType<ChallengeResult>(kq);
        Assert.Equal(0, dv.SoLanGoiCoc);
    }

    [Fact]
    public async Task KhongDuQuyenTraForbidVaKhongTonTaiTraNotFound()
    {
        var forbid = await TaoController(new DichVuGia(KetQuaCoc<ThongTinCocDto>.ThatBai(MaLoiCoc.KhongDuQuyen))).Coc(7, default);
        Assert.IsType<ForbidResult>(forbid);
        var notFound = await TaoController(new DichVuGia(KetQuaCoc<ThongTinCocDto>.ThatBai(MaLoiCoc.KhongTonTai))).Coc(7, default);
        Assert.IsType<NotFoundResult>(notFound);
    }

    [Fact]
    public async Task LoiKhacChuyenVeChiTietKemThongBaoKhongLoChiTietNoiBo()
    {
        var controller = TaoController(new DichVuGia(KetQuaCoc<ThongTinCocDto>.ThatBai(MaLoiCoc.DuLieuTienKhongHopLe)));
        var kq = await controller.Coc(7, default);
        var redirect = Assert.IsType<RedirectToActionResult>(kq);
        Assert.Equal(nameof(DonDatController.ChiTiet), redirect.ActionName);
        Assert.Equal(7, redirect.RouteValues!["id"]);
        Assert.Equal(HienThiCoc.ThongBaoLoi(MaLoiCoc.DuLieuTienKhongHopLe), controller.TempData["Error"]);
    }

    [Fact]
    public async Task ChuaNhanCoTheChuyenHienCocCanChuyenVaConPhaiThanhToanTamTinh()
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.ChuaNhan, coTheChuyen: true)));
        var view = Assert.IsType<ViewResult>(await TaoController(dv).Coc(7, default));
        var model = Assert.IsType<CocDonViewModel>(view.Model);
        Assert.Equal(150_000m, model.TienCocYeuCau);
        Assert.Equal(0m, model.TienCocDaNhan);
        Assert.Equal(300_000m, model.ConPhaiThanhToanTamTinh);
        Assert.True(model.ConPhaiThanhToanLaTamTinh);
        Assert.Equal("DEMO:ORDER:7", model.NoiDungQrMoPhong);
        Assert.Null(model.BienNhan);
    }

    [Fact]
    public async Task DaNhanCocGiamConPhaiThanhToanVaKhongAm()
    {
        var bn = new BienNhanCocDto(1, 7, 150_000m, "TEST-TXN-0001", "MoPhong", new DateTime(2026, 11, 4, 8, 0, 0), new DateTime(2026, 11, 4, 8, 30, 0));
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.DaNhan, cocDaNhan: 150_000m, bienNhan: bn)));
        var model = (CocDonViewModel)((ViewResult)await TaoController(dv).Coc(7, default)).Model!;
        Assert.Equal(150_000m, model.ConPhaiThanhToanTamTinh);
        Assert.Equal(150_000m, model.BienNhan!.SoTien);

        var thua = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.DaNhan, cocDaNhan: 400_000m)));
        var modelThua = (CocDonViewModel)((ViewResult)await TaoController(thua).Coc(7, default)).Model!;
        Assert.Equal(0m, modelThua.ConPhaiThanhToanTamTinh);
    }

    [Theory]
    [InlineData(TrangThaiCocHienThi.ChuaNhan)]
    [InlineData(TrangThaiCocHienThi.ChoDoiSoat)]
    [InlineData(TrangThaiCocHienThi.CanXuLyNgoaiLe)]
    [InlineData(TrangThaiCocHienThi.KhongSanSang)]
    [InlineData(TrangThaiCocHienThi.KhongApDung)]
    public void ChiTrangThaiDaNhanMoiHienDaNhanCoc(TrangThaiCocHienThi trangThai)
    {
        Assert.DoesNotContain("Đã nhận cọc", HienThiCoc.TenTrangThai(trangThai));
        Assert.Equal("Đã nhận cọc", HienThiCoc.TenTrangThai(TrangThaiCocHienThi.DaNhan));
    }

    [Fact]
    public void MoiMaLoiCocDeuCoThongBaoTiengVietKhongRong()
    {
        foreach (var ma in Enum.GetValues<MaLoiCoc>())
            Assert.False(string.IsNullOrWhiteSpace(HienThiCoc.ThongBaoLoi(ma)), ma.ToString());
    }
}

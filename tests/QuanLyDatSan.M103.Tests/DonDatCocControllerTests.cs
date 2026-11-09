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

    private sealed class DichVuGia(KetQuaCoc<ThongTinCocDto> ketQua, ChiTietDonDto? chiTiet = null) : IDatSanService
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
        public Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken) => Task.FromResult(chiTiet);
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
    public async Task TrangCocChiTruyenCacKhoanServiceDaCungCapVaKhongTuTinhConLai()
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.ChuaNhan, coTheChuyen: true)));
        var view = Assert.IsType<ViewResult>(await TaoController(dv).Coc(7, default));
        var model = Assert.IsType<CocDonViewModel>(view.Model);
        Assert.Equal(300_000m, model.TienSanDuKien);
        Assert.Equal(150_000m, model.TienCocYeuCau);
        Assert.Equal(0m, model.TienCocDaNhan);
        Assert.Equal("DEMO:ORDER:7", model.NoiDungQrMoPhong);
        Assert.Null(model.BienNhan);
        Assert.Null(model.NhanPhanConLai);
        // Hồi quy: controller/ViewModel không được có trường tiền "còn lại" tự tính.
        Assert.DoesNotContain(typeof(CocDonViewModel).GetProperties(), x => x.Name.StartsWith("ConPhai"));
        Assert.DoesNotContain(typeof(ChiTietDonViewModel).GetProperties(), x => x.Name.StartsWith("ConPhai"));
    }

    [Fact]
    public async Task CocVuotTongKhongBiCheGiauVaKhongCoSoTienConLai()
    {
        // Cọc đã ghi nhận lớn hơn tiền sân: chỉ chuyển nguyên giá trị service, không Math.Max về 0.
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.DaNhan, cocDaNhan: 400_000m)));
        var model = (CocDonViewModel)((ViewResult)await TaoController(dv).Coc(7, default)).Model!;
        Assert.Equal(400_000m, model.TienCocDaNhan);
        Assert.Equal(300_000m, model.TienSanDuKien);
    }

    private static ChiTietDonDto Don(TrangThaiDatSan trangThai, decimal tienCoc = 0m, decimal? tienSan = null,
        decimal? tienDichVu = null, decimal? tong = null, int gioToi = 3) => new()
    {
        MaDatSan = 7, TenSan = "Sân A", TenLoai = "Bóng đá", DiaChi = "Hà Nội",
        NgayDat = new DateTime(2026, 11, 3), GioBatDau = new DateTime(2026, 11, 4, 9 + gioToi, 0, 0),
        GioKetThuc = new DateTime(2026, 11, 4, 10 + gioToi, 0, 0), DonGia = 200_000m, TienCoc = tienCoc,
        TrangThai = trangThai, TienSan = tienSan, TienDichVu = tienDichVu, TongTien = tong,
        TienSanDuKien = 200_000m, PhienBan = new byte[8]
    };

    [Fact]
    public async Task DonHoanThanhHienDaThuPhanConLaiVaGiuCacKhoanDaChot()
    {
        // Tiền sân 200.000, dịch vụ 60.000, cọc 100.000: controller không được suy ra 160.000 hay 100.000.
        var don = Don(TrangThaiDatSan.HoanThanh, tienCoc: 100_000m, tienSan: 200_000m, tienDichVu: 60_000m, tong: 260_000m);
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(
            Thong(TrangThaiCocHienThi.DaNhan, cocDaNhan: 100_000m) with { TrangThai = TrangThaiDatSan.HoanThanh }), don);

        var view = Assert.IsType<ViewResult>(await TaoController(dv).ChiTiet(7, default));
        var model = Assert.IsType<ChiTietDonViewModel>(view.Model);

        Assert.Equal("Đã thu phần còn lại", model.NhanPhanConLai);
        Assert.Equal("Đã thu phần còn lại", model.Coc!.NhanPhanConLai);
        Assert.Equal(200_000m, model.TienSan);
        Assert.Equal(60_000m, model.TienDichVu);
        Assert.Equal(260_000m, model.TongTien);
        Assert.Equal(100_000m, model.Coc.TienCocDaNhan);
        Assert.False(model.CoTheHuy);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.ChoXuLy)]
    [InlineData(TrangThaiDatSan.DangXuLy)]
    public async Task DonChuaHoanThanhKhongCoNhanPhanConLai(TrangThaiDatSan trangThai)
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.ChuaNhan, coTheChuyen: true)),
            Don(trangThai));
        var model = (ChiTietDonViewModel)((ViewResult)await TaoController(dv).ChiTiet(7, default)).Model!;
        Assert.Null(model.NhanPhanConLai);
        Assert.Null(model.TongTien);
    }

    [Theory]
    [InlineData(TrangThaiCocHienThi.ChoDoiSoat)]
    [InlineData(TrangThaiCocHienThi.CanXuLyNgoaiLe)]
    [InlineData(TrangThaiCocHienThi.DaNhan)]
    public async Task GiaoDichChoDoiSoatHoacNgoaiLeAnNutHuyVaGiaiThich(TrangThaiCocHienThi trangThaiCoc)
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(trangThaiCoc)), Don(TrangThaiDatSan.ChoXuLy));
        var model = (ChiTietDonViewModel)((ViewResult)await TaoController(dv).ChiTiet(7, default)).Model!;
        Assert.False(model.CoTheHuy);
        Assert.False(string.IsNullOrWhiteSpace(model.LyDoKhongTheHuy));
        Assert.Contains("nhân viên", model.LyDoKhongTheHuy);
    }

    [Theory]
    [InlineData(TrangThaiCocHienThi.ChuaNhan)]
    [InlineData(TrangThaiCocHienThi.KhongApDung)]
    public async Task DonChoChuaCoGiaoDichChoPhepHuy(TrangThaiCocHienThi trangThaiCoc)
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(trangThaiCoc)), Don(TrangThaiDatSan.ChoXuLy));
        var model = (ChiTietDonViewModel)((ViewResult)await TaoController(dv).ChiTiet(7, default)).Model!;
        Assert.True(model.CoTheHuy);
        Assert.Null(model.LyDoKhongTheHuy);
    }

    [Fact]
    public async Task DaQuaGioBatDauKhongHienNutVaKhongHienLyDoHuy()
    {
        var dv = new DichVuGia(KetQuaCoc<ThongTinCocDto>.DaXong(Thong(TrangThaiCocHienThi.ChuaNhan)),
            Don(TrangThaiDatSan.ChoXuLy, gioToi: -1));
        var model = (ChiTietDonViewModel)((ViewResult)await TaoController(dv).ChiTiet(7, default)).Model!;
        Assert.False(model.CoTheHuy);
        Assert.Null(model.LyDoKhongTheHuy);
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

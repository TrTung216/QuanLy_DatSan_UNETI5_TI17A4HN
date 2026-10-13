// Phan Giang Tâm - 23103100196. M2: Ánh xạ mã lỗi và kiểm tra lỗi chung ngoài Development.
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuanLyDatSan.M103.Tests;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

public class PhanHoiSanTests
{
    private static SuaSanViewModel Form() => new()
    {
        MaSan = 1, TenSan = "Sân nhập lại", DiaChi = "Hà Nội", MaLoaiSan = 1,
        DonGia = 200000, DonGiaCaoDiem = 350000, DonGiaCuoiTuan = 450000,
        TrangThai = TrangThaiSan.HoatDong, GioMoCua = new(6, 0), GioDongCua = new(22, 0)
    };

    private static SanTheThaoController Controller(ServiceGia service) => new(service)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        TempData = new TempDataDictionary(new DefaultHttpContext(), new TempDataGia())
    };

    [Theory]
    [InlineData(MaLoiSan.LoaiSanKhongHopLe, false, "MaLoaiSan", "Vui lòng chọn loại sân đang hoạt động.")]
    [InlineData(MaLoiSan.LoaiSanKhongHopLe, true, "MaLoaiSan", "Loại sân không hợp lệ hoặc đã ngừng hoạt động.")]
    [InlineData(MaLoiSan.DuLieuKhongHopLe, false, "", "Thông tin sân không hợp lệ.")]
    [InlineData(MaLoiSan.DuLieuKhongHopLe, true, "", "Thông tin sân không hợp lệ.")]
    [InlineData(MaLoiSan.DuLieuLienQuanThayDoi, false, "", "Dữ liệu không còn hợp lệ. Vui lòng kiểm tra loại sân, giá và giờ hoạt động rồi thử lại.")]
    [InlineData(MaLoiSan.DuLieuLienQuanThayDoi, true, "", "Không lưu được vì dữ liệu liên quan đã thay đổi. Vui lòng kiểm tra lại thông tin sân.")]
    public async Task LoiGhiAnhXaDungTruongVaGiuForm(MaLoiSan ma, bool sua, string truong, string thongBao)
    {
        var service = new ServiceGia { KetQuaGhi = KetQuaSan<int>.Loi(ma) };
        var controller = Controller(service);
        var form = Form();
        var result = sua ? await controller.Edit(1, form, default) : await controller.Create(form, default);
        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(form, view.Model);
        Assert.Contains(controller.ModelState[truong]!.Errors, x => x.ErrorMessage == thongBao);
        Assert.Single(form.CacLoaiSan);
        Assert.False(controller.TempData.ContainsKey("Success"));
    }

    [Theory]
    [InlineData(MaLoiSan.KhongTonTai)]
    [InlineData(MaLoiSan.DangDuocThamChieu)]
    [InlineData(MaLoiSan.DaBiXoa)]
    [InlineData(MaLoiSan.ThanhCong)]
    public async Task XoaAnhXaDayDuKetQua(MaLoiSan ma)
    {
        var controller = Controller(new ServiceGia
        {
            KetQuaGhi = ma == MaLoiSan.ThanhCong ? KetQuaSan<int>.Dat(1) : KetQuaSan<int>.Loi(ma)
        });
        var result = await controller.DeleteConfirmed(1, default);
        switch (ma)
        {
            case MaLoiSan.KhongTonTai: Assert.IsType<NotFoundResult>(result); break;
            case MaLoiSan.DangDuocThamChieu:
                var view = Assert.IsType<ViewResult>(result);
                Assert.Equal("Delete", view.ViewName);
                Assert.IsType<SanChiTietViewModel>(view.Model);
                Assert.Contains(controller.ModelState[string.Empty]!.Errors, x => x.ErrorMessage.Contains("Không thể xóa sân"));
                break;
            case MaLoiSan.DaBiXoa:
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
                Assert.Contains("một thao tác khác", Assert.IsType<string>(controller.TempData["Warning"]));
                break;
            case MaLoiSan.ThanhCong:
                Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
                Assert.Equal("Đã xóa sân thành công.", controller.TempData["Success"]);
                break;
        }
        if (ma != MaLoiSan.ThanhCong) Assert.False(controller.TempData.ContainsKey("Success"));
    }

    [Fact]
    public async Task SuaSanBiXoaTrongLucGuiFormTra404()
    {
        var controller = Controller(new ServiceGia { KetQuaGhi = KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai) });
        Assert.IsType<NotFoundResult>(await controller.Edit(1, Form(), default));
    }

    [Fact]
    public async Task MaLoiNgoaiHopDongKhongBiBaoThanhCong()
    {
        var controller = Controller(new ServiceGia { KetQuaGhi = KetQuaSan<int>.Loi(MaLoiSan.GioThueKhongHopLe) });
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Create(Form(), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Edit(1, Form(), default));
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.DeleteConfirmed(1, default));
    }

    [Theory]
    [InlineData(MaLoiSan.ThanhCong)]
    [InlineData(MaLoiSan.KhongTonTai)]
    [InlineData(MaLoiSan.GioThueKhongHopLe)]
    public async Task GiaAnhXaDayDuKetQua(MaLoiSan ma)
    {
        var controller = Controller(new ServiceGia
        {
            KetQuaGia = ma == MaLoiSan.ThanhCong ? KetQuaSan<decimal>.Dat(550000) : KetQuaSan<decimal>.Loi(ma)
        });
        var result = await controller.Details(1, default, new(2026, 10, 12), new(17, 0), new(19, 0));
        if (ma == MaLoiSan.KhongTonTai) Assert.IsType<NotFoundResult>(result);
        else Assert.IsType<ViewResult>(result);
        if (ma == MaLoiSan.ThanhCong) Assert.Equal(550000m, controller.ViewData["GiaDuKien"]);
        if (ma == MaLoiSan.GioThueKhongHopLe) Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task LoiBatNgoTra500TrangLoiVaLogTrongProduction()
    {
        const string chiTietNoiBo = "M2_LOI_THU_KHONG_HIEN_CHO_KHACH";
        using var app = new UngDungKiemThu();
        using var log = new LogThu();
        using var production = app.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureLogging(logging => logging.AddProvider(log));
            builder.ConfigureServices(services => services.AddScoped<ISanTheThaoService>(_ =>
                new ServiceGia { LoiDanhSach = new InvalidOperationException(chiTietNoiBo) }));
        });
        using var client = production.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost")
        });
        Assert.True(production.Services.GetRequiredService<IHostEnvironment>().IsProduction());
        var response = await client.GetAsync("/SanTheThao");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Có lỗi xảy ra", html);
        Assert.Matches("Mã yêu cầu:</strong>\\s*<code>[^<]+</code>", html);
        Assert.DoesNotContain(chiTietNoiBo, html);
        Assert.DoesNotContain("InvalidOperationException", html);
        Assert.Contains(log.Loi, x => x?.Message == chiTietNoiBo);
        // Cơ chế tạo lỗi chỉ tồn tại trong test, không đăng ký trong ứng dụng.
    }

    private sealed class ServiceGia : ISanTheThaoService
    {
        public KetQuaSan<int> KetQuaGhi { get; init; } = KetQuaSan<int>.Dat(1);
        public KetQuaSan<decimal> KetQuaGia { get; init; } = KetQuaSan<decimal>.Dat(550000);
        public Exception? LoiDanhSach { get; init; }
        public Task<TrangSanDoc> LayDanhSachAsync(int trang, CancellationToken token)
            => LoiDanhSach is null ? Task.FromResult(new TrangSanDoc([], 1, 1, 0)) : Task.FromException<TrangSanDoc>(LoiDanhSach);
        public Task<SanDoc?> LayChiTietAsync(int id, CancellationToken token)
            => Task.FromResult<SanDoc?>(new(1, "Sân cũ", 1, "Bóng đá", "Hà Nội", null, 200000, 350000, 450000,
                TrangThaiSan.HoatDong, new(6, 0), new(22, 0), null, null));
        public Task<IReadOnlyList<LoaiSanLuaChon>> LayLuaChonAsync(int? id, CancellationToken token)
            => Task.FromResult<IReadOnlyList<LoaiSanLuaChon>>([new(1, "Bóng đá", true, 200000, 350000, 450000)]);
        public Task<bool> KiemTraLoaiSanAsync(int? id, int? cu, CancellationToken token) => Task.FromResult(true);
        public Task<KetQuaSan<int>> TaoAsync(GhiSan x, CancellationToken token) => Task.FromResult(KetQuaGhi);
        public Task<KetQuaSan<int>> SuaAsync(int id, GhiSan x, CancellationToken token) => Task.FromResult(KetQuaGhi);
        public Task<KetQuaSan<int>> XoaAsync(int id, CancellationToken token) => Task.FromResult(KetQuaGhi);
        public Task<KetQuaSan<decimal>> TinhGiaAsync(int id, DateOnly ngay, TimeOnly dau, TimeOnly cuoi, CancellationToken token)
            => Task.FromResult(KetQuaGia);
    }

    private sealed class TempDataGia : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class LogThu : ILoggerProvider
    {
        public ConcurrentQueue<Exception?> Loi { get; } = new();
        public ILogger CreateLogger(string categoryName) => new NguoiGhi(Loi);
        public void Dispose() { }
        private sealed class NguoiGhi(ConcurrentQueue<Exception?> loi) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => level >= LogLevel.Error;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (level >= LogLevel.Error) loi.Enqueue(exception);
            }
        }
    }
}

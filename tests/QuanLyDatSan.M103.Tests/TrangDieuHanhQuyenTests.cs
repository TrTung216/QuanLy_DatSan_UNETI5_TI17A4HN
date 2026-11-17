// M1: Trần Trọng Tùng; MSSV 23103100202. Kiểm tra quyền thống kê và lỗi trang điều hành.
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyThongKe;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DieuHanh;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class TrangDieuHanhQuyenTests
{
    [Theory]
    [InlineData("Admin", 1)] [InlineData("NhanVien", 0)]
    public async Task ChiAdminDuocLayDoanhThu(string role, int calls)
    {
        var stub = new DuLieu();
        var view = Assert.IsType<ViewResult>(await Controller(stub, role).Index(default));
        var model = Assert.IsType<TrangDieuHanhViewModel>(view.Model);
        Assert.Equal(calls, stub.Calls);
        Assert.Equal(calls == 1, model.ThongKe is not null);
        Assert.Equal(3, model.SoSanHoatDong);
    }

    [Fact]
    public async Task LoiThongKeKhongGiaSoLieuVaKhongNuotHuyYeuCau()
    {
        var stub = new DuLieu { Error = new InvalidOperationException("test") };
        var view = Assert.IsType<ViewResult>(await Controller(stub, "Admin").Index(default));
        Assert.Null(Assert.IsType<TrangDieuHanhViewModel>(view.Model).ThongKe);
        stub.Error = new OperationCanceledException();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Controller(stub, "Admin").Index(default));
    }

    private static QuanTriController Controller(DuLieu stub, string role) => new(stub, stub, stub, NullLogger<QuanTriController>.Instance)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test")) } }
    };

    private sealed class DuLieu : IDieuHanhService, IThongKeService, IDongHo
    {
        public int Calls;
        public Exception? Error;
        public DateOnly HomNay => new(2026, 11, 16);
        public DateTime BayGio => HomNay.ToDateTime(TimeOnly.MinValue);
        public Task<SoLieuDieuHanh> LaySoLieuAsync(CancellationToken ct) => Task.FromResult(new SoLieuDieuHanh(1, 2, 3));
        public string? KiemTraBoLoc(BoLocThongKe filter) => null;
        public Task<KetQuaThongKe> LayAsync(BoLocThongKe filter, CancellationToken ct)
        {
            Calls++;
            if (Error is not null) throw Error;
            Assert.Equal(new DateOnly(2026, 11, 1), filter.TuNgay);
            Assert.Equal(HomNay, filter.DenNgay);
            return Task.FromResult(new KetQuaThongKe(filter, 0, 0, 0, 0, [], [], [], []));
        }
    }
}

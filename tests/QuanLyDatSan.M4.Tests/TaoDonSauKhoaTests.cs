// M3/M4: kiểm thử hồi quy sau merge main; Codex hỗ trợ kiểm tra lại điều kiện sau khi khóa sân.
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class TaoDonSauKhoaTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    [Theory]
    [InlineData("tai-khoan", MaLoiDatSan.HoSoKhongHoatDong)]
    [InlineData("ho-so", MaLoiDatSan.HoSoKhongHoatDong)]
    [InlineData("vai-tro", MaLoiDatSan.HoSoKhongHoatDong)]
    [InlineData("qua-gio", MaLoiDatSan.BatDauKhongTuongLai)]
    public async Task DocLaiDieuKienSauKhoaKhongDungDuLieuCu(string scenario, MaLoiDatSan expected)
    {
        var order = await f.Booking(TrangThaiDatSan.DaHuy);
        var clock = new DongHoThayDoi { BayGio = f.Clock.BayGio };
        var interceptor = new SauKhoa(async () =>
        {
            if (scenario == "qua-gio")
            {
                clock.BayGio = order.GioBatDau;
                return;
            }
            await using var changed = f.Db();
            var account = await changed.TaiKhoans.FindAsync(f.CustomerAccountId);
            var customer = await changed.KhachHangs.FindAsync(f.CustomerId);
            if (scenario == "tai-khoan") account!.TrangThai = TrangThaiTaiKhoan.BiKhoa;
            if (scenario == "vai-tro") account!.VaiTro = VaiTro.NhanVien;
            if (scenario == "ho-so") customer!.TrangThai = TrangThaiKhachHang.BiKhoa;
            await changed.SaveChangesAsync();
        });
        try
        {
            await using var db = f.Db(interceptor);
            // Cố tình có dữ liệu đã track trước khi điều kiện thay đổi.
            await db.KhachHangs.Include(x => x.TaiKhoan).SingleAsync(x => x.MaKhachHang == f.CustomerId);
            var service = new DatSanService(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan.DatSanRepository(db), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(db), clock, new QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien.TinhTienService());
            var result = await service.TaoDonAsync(new(f.CustomerAccountId, order.MaSan,
                DateOnly.FromDateTime(order.GioBatDau), TimeOnly.FromDateTime(order.GioBatDau),
                TimeOnly.FromDateTime(order.GioKetThuc)), default);
            Assert.Equal(expected, result.Loi?.Ma);
            Assert.False(result.ThanhCong);
            await using var check = f.Db();
            Assert.False(await check.DatSans.AnyAsync(x => x.MaSan == order.MaSan && x.TrangThai == TrangThaiDatSan.ChoXuLy));
        }
        finally
        {
            await using var reset = f.Db();
            var account = await reset.TaiKhoans.FindAsync(f.CustomerAccountId);
            var customer = await reset.KhachHangs.FindAsync(f.CustomerId);
            account!.TrangThai = TrangThaiTaiKhoan.HoatDong;
            account.VaiTro = VaiTro.KhachHang;
            customer!.TrangThai = TrangThaiKhachHang.HoatDong;
            await reset.SaveChangesAsync();
        }
    }

    private sealed class DongHoThayDoi : IDongHo
    {
        public DateTime BayGio { get; set; }
        public DateOnly HomNay => DateOnly.FromDateTime(BayGio);
    }

    private sealed class SauKhoa(Func<Task> change) : DbCommandInterceptor
    {
        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command,
            CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK", StringComparison.Ordinal))
                await change();
            return result;
        }
    }
}

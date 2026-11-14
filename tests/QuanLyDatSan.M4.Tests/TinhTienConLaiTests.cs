// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm tra công thức cọc/còn phải thu chung theo hợp đồng M3/M4/M5.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class TinhTienConLaiTests
{
    private readonly ITinhTienService service = new TinhTienService();

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(101, 51)]
    [InlineData(300000, 150000)]
    public void CocLaNamMuoiPhanTramTienSanLamTronDenDong(int tienSan, int coc) =>
        Assert.Equal(coc, service.TinhTienCocYeuCau(tienSan));

    [Theory]
    [InlineData(360000, 150000, 210000)]
    [InlineData(101, 51, 50)]
    [InlineData(100, 0, 100)]
    [InlineData(100, 100, 0)]
    [InlineData(0, 0, 0)]
    public void ConLaiLaTongDaGomDichVuTruCoc(int tong, int coc, int conLai) =>
        Assert.Equal(conLai, service.TinhConPhaiThu(tong, coc));

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(100, -1)]
    [InlineData(100, 101)]
    public void TienSaiKhongDuocEpVeKhong(int tong, int coc) =>
        Assert.ThrowsAny<ArgumentException>(() => service.TinhConPhaiThu(tong, coc));

    [Fact]
    public void ChanTienAmVaVuotGioiHanLuuTru()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhTienCocYeuCau(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhTienCocYeuCau(10000000000000000m));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhConPhaiThu(10000000000000000m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhConPhaiThu(0m, decimal.MaxValue));
        Assert.Equal(9999999999999999.99m, service.TinhConPhaiThu(9999999999999999.99m, 0m));
    }
}

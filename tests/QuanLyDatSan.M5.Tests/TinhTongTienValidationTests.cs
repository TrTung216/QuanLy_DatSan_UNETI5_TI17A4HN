using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan.M5.Tests;

public class TinhTongTienValidationTests
{
    private const decimal TienToiDa = 9999999999999999.99m;
    private readonly TinhTienService service = new();

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void TinhTongTien_TienAm_NemLoi(decimal tienSan, decimal tienDichVu)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhTongTien(tienSan, tienDichVu));
    }

    [Fact]
    public void TinhTongTien_TongVuotDecimal18_2_NemLoi()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhTongTien(TienToiDa, 0.01m));
    }

    [Fact]
    public void TinhTongTien_TongBangGioiHanDecimal18_2_DuocChapNhan()
    {
        Assert.Equal(TienToiDa, service.TinhTongTien(TienToiDa, 0m));
    }

    [Fact]
    public void TinhTongTien_KhoanTienVuotGioiHan_NemLoi()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhTongTien(TienToiDa + 0.01m, 0m));
    }

    [Fact]
    public void TinhTongTien_QuaHaiChuSoThapPhan_NemLoi()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => service.TinhTongTien(1.001m, 0m));
    }
}

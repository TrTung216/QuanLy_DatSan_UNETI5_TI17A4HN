//M5: Nguyen HuuQuang MSSV: 23103100184 
//    Test các test case tính tiền

using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M5.Tests;

public class TinhTienDichVuTests
{
    private readonly TinhTienService _service = new();

    [Fact]
    public void TinhTienDichVu_HaiDong_Dung60000()
    {
        var cacDong = new[]
        {
            new DongDichVu(2, 15_000m),
            new DongDichVu(1, 30_000m)
        };

        Assert.Equal(60_000m, _service.TinhTienDichVu(cacDong));
    }

    [Fact]
    public void TinhTienDichVu_KhongCoDong_Bang0()
    {
        Assert.Equal(0m, _service.TinhTienDichVu(Array.Empty<DongDichVu>()));
    }

    [Fact]
    public void TinhTienDichVu_SoLuongBang0_NemLoi()
    {
        var cacDong = new[] { new DongDichVu(0, 15_000m) };

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.TinhTienDichVu(cacDong));
    }

    [Fact]
    public void TinhTienDichVu_DonGiaAm_NemLoi()
    {
        var cacDong = new[] { new DongDichVu(1, -1m) };

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.TinhTienDichVu(cacDong));
    }

    [Fact]
    public void TinhTongTien_TienSanCongDichVu_Dung360000()
    {
        Assert.Equal(360_000m, _service.TinhTongTien(300_000m, 60_000m));
    }
}
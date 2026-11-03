// M1: Trần Trọng Tùng; MSSV: 23103100202. Kiểm tra DI cấu hình cọc và guard môi trường, không tạo endpoint mô phỏng.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Extensions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class CocCauHinhTests(SqlFixture f) : IClassFixture<SqlFixture>
{
    [Theory]
    [InlineData(null, "Development", false)]
    [InlineData("true", "Development", true)]
    [InlineData("false", "Development", false)]
    [InlineData("true", "Production", false)]
    public async Task BindCauHinhQuaDiVaKhongChoMoPhongNgoaiDevelopment(string? bat, string environment, bool choPhep)
    {
        var values = new Dictionary<string, string?>
        {
            ["CocMoPhong:TaiKhoanNhan"] = "DEMO-DI-TEST"
        };
        if (bat is not null) values["CocMoPhong:Bat"] = bat;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new EnvironmentTest { EnvironmentName = environment });
        services.AddScoped(_ => f.Db());
        services.AddSingleton<IDongHo>(f.Clock);
        services.AddScoped<ITaiKhoanHienTai>(_ => new ActorTest(f.AdminAccountId));
        services.AddUngDungServices();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<CocMoPhongOptions>>().Value;
        Assert.Equal("DEMO-DI-TEST", options.TaiKhoanNhan);
        var svc = scope.ServiceProvider.GetRequiredService<IDatSanService>();
        var result = await svc.TaoGiaoDichMoPhongAsync(new(Guid.NewGuid().ToString("N"), "COC 99999999", 1m, f.Clock.BayGio), default);
        Assert.Equal(choPhep, result.ThanhCong);
        if (choPhep) Assert.Equal("DEMO-DI-TEST", result.DuLieu!.TaiKhoanNhan);
        else Assert.Equal(MaLoiCoc.MoPhongKhongDuocPhep, result.Loi);
    }
}

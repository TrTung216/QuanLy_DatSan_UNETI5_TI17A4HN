using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddUngDungServices(this IServiceCollection services)
    {
        services.AddModuleDichVu();
        // Module khác tự thêm dòng của mình ở đây
        return services;
    }

    // M5: dịch vụ
    private static IServiceCollection AddModuleDichVu(this IServiceCollection services)
    {
        services.AddScoped<IDichVuService, DichVuService>();
        return services;
    }
}
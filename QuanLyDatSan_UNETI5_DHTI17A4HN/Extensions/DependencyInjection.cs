using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ gom DI M1; giữ đăng ký M5 hiện có.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. thêm đăng ký service hồ sơ khách hàng.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddUngDungServices(this IServiceCollection services)
    {
        services.AddModuleTaiKhoanVaLoaiSan();
        services.AddModuleDichVu();
        services.AddModuleKhachHang();
        // Module khác tự thêm dòng của mình ở đây
        return services;
    }

    // M1: nghiệp vụ tài khoản, xác thực và loại sân; hạ tầng cookie/Session giữ trong Program.cs.
    private static IServiceCollection AddModuleTaiKhoanVaLoaiSan(this IServiceCollection services)
    {
        services.AddScoped<ILoaiSanService, LoaiSanService>();
        services.AddScoped<IQuanLyTaiKhoanService, QuanLyTaiKhoanService>();
        services.AddScoped<IXacThucTaiKhoanService, XacThucTaiKhoanService>();
        return services;
    }

    // M5: dịch vụ
    private static IServiceCollection AddModuleDichVu(this IServiceCollection services)
    {
        services.AddScoped<IDichVuService, DichVuService>();
        return services;
    }

    // M3: hồ sơ khách hàng
    private static IServiceCollection AddModuleKhachHang(this IServiceCollection services)
    {
        services.AddScoped<IKhachHangService, KhachHangService>();
        return services;
    }
}
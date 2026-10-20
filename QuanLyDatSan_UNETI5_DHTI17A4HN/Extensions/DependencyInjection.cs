using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ gom DI M1; giữ đăng ký M5 hiện có.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. thêm đăng ký service hồ sơ khách hàng.
//M5 :Nguyễn Hữu Quang MSSV : 23103100184 . Thêm đăng kí dịch vụ tính tiền
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddUngDungServices(this IServiceCollection services)
    {
        services.AddModuleTaiKhoanVaLoaiSan();
        services.AddScoped<ITrangChuService, TrangChuService>();
        services.AddModuleDichVu();
        services.AddModuleSan();
        services.AddModuleKhachHang();
        // trong AddUngDungServices(), dưới services.AddModuleDichVu();
        services.AddModuleTinhTien();
        services.AddModuleDichVuTrongDon();
        // Module khác tự thêm dòng của mình ở đây
        services.AddModuleDatSan();
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

    // M2: Phan Giang Tâm - 23103100196. Đăng ký service quản lý sân.
    private static IServiceCollection AddModuleSan(this IServiceCollection services)
    {
        services.AddScoped<ISanTheThaoService, SanTheThaoService>();
        return services;
    }
    
    // M3: hồ sơ khách hàng
    private static IServiceCollection AddModuleKhachHang(this IServiceCollection services)
    {
        services.AddScoped<IKhachHangService, KhachHangService>();
        return services;
    }

    // M4: Phạm Thành Nghĩa; MSSV: 23103100308. Đăng ký service đặt sân.
    private static IServiceCollection AddModuleDatSan(this IServiceCollection services)
    {
        services.AddScoped<IDatSanService, DatSanService>();
        return services;
    }
    // M5: tính tiền
    private static IServiceCollection AddModuleTinhTien(this IServiceCollection services)
    {
        services.AddScoped<ITinhTienService, TinhTienService>();
        return services;
    }
    // M5: dịch vụ trong đơn
    private static IServiceCollection AddModuleDichVuTrongDon(this IServiceCollection services)
    {
        services.AddScoped<IDichVuTrongDonService, DichVuTrongDonService>();
        return services;
    }
}

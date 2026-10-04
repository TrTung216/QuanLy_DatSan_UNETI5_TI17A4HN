// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tạo Admin đầu tiên qua CLI.
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public static class KhoiTaoAdmin
{
    public static async Task<int> ChayAsync(IServiceProvider services, IConfiguration configuration)
    {
        var ten = configuration["AdminKhoiTao:TenDangNhap"]?.Trim();
        var hoTen = configuration["AdminKhoiTao:HoTen"]?.Trim();
        var email = configuration["AdminKhoiTao:Email"]?.Trim();
        var matKhau = configuration["AdminKhoiTao:MatKhau"];
        if (string.IsNullOrWhiteSpace(ten) || ten.Length > 50
            || string.IsNullOrWhiteSpace(hoTen) || hoTen.Length > 100
            || string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new EmailAddressAttribute().IsValid(email)
            || string.IsNullOrWhiteSpace(matKhau) || matKhau.Length is < 12 or > 128)
        {
            Console.Error.WriteLine("Cần cấu hình AdminKhoiTao: TenDangNhap (1–50), HoTen (1–100), Email hợp lệ và MatKhau (12–128 ký tự) bằng User Secrets hoặc biến môi trường.");
            return 1;
        }
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (await db.TaiKhoans.AnyAsync(x => x.VaiTro == VaiTro.Admin || x.TenDangNhap == ten))
            {
                Console.Error.WriteLine("Đã có Admin hoặc tên đăng nhập đã được sử dụng. Không thay đổi tài khoản hiện có.");
                return 1;
            }
            var taiKhoan = new TaiKhoan { TenDangNhap = ten, HoTen = hoTen, Email = email, VaiTro = VaiTro.Admin };
            taiKhoan.MatKhau = scope.ServiceProvider.GetRequiredService<IPasswordHasher<TaiKhoan>>().HashPassword(taiKhoan, matKhau);
            db.TaiKhoans.Add(taiKhoan);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            Console.WriteLine("Đã tạo Admin đầu tiên. Hãy xóa cấu hình AdminKhoiTao sau khi sử dụng.");
            return 0;
        }
        catch (Exception exception) when (exception is System.Data.Common.DbException or DbUpdateException)
        {
            Console.Error.WriteLine("Không tạo được Admin. Kiểm tra kết nối, migration và tên đăng nhập; thử lại nếu có yêu cầu đồng thời.");
            return 1;
        }
    }
}

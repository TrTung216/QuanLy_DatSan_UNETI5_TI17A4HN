// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ hợp đồng tài khoản hiện tại.
using System.Security.Claims;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public interface ITaiKhoanHienTai
{
    int? MaTaiKhoan { get; }
    string? HoTen { get; }
    VaiTro? VaiTro { get; }
}

// Chỉ đọc principal đã xác thực; không lấy mã hay vai trò từ form.
public class TaiKhoanHienTai(IHttpContextAccessor accessor) : ITaiKhoanHienTai
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;
    public int? MaTaiKhoan => User?.Identity?.IsAuthenticated == true
        && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ma) ? ma : null;
    public string? HoTen => MaTaiKhoan.HasValue ? User?.Identity?.Name : null;
    public VaiTro? VaiTro => MaTaiKhoan.HasValue
        && Enum.TryParse<VaiTro>(User?.FindFirstValue(ClaimTypes.Role), out var vaiTro)
        && Enum.IsDefined(vaiTro) ? vaiTro : null;
}

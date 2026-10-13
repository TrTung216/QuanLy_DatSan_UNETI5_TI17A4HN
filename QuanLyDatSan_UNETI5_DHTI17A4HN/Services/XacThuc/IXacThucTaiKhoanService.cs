// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ hợp đồng xác thực; không trả entity hoặc hash mật khẩu.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;

public sealed class XacThucTaiKhoanYeuCau
{
    public required string TenDangNhap { get; init; }
    public required string MatKhau { get; init; }
}
public record TaiKhoanDaXacThuc(int MaTaiKhoan, string HoTen, VaiTro VaiTro);
public enum MaLoiXacThuc { ThanhCong, ThongTinKhongHopLe }
public record KetQuaXacThuc(MaLoiXacThuc MaLoi, TaiKhoanDaXacThuc? TaiKhoan = null);

// Người gọi: TaiKhoanController. TaiKhoan chỉ có khi thành công; mọi lỗi đăng nhập dùng cùng mã để không lộ tài khoản.
// Service chỉ xác thực/rehash. Session, cookie và ReturnUrl thuộc Controller/hạ tầng hiện có.
public interface IXacThucTaiKhoanService
{
    Task<KetQuaXacThuc> XacThucAsync(XacThucTaiKhoanYeuCau yeuCau, CancellationToken cancellationToken);
}

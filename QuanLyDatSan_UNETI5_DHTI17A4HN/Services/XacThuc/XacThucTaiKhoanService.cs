// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách kiểm tra đăng nhập và nâng cấp hash mật khẩu.
using Microsoft.AspNetCore.Identity;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.XacThuc;

public class XacThucTaiKhoanService(ITaiKhoanRepository repository, IDonViCongViec donViCongViec, IPasswordHasher<TaiKhoan> hasher) : IXacThucTaiKhoanService
{
    public async Task<KetQuaXacThuc> XacThucAsync(XacThucTaiKhoanYeuCau yeuCau, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.TenDangNhap) || yeuCau.TenDangNhap.Length > 50
            || string.IsNullOrWhiteSpace(yeuCau.MatKhau) || yeuCau.MatKhau.Length > 128)
            return new(MaLoiXacThuc.ThongTinKhongHopLe);
        var tenDangNhap = yeuCau.TenDangNhap.Trim();
        var taiKhoan = await repository.LayTheoTenDeCapNhatAsync(tenDangNhap, cancellationToken);
        var ketQua = PasswordVerificationResult.Failed;
        if (taiKhoan is not null && taiKhoan.TrangThai == TrangThaiTaiKhoan.HoatDong && Enum.IsDefined(taiKhoan.VaiTro))
        {
            try { ketQua = hasher.VerifyHashedPassword(taiKhoan, taiKhoan.MatKhau, yeuCau.MatKhau); }
            catch (FormatException) { /* Giữ hành vi từ chối giá trị băm hỏng. */ }
        }
        if (taiKhoan is null || ketQua == PasswordVerificationResult.Failed) return new(MaLoiXacThuc.ThongTinKhongHopLe);
        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
        {
            taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, yeuCau.MatKhau);
            await donViCongViec.LuuAsync(cancellationToken);
        }
        return new(MaLoiXacThuc.ThanhCong, new(taiKhoan.MaTaiKhoan, taiKhoan.HoTen, taiKhoan.VaiTro));
    }
}

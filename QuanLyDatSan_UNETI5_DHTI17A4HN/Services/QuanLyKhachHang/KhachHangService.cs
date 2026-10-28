// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. nghiệp vụ xem và sửa hồ sơ cá nhân của khách hàng đang đăng nhập.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

public class KhachHangService(IKhachHangRepository repository, IDonViCongViec donViCongViec, ITaiKhoanHienTai taiKhoanHienTai, IDongHo dongHo)
    : IKhachHangService
{
    public async Task<HoSoKhachDto?> LayHoSoAsync(CancellationToken cancellationToken)
    {
        // Mã tài khoản lấy từ phiên đăng nhập, không nhận từ form hay URL.
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return null;

        return await repository.LayHoSoAsync(maTaiKhoan, cancellationToken);
    }

    public async Task<KetQuaCapNhatHoSo> CapNhatHoSoAsync(CapNhatHoSoDto dto, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan)
            return KetQuaCapNhatHoSo.ThatBai(MaLoiHoSo.KhongTonTai);

        // Service tự kiểm tra lại, không phụ thuộc việc Controller đã validate.
        if (dto.NgaySinh is { } ngaySinh && (ngaySinh > dongHo.HomNay || ngaySinh.Year < 1900))
            return KetQuaCapNhatHoSo.ThatBai(MaLoiHoSo.NgaySinhKhongHopLe);

        var khachHang = await repository.LayDeCapNhatAsync(maTaiKhoan, cancellationToken);
        if (khachHang is null) return KetQuaCapNhatHoSo.ThatBai(MaLoiHoSo.KhongTonTai);
        if (khachHang.TrangThai != TrangThaiKhachHang.HoatDong)
            return KetQuaCapNhatHoSo.ThatBai(MaLoiHoSo.HoSoBiKhoa);

        var hoTen = dto.HoTen.Trim();
        var email = dto.Email.Trim();
        khachHang.HoTen = hoTen;
        khachHang.Email = email;
        khachHang.SoDienThoai = dto.SoDienThoai.Trim();
        khachHang.NgaySinh = dto.NgaySinh;
        khachHang.GioiTinh = dto.GioiTinh;
        khachHang.DiaChi = string.IsNullOrWhiteSpace(dto.DiaChi) ? null : dto.DiaChi.Trim();
        // Hồ sơ và tài khoản trùng HoTen/Email: cập nhật cùng một lần SaveChanges (một giao dịch).
        khachHang.TaiKhoan.HoTen = hoTen;
        khachHang.TaiKhoan.Email = email;
        await donViCongViec.LuuAsync(cancellationToken);

        return KetQuaCapNhatHoSo.ThanhCongMoi();
    }
}

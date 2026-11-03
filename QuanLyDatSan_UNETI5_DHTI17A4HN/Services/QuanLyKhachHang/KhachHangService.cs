// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. nghiệp vụ xem và sửa hồ sơ cá nhân của khách hàng đang đăng nhập.
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

public partial class KhachHangService(IKhachHangRepository repository, IDonViCongViec donViCongViec, ITaiKhoanHienTai taiKhoanHienTai, IDongHo dongHo)
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
        var loi = KiemTraDuLieu(dto);
        if (loi is not null) return KetQuaCapNhatHoSo.ThatBai(loi.Value);

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

    private static readonly EmailAddressAttribute KiemTraEmail = new();

    [GeneratedRegex(@"^(0|\+84)\d{9,10}$")]
    private static partial Regex MauSoDienThoai();

    // Thứ tự kiểm tra cố định để một dữ liệu sai luôn trả cùng một mã lỗi.
    // Chuỗi null (dữ liệu không đi qua ViewModel) được coi là thiếu, không gây NullReferenceException.
    private MaLoiHoSo? KiemTraDuLieu(CapNhatHoSoDto dto)
    {
        var hoTen = dto.HoTen?.Trim();
        if (string.IsNullOrEmpty(hoTen) || hoTen.Length > 100) return MaLoiHoSo.HoTenKhongHopLe;

        var email = dto.Email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 254 || !KiemTraEmail.IsValid(email))
            return MaLoiHoSo.EmailKhongHopLe;

        var soDienThoai = dto.SoDienThoai?.Trim();
        if (string.IsNullOrEmpty(soDienThoai) || soDienThoai.Length > 20
            || !MauSoDienThoai().IsMatch(soDienThoai))
            return MaLoiHoSo.SoDienThoaiKhongHopLe;

        if (dto.NgaySinh is { } ngaySinh && (ngaySinh > dongHo.HomNay || ngaySinh.Year < 1900))
            return MaLoiHoSo.NgaySinhKhongHopLe;

        if (!Enum.IsDefined(dto.GioiTinh)) return MaLoiHoSo.GioiTinhKhongHopLe;

        if (dto.DiaChi?.Trim().Length > 255) return MaLoiHoSo.DiaChiKhongHopLe;
        return null;
    }
}

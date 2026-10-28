// M3: Nguyễn Văn Quý; MSSV: 23103100181. Nghiệp vụ đăng ký gốc.
// Codex: tách khỏi controller, kiểm tra đầu vào và dùng Unit of Work.
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;

public sealed class DangKyKhachService(ITaiKhoanRepository taiKhoanRepository, ITrangChuKhachRepository repository,
    IDonViCongViec donViCongViec, IPasswordHasher<TaiKhoan> hasher, IDongHo dongHo, ITaiKhoanHienTai hienTai) : IDangKyKhachService
{
    public async Task<TrangChuKhachDto?> LayTrangChuAsync(CancellationToken cancellationToken) =>
        hienTai.MaTaiKhoan is { } id ? await repository.LayTrangChuAsync(id, dongHo.BayGio, cancellationToken) : null;

    public async Task<MaLoiDangKyKhach> DangKyAsync(DangKyKhach yeuCau, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(yeuCau.TenDangNhap) || yeuCau.TenDangNhap.Length > 50
            || string.IsNullOrWhiteSpace(yeuCau.MatKhau) || yeuCau.MatKhau.Length is < 8 or > 128
            || string.IsNullOrWhiteSpace(yeuCau.HoTen) || yeuCau.HoTen.Length > 100
            || string.IsNullOrWhiteSpace(yeuCau.Email) || yeuCau.Email.Length > 254 || !new EmailAddressAttribute().IsValid(yeuCau.Email)
            || string.IsNullOrWhiteSpace(yeuCau.SoDienThoai) || yeuCau.SoDienThoai.Length > 20
            || !Regex.IsMatch(yeuCau.SoDienThoai, @"^(0|\+84)\d{9,10}$")
            || (yeuCau.NgaySinh is { } ngaySinh && (ngaySinh > dongHo.HomNay || ngaySinh.Year < 1900))
            || yeuCau.DiaChi?.Length > 255 || (yeuCau.GioiTinh.HasValue && !Enum.IsDefined(yeuCau.GioiTinh.Value)))
            return MaLoiDangKyKhach.DuLieuKhongHopLe;
        var tenDangNhap = yeuCau.TenDangNhap.Trim();
        if (await taiKhoanRepository.TrungTenAsync(tenDangNhap, cancellationToken)) return MaLoiDangKyKhach.TrungTen;
        var hoTen = yeuCau.HoTen.Trim();
        var email = yeuCau.Email.Trim();
        var taiKhoan = new TaiKhoan
        {
            TenDangNhap = tenDangNhap,
            HoTen = hoTen,
            Email = email,
            VaiTro = VaiTro.KhachHang,
            TrangThai = TrangThaiTaiKhoan.HoatDong
        };
        taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, yeuCau.MatKhau);
        taiKhoan.KhachHang = new KhachHang
        {
            HoTen = hoTen,
            Email = email,
            SoDienThoai = yeuCau.SoDienThoai.Trim(),
            NgaySinh = yeuCau.NgaySinh,
            GioiTinh = yeuCau.GioiTinh,
            DiaChi = string.IsNullOrWhiteSpace(yeuCau.DiaChi) ? null : yeuCau.DiaChi.Trim(),
            NgayDangKy = dongHo.BayGio,
            DiemTichLuy = 0,
            TrangThai = TrangThaiKhachHang.HoatDong
        };

        taiKhoanRepository.Them(taiKhoan);
        try { await donViCongViec.LuuAsync(cancellationToken); }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.TrungTenDangNhap) { return MaLoiDangKyKhach.TrungTen; }
        return MaLoiDangKyKhach.ThanhCong;
    }
}

// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách nghiệp vụ tài khoản, transaction và thu hồi phiên.
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;

public class QuanLyTaiKhoanService(ITaiKhoanRepository repository, IDonViCongViec donViCongViec, IPasswordHasher<TaiKhoan> hasher,
    ITaiKhoanHienTai hienTai, PhienBanQuyenTaiKhoan phienBanQuyen) : IQuanLyTaiKhoanService
{

    public Task<TrangTaiKhoan> LayDanhSachAsync(string? tuKhoa, VaiTro? vaiTro,
        TrangThaiTaiKhoan? trangThai, int trang, CancellationToken cancellationToken) =>
        repository.LayDanhSachAsync(tuKhoa?.Trim(), vaiTro, trangThai, trang, cancellationToken);
    public Task<TaiKhoanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken) =>
        repository.LayChiTietAsync(id, cancellationToken);
    public Task<KetQuaQuanLyTaiKhoan> TaoAsync(TaoTaiKhoan yeuCau, CancellationToken cancellationToken)
    {
        if (!ThongTinHopLe(yeuCau.HoTen, yeuCau.Email, yeuCau.VaiTro)
            || string.IsNullOrWhiteSpace(yeuCau.TenDangNhap) || yeuCau.TenDangNhap.Length > 50
            || string.IsNullOrWhiteSpace(yeuCau.MatKhau) || yeuCau.MatKhau.Length is < 12 or > 128)
            return Task.FromResult(new KetQuaQuanLyTaiKhoan([MaLoiTaiKhoan.DuLieuKhongHopLe]));
        var ten = yeuCau.TenDangNhap.Trim();
        return LuuAsync(async _ =>
        {
            if (await repository.TrungTenAsync(ten, cancellationToken))
                return new([MaLoiTaiKhoan.TrungTen]);
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = ten,
                HoTen = yeuCau.HoTen,
                Email = yeuCau.Email,
                VaiTro = yeuCau.VaiTro,
                TrangThai = TrangThaiTaiKhoan.HoatDong
            };
            taiKhoan.MatKhau = hasher.HashPassword(taiKhoan, yeuCau.MatKhau);
            repository.Them(taiKhoan);
            return new([]);
        }, cancellationToken);
    }

    public Task<KetQuaQuanLyTaiKhoan> SuaAsync(int id, SuaTaiKhoan yeuCau, CancellationToken cancellationToken)
    {
        if (!ThongTinHopLe(yeuCau.HoTen, yeuCau.Email, yeuCau.VaiTro) || !Enum.IsDefined(yeuCau.TrangThai))
            return Task.FromResult(new KetQuaQuanLyTaiKhoan([MaLoiTaiKhoan.DuLieuKhongHopLe]));
        var doiQuyenHoacTrangThai = false;
        return LuuAsync(async adminIds =>
        {
            var taiKhoan = await repository.LayDeCapNhatAsync(id, cancellationToken);
            if (taiKhoan is null) return new([MaLoiTaiKhoan.KhongTonTai]);
            var conLaAdminHoatDong = yeuCau.VaiTro == VaiTro.Admin && yeuCau.TrangThai == TrangThaiTaiKhoan.HoatDong;
            List<MaLoiTaiKhoan> loi = [];
            if (!conLaAdminHoatDong && adminIds.Contains(id) && adminIds.Count == 1) loi.Add(MaLoiTaiKhoan.AdminCuoiCung);
            if (id == hienTai.MaTaiKhoan && !conLaAdminHoatDong) loi.Add(MaLoiTaiKhoan.TuKhoaHoacHaQuyen);
            if (loi.Count > 0) return new(loi);

            doiQuyenHoacTrangThai = taiKhoan.VaiTro != yeuCau.VaiTro || taiKhoan.TrangThai != yeuCau.TrangThai;
            taiKhoan.HoTen = yeuCau.HoTen;
            taiKhoan.Email = yeuCau.Email;
            taiKhoan.VaiTro = yeuCau.VaiTro;
            taiKhoan.TrangThai = yeuCau.TrangThai;
            return new([]);
        }, cancellationToken, () =>
        {
            if (doiQuyenHoacTrangThai) phienBanQuyen.VoHieuHoa(id);
        });
    }

    private async Task<KetQuaQuanLyTaiKhoan> LuuAsync(Func<List<int>, Task<KetQuaQuanLyTaiKhoan>> capNhat,
        CancellationToken cancellationToken, Action? sauKhiLuu = null)
    {
        KetQuaQuanLyTaiKhoan ketQua;
        try
        {
            ketQua = await donViCongViec.TrongGiaoDichAsync(async token =>
            {
                var adminIds = await repository.LayAdminHoatDongAsync(token);
                if (!hienTai.MaTaiKhoan.HasValue || !adminIds.Contains(hienTai.MaTaiKhoan.Value))
                    return new KetQuaQuanLyTaiKhoan([MaLoiTaiKhoan.KhongDuQuyen]);
                return await capNhat(adminIds);
            }, x => x.ThanhCong, IsolationLevel.Serializable, cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.Ma == MaLoiLuuTru.TrungTenTaiKhoan)
        {
            return new([MaLoiTaiKhoan.TrungTen]);
        }
        catch (LoiLuuTruException ex) when (ex.Ma == MaLoiLuuTru.Deadlock)
        {
            return new([MaLoiTaiKhoan.XungDotDuLieu]);
        }
        // Nằm ngoài transaction và catch lưu trữ: lỗi thu hồi phiên không bị coi là rollback.
        if (ketQua.ThanhCong) sauKhiLuu?.Invoke();
        return ketQua;
    }
    private static bool ThongTinHopLe(string hoTen, string email, VaiTro vaiTro) =>
        !string.IsNullOrWhiteSpace(hoTen) && hoTen.Length <= 100
        && !string.IsNullOrWhiteSpace(email) && email.Length <= 254 && new EmailAddressAttribute().IsValid(email)
        && Enum.IsDefined(vaiTro);

}

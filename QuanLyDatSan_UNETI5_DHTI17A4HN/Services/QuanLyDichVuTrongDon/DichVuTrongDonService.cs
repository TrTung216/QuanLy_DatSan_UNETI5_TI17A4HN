// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: quản lý dịch vụ trong đơn; thay đổi khóa sân, kiểm tra RowVersion và lưu cùng transaction.
using System.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;

public class DichVuTrongDonService(
    IDichVuTrongDonRepository repository,
    IDonViCongViec donViCongViec,
    ITinhTienService tinhTien,
    ITaiKhoanHienTai taiKhoanHienTai) : IDichVuTrongDonService
{
    private const int SoLuongToiDa = 100;
    private const int DoDaiRowVersion = 8;

    public async Task<ChiTietDichVuTrongDon?> LayTheoDonAsync(int maDatSan, CancellationToken cancellationToken)
    {
        await KiemTraQuyenQuanLyAsync(cancellationToken);

        var trangThai = await repository.LayTrangThaiAsync(maDatSan, cancellationToken);
        if (trangThai is null) return null;

        var rowVersion = await repository.LayRowVersionAsync(maDatSan, cancellationToken);
        if (rowVersion is null) return null;

        var dong = await repository.LayDongAsync(maDatSan, cancellationToken);
        dong.AddRange(await repository.LayLichSuHuyAsync(maDatSan, cancellationToken));
        var choPhepSua = trangThai == TrangThaiDatSan.DangXuLy;
        var coTheThem = choPhepSua
            ? await repository.LayDichVuHoatDongAsync(cancellationToken)
            : [];
        var tongTien = tinhTien.TinhTienDichVu(dong
            .Where(x => !x.DaHuy)
            .Select(x => new DongDichVu(x.SoLuong, x.DonGia)));

        return new ChiTietDichVuTrongDon(maDatSan, trangThai.Value, choPhepSua, tongTien,
            Convert.ToBase64String(rowVersion), dong, coTheThem);
    }

    public async Task<KetQuaNghiepVu> ThemAsync(int maDatSan, int maDichVu, int soLuong, string phienBan,
        CancellationToken cancellationToken)
    {
        if (soLuong < 1 || soLuong > SoLuongToiDa)
            return KetQuaNghiepVu.BaoLoi($"Số lượng phải từ 1 đến {SoLuongToiDa}.", "SoLuong");

        return await ThayDoiAsync(maDatSan, phienBan, async (don, _, token) =>
        {
            var dichVu = await repository.LayDichVuAsync(maDichVu, token);
            if (dichVu is null) return KetQuaNghiepVu.BaoLoi("Dịch vụ không tồn tại.", "MaDichVu");
            if (dichVu.TrangThai != TrangThaiDichVu.HoatDong)
                return KetQuaNghiepVu.BaoLoi("Dịch vụ đã ngừng hoạt động.", "MaDichVu");

            var dongCoSan = don.ChiTietDatSans.SingleOrDefault(x => x.MaDichVu == maDichVu);
            if (dongCoSan is null)
            {
                don.ChiTietDatSans.Add(new ChiTietDatSan
                {
                    MaDichVu = maDichVu,
                    SoLuong = soLuong,
                    DonGia = dichVu.DonGia
                });
            }
            else
            {
                if (dongCoSan.SoLuong + soLuong > SoLuongToiDa)
                    return KetQuaNghiepVu.BaoLoi($"Tổng số lượng không được vượt quá {SoLuongToiDa}.", "SoLuong");
                dongCoSan.SoLuong += soLuong;
            }

            return null;
        }, "Đã thêm dịch vụ vào đơn.", cancellationToken);
    }

    public async Task<KetQuaNghiepVu> SuaSoLuongAsync(int maDatSan, int maChiTiet, int soLuong, string phienBan,
        CancellationToken cancellationToken)
    {
        if (soLuong < 1 || soLuong > SoLuongToiDa)
            return KetQuaNghiepVu.BaoLoi($"Số lượng phải từ 1 đến {SoLuongToiDa}.", "SoLuong");

        return await ThayDoiAsync(maDatSan, phienBan, (don, _, _) =>
        {
            var dong = don.ChiTietDatSans.SingleOrDefault(x => x.MaChiTiet == maChiTiet);
            if (dong is null) return Task.FromResult<KetQuaNghiepVu?>(KetQuaNghiepVu.KhongThay());

            dong.SoLuong = soLuong;
            return Task.FromResult<KetQuaNghiepVu?>(null);
        }, "Đã cập nhật số lượng dịch vụ.", cancellationToken);
    }

    public Task<KetQuaNghiepVu> HuyAsync(int maDatSan, int maChiTiet, string phienBan,
        CancellationToken cancellationToken) =>
        ThayDoiAsync(maDatSan, phienBan, async (don, maTaiKhoan, token) =>
        {
            var dong = don.ChiTietDatSans.SingleOrDefault(x => x.MaChiTiet == maChiTiet);
            if (dong is null) return KetQuaNghiepVu.KhongThay();

            await repository.GhiLichSuHuyVaXoaDongAsync(dong, maTaiKhoan, token);
            return null;
        }, "Đã hủy dịch vụ khỏi đơn; lịch sử được lưu.", cancellationToken);

    private async Task<KetQuaNghiepVu> ThayDoiAsync(int maDatSan, string phienBan,
        Func<DatSan, int, CancellationToken, Task<KetQuaNghiepVu?>> thayDoi, string thongBao,
        CancellationToken cancellationToken)
    {
        var maTaiKhoan = await KiemTraQuyenQuanLyAsync(cancellationToken);
        if (!TryDocRowVersion(phienBan, out var rowVersion))
            return KetQuaNghiepVu.BaoLoi("Phiên bản đơn không hợp lệ. Vui lòng tải lại trang.", "PhienBan");

        try
        {
            return await donViCongViec.ThucHienTrongGiaoDichAsync(async token =>
            {
                var maSan = await repository.LayMaSanAsync(maDatSan, token);
                if (maSan is null)
                    return new QuyetDinhLuu<KetQuaNghiepVu>(false, KetQuaNghiepVu.KhongThay());

                await repository.KhoaSanAsync(maSan.Value, token);
                if (!await repository.CoQuyenQuanLyAsync(maTaiKhoan, token))
                    throw new UnauthorizedAccessException(
                        "Tài khoản không còn quyền quản lý dịch vụ đơn.");
                var don = await repository.LayDonDeCapNhatAsync(maDatSan, token);
                if (don is null)
                    return new QuyetDinhLuu<KetQuaNghiepVu>(false, KetQuaNghiepVu.KhongThay());
                if (don.TrangThai != TrangThaiDatSan.DangXuLy)
                    return new QuyetDinhLuu<KetQuaNghiepVu>(false,
                        KetQuaNghiepVu.BaoLoi("Chỉ được thay đổi dịch vụ khi đơn đang xử lý."));
                if (!don.RowVersion.AsSpan().SequenceEqual(rowVersion))
                    return new QuyetDinhLuu<KetQuaNghiepVu>(false,
                        KetQuaNghiepVu.BaoLoi("Đơn vừa được cập nhật bởi người khác. Vui lòng tải lại và thử lại."));

                repository.DatRowVersionGoc(don, rowVersion);
                var loi = await thayDoi(don, maTaiKhoan, token);
                if (loi is not null)
                    return new QuyetDinhLuu<KetQuaNghiepVu>(false, loi);

                don.TienDichVu = tinhTien.TinhTienDichVu(repository.LayDongTinhTien(don));
                repository.DanhDauCapNhatTienDichVu(don);
                return new QuyetDinhLuu<KetQuaNghiepVu>(true, KetQuaNghiepVu.ThanhCongVoi(thongBao));
            }, IsolationLevel.ReadCommitted, cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            return KetQuaNghiepVu.BaoLoi("Đơn vừa được cập nhật bởi người khác. Vui lòng tải lại và thử lại.");
        }
    }

    private async Task<int> KiemTraQuyenQuanLyAsync(CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan
            || !await repository.CoQuyenQuanLyAsync(maTaiKhoan, cancellationToken))
            throw new UnauthorizedAccessException("Chỉ tài khoản Admin hoặc Nhân viên đang hoạt động được quản lý dịch vụ đơn.");
        return maTaiKhoan;
    }

    private static bool TryDocRowVersion(string? phienBan, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(phienBan)) return false;

        try
        {
            rowVersion = Convert.FromBase64String(phienBan);
            return rowVersion.Length == DoDaiRowVersion;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

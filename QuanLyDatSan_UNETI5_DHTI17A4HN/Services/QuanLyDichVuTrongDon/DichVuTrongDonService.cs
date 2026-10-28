// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: nghiệp vụ dịch vụ trong đơn; mỗi thao tác tính lại DatSan.TienDichVu và kiểm tra RowVersion của đơn.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;

public class DichVuTrongDonService(IDichVuTrongDonRepository repository, IDonViCongViec donViCongViec, ITinhTienService tinhTien) : IDichVuTrongDonService
{
    private const int SoLuongToiDa = 100;

    // Chỉ đơn đang chờ xử lý hoặc đã xác nhận mới được đổi dịch vụ.
    private static bool ChoPhepSua(TrangThaiDatSan trangThai)
        => trangThai is TrangThaiDatSan.ChoXuLy or TrangThaiDatSan.DangXuLy;

    public async Task<ChiTietDichVuTrongDon?> LayTheoDonAsync(int maDatSan, CancellationToken cancellationToken)
    {
        var trangThai = await repository.LayTrangThaiAsync(maDatSan, cancellationToken);
        if (trangThai is null) return null;
        var dong = await repository.LayDongAsync(maDatSan, cancellationToken);
        var choPhepSua = ChoPhepSua(trangThai.Value);
        var coTheThem = choPhepSua ? await repository.LayDichVuHoatDongAsync(cancellationToken) : [];
        var tongTien = tinhTien.TinhTienDichVu(dong.Select(x => new DongDichVu(x.SoLuong, x.DonGia)));
        return new ChiTietDichVuTrongDon(maDatSan, trangThai.Value, choPhepSua, tongTien, dong, coTheThem);
    }

    public async Task<KetQuaNghiepVu> ThemAsync(int maDatSan, int maDichVu, int soLuong,
        CancellationToken cancellationToken)
    {
        if (soLuong < 1 || soLuong > SoLuongToiDa)
            return KetQuaNghiepVu.BaoLoi($"Số lượng phải từ 1 đến {SoLuongToiDa}.", "SoLuong");

        var (don, loi) = await TaiDonAsync(maDatSan, cancellationToken);
        if (don is null) return loi!;

        var dichVu = await repository.LayDichVuAsync(maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.BaoLoi("Dịch vụ không tồn tại.", "MaDichVu");
        if (dichVu.TrangThai != TrangThaiDichVu.HoatDong)
            return KetQuaNghiepVu.BaoLoi("Dịch vụ đã ngừng hoạt động.", "MaDichVu");

        var dongCoSan = don.ChiTietDatSans.SingleOrDefault(x => x.MaDichVu == maDichVu);
        if (dongCoSan is null)
        {
            // Chụp giá hiện tại của dịch vụ vào dòng chi tiết.
            don.ChiTietDatSans.Add(new ChiTietDatSan { MaDichVu = maDichVu, SoLuong = soLuong, DonGia = dichVu.DonGia });
        }
        else
        {
            // Đã có dịch vụ này trong đơn: cộng dồn số lượng, giữ nguyên giá đã chụp.
            if (dongCoSan.SoLuong + soLuong > SoLuongToiDa)
                return KetQuaNghiepVu.BaoLoi($"Tổng số lượng không được vượt quá {SoLuongToiDa}.", "SoLuong");
            dongCoSan.SoLuong += soLuong;
        }

        return await LuuAsync(don, "Đã thêm dịch vụ vào đơn.", cancellationToken);
    }

    public async Task<KetQuaNghiepVu> SuaSoLuongAsync(int maDatSan, int maChiTiet, int soLuong,
        CancellationToken cancellationToken)
    {
        if (soLuong < 1 || soLuong > SoLuongToiDa)
            return KetQuaNghiepVu.BaoLoi($"Số lượng phải từ 1 đến {SoLuongToiDa}.", "SoLuong");

        var (don, loi) = await TaiDonAsync(maDatSan, cancellationToken);
        if (don is null) return loi!;

        var dong = don.ChiTietDatSans.SingleOrDefault(x => x.MaChiTiet == maChiTiet);
        if (dong is null) return KetQuaNghiepVu.KhongThay();

        dong.SoLuong = soLuong;
        return await LuuAsync(don, "Đã cập nhật số lượng dịch vụ.", cancellationToken);
    }

    // Hủy = xóa dòng chi tiết khỏi đơn.
    public async Task<KetQuaNghiepVu> HuyAsync(int maDatSan, int maChiTiet, CancellationToken cancellationToken)
    {
        var (don, loi) = await TaiDonAsync(maDatSan, cancellationToken);
        if (don is null) return loi!;

        var dong = don.ChiTietDatSans.SingleOrDefault(x => x.MaChiTiet == maChiTiet);
        if (dong is null) return KetQuaNghiepVu.KhongThay();

        repository.XoaDong(dong);
        return await LuuAsync(don, "Đã hủy dịch vụ khỏi đơn.", cancellationToken);
    }

    private async Task<(DatSan? Don, KetQuaNghiepVu? Loi)> TaiDonAsync(int maDatSan,
        CancellationToken cancellationToken)
    {
        var don = await repository.LayDonDeCapNhatAsync(maDatSan, cancellationToken);
        if (don is null) return (null, KetQuaNghiepVu.KhongThay());
        if (!ChoPhepSua(don.TrangThai))
            return (null, KetQuaNghiepVu.BaoLoi("Đơn ở trạng thái hiện tại không được sửa dịch vụ."));
        return (don, null);
    }

    private async Task<KetQuaNghiepVu> LuuAsync(DatSan don, string thongBao, CancellationToken cancellationToken)
    {
        var cacDong = repository.LayDongTinhTien(don);
        don.TienDichVu = tinhTien.TinhTienDichVu(cacDong);
        // Luôn UPDATE đơn để RowVersion được kiểm tra, tránh ghi đè khi M4 đổi trạng thái cùng lúc.
        repository.DanhDauCapNhatTienDichVu(don);

        try
        {
            await donViCongViec.LuuAsync(cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            return KetQuaNghiepVu.BaoLoi("Đơn vừa được cập nhật bởi người khác. Vui lòng tải lại và thử lại.");
        }
        return KetQuaNghiepVu.ThanhCongVoi(thongBao);
    }
}
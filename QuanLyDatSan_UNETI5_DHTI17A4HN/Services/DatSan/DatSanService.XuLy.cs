// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Chặn bỏ qua đối soát cọc và hủy đơn có tiền chờ xử lý.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Xác nhận, từ chối, hủy, ghi cọc và hoàn thành đơn; kiểm tra trạng thái và RowVersion.
// Khóa sân và lưu trong transaction; cọc là tổng đã nhận, tiền hoàn thành được tính lại qua M5.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    public Task<KetQuaXuLyDon> XacNhanDonAsync(XuLyDonYeuCau yeuCau,
        CancellationToken cancellationToken) =>
        XuLyDonCoKhoaSanAsync(yeuCau.MaDatSan, yeuCau.PhienBan, async (don, token) =>
        {
            if (don.PhienBanChinhSachCoc == 1 && don.TienCocYeuCau > 0)
                return MaLoiDatSan.YeuCauDoiSoatCoc;
            var loi = await KiemTraXacNhanAsync(don, token);
            if (loi.HasValue) return loi;

            don.TrangThai = TrangThaiDatSan.DangXuLy;
            don.NgayXacNhan = dongHo.BayGio;
            return null;
        }, cancellationToken);

    public Task<KetQuaXuLyDon> TuChoiDonAsync(HuyDonYeuCau yeuCau,
        CancellationToken cancellationToken)
    {
        var (maDatSan, lyDo, phienBan) = yeuCau;
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.LyDoTuChoiKhongHopLe));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, async (don, token) =>
        {
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return MaLoiDatSan.ChiTuChoiDonCho;

            if (await KiemTraTienTruocHuyAsync(don, token) is { } loiCoc) return loiCoc;

            don.TrangThai = TrangThaiDatSan.TuChoi;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return null;
        }, cancellationToken);
    }

    public Task<KetQuaXuLyDon> KhachHangHuyDonAsync(HuyDonYeuCau yeuCau, int maTaiKhoan, CancellationToken cancellationToken)
    {
        var (maDatSan, lyDo, phienBan) = yeuCau;
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.LyDoHuyKhongHopLe));
        if (maTaiKhoan <= 0)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.TaiKhoanKhongHopLe));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, async (don, token) =>
        {
            if (don.KhachHang.MaTaiKhoan != maTaiKhoan)
                return MaLoiDatSan.KhongTonTai;
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return MaLoiDatSan.KhachChiHuyDonCho;
            if (don.GioBatDau <= dongHo.BayGio)
                return MaLoiDatSan.QuaGioHuy;

            if (await KiemTraTienTruocHuyAsync(don, token) is { } loiCoc) return loiCoc;

            don.TrangThai = TrangThaiDatSan.DaHuy;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return null;
        }, cancellationToken, maTaiKhoan);
    }

    public Task<KetQuaXuLyDon> NhanVienHuyDonAsync(HuyDonYeuCau yeuCau,
        CancellationToken cancellationToken)
    {
        var (maDatSan, lyDo, phienBan) = yeuCau;
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.LyDoHuyKhongHopLe));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, async (don, token) =>
        {
            if (don.TrangThai is not (TrangThaiDatSan.ChoXuLy or TrangThaiDatSan.DangXuLy))
                return MaLoiDatSan.TrangThaiKhongChoHuy;
            if (don.GioBatDau <= dongHo.BayGio)
                return MaLoiDatSan.QuaGioHuy;

            if (await KiemTraTienTruocHuyAsync(don, token) is { } loiCoc) return loiCoc;

            don.TrangThai = TrangThaiDatSan.DaHuy;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return null;
        }, cancellationToken);
    }

    public Task<KetQuaXuLyDon> GhiCocAsync(GhiCocYeuCau yeuCau, CancellationToken cancellationToken)
    {
        if (yeuCau.TongCoc <= 0 || yeuCau.TongCoc > TienToiDa
            || decimal.Truncate(yeuCau.TongCoc) != yeuCau.TongCoc)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.TienCocKhongHopLe));

        return XuLyDonCoKhoaSanAsync(yeuCau.MaDatSan, yeuCau.PhienBan, async (don, token) =>
        {
            if (don.PhienBanChinhSachCoc != 0) return MaLoiDatSan.YeuCauDoiSoatCoc;
            if (don.TrangThai != TrangThaiDatSan.DangXuLy)
                return MaLoiDatSan.ChiGhiCocDonXacNhan;
            if (yeuCau.TongCoc <= don.TienCoc)
                return MaLoiDatSan.CocKhongTang;
            var tien = await TinhTienDonAsync(don, token);
            if (tien.Tong > TienToiDa) return MaLoiDatSan.TienVuotGioiHan;
            if (yeuCau.TongCoc > tien.Tong) return MaLoiDatSan.CocVuotTongTien;
            don.TienCoc = yeuCau.TongCoc;
            return null;
        }, cancellationToken);
    }

    public Task<KetQuaXuLyDon> HoanThanhDonAsync(HoanThanhDonYeuCau yeuCau, CancellationToken cancellationToken)
    {
        var (maDatSan, daThuDu, phienBan) = yeuCau;
        if (!daThuDu)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.ChuaThuDu));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, async (don, token) =>
        {
            if (don.TrangThai != TrangThaiDatSan.DangXuLy)
                return MaLoiDatSan.ChiHoanThanhDonXacNhan;
            if (don.GioKetThuc > dongHo.BayGio)
                return MaLoiDatSan.ChuaDenGioKetThuc;
            var tien = await TinhTienDonAsync(don, token);
            if (tien.Tong > TienToiDa) return MaLoiDatSan.TienVuotGioiHan;
            if (don.TienCoc > tien.Tong)
                return MaLoiDatSan.TongTienThapHonCoc;

            don.TienSan = tien.San;
            don.TienDichVu = tien.DichVu;
            don.TongTien = tien.Tong;
            don.NgayHoanThanh = dongHo.BayGio;
            don.TrangThai = TrangThaiDatSan.HoanThanh;
            return null;
        }, cancellationToken);
    }

    private async Task<KetQuaXuLyDon> XuLyDonCoKhoaSanAsync(int maDatSan, byte[] phienBan,
        Func<DatSan, CancellationToken, Task<MaLoiDatSan?>> apDung,
        CancellationToken cancellationToken, int? maTaiKhoanChuDon = null)
    {
        if (maDatSan <= 0)
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.MaDonKhongHopLe);
        if (phienBan is not { Length: 8 })
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.PhienBanKhongHopLe);
        if (maTaiKhoanChuDon.HasValue
            ? taiKhoanHienTai.MaTaiKhoan != maTaiKhoanChuDon || !await KhachDuDieuKienAsync(maTaiKhoanChuDon.Value, cancellationToken)
            : !await CoQuyenQuanLyAsync(cancellationToken))
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongDuQuyen);

        var maSan = await repository.LayMaSanCuaDonAsync(maDatSan, maTaiKhoanChuDon, cancellationToken);
        if (!maSan.HasValue)
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongTonTai);

        try
        {
            return await donViCongViec.TrongGiaoDichAsync(async token =>
            {
                await KhoaSanAsync(maSan.Value, cancellationToken);
                if (maTaiKhoanChuDon.HasValue
                    ? taiKhoanHienTai.MaTaiKhoan != maTaiKhoanChuDon || !await KhachDuDieuKienAsync(maTaiKhoanChuDon.Value, token)
                    : !await CoQuyenQuanLyAsync(token))
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongDuQuyen);

                var don = await repository.LayDonDeCapNhatAsync(maDatSan, maTaiKhoanChuDon, token);
                if (don is null)
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongTonTai);
                if (don.MaSan != maSan.Value)
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.SanDaThayDoi);
                if (!don.RowVersion.AsSpan().SequenceEqual(phienBan))
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.PhienBanDaCu);

                var loi = await apDung(don, token);
                if (loi is not null)
                    return KetQuaXuLyDon.ThatBai(loi.Value);

                return KetQuaXuLyDon.ThanhCongResult();
            }, x => x.ThanhCong, System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.XungDotDuLieu);
        }
    }
}

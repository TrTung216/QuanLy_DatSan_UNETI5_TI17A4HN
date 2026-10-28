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
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return MaLoiDatSan.ChiXacNhanDonCho;

            var now = dongHo.BayGio;
            var loiGio = KiemTraKhoangGio(don.GioBatDau, don.GioKetThuc, now);
            if (loiGio is not null) return AnhXaLoiKhaDung(loiGio.Value, khiTao: false);
            var san = await repository.LayKhaDungAsync(don.MaSan, don.GioBatDau, don.GioKetThuc, don.MaDatSan, token);
            var loiSan = DanhGiaKhaDungSan(san, don.GioBatDau, don.GioKetThuc);
            if (loiSan is not null) return AnhXaLoiKhaDung(loiSan.Value, khiTao: false);

            don.TrangThai = TrangThaiDatSan.DangXuLy;
            don.NgayXacNhan = now;
            return null;
        }, cancellationToken);

    public Task<KetQuaXuLyDon> TuChoiDonAsync(HuyDonYeuCau yeuCau,
        CancellationToken cancellationToken)
    {
        var (maDatSan, lyDo, phienBan) = yeuCau;
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.LyDoTuChoiKhongHopLe));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.ChiTuChoiDonCho);

            if (don.TienCoc > 0m)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.DaNhanCoc);

            don.TrangThai = TrangThaiDatSan.TuChoi;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return Task.FromResult<MaLoiDatSan?>(null);
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

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.KhachHang.MaTaiKhoan != maTaiKhoan)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.KhongTonTai);
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.KhachChiHuyDonCho);
            if (don.GioBatDau <= dongHo.BayGio)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.QuaGioHuy);

            if (don.TienCoc > 0m)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.DaNhanCoc);

            don.TrangThai = TrangThaiDatSan.DaHuy;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return Task.FromResult<MaLoiDatSan?>(null);
        }, cancellationToken, maTaiKhoan);
    }

    public Task<KetQuaXuLyDon> NhanVienHuyDonAsync(HuyDonYeuCau yeuCau,
        CancellationToken cancellationToken)
    {
        var (maDatSan, lyDo, phienBan) = yeuCau;
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.LyDoHuyKhongHopLe));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.TrangThai is not (TrangThaiDatSan.ChoXuLy or TrangThaiDatSan.DangXuLy))
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.TrangThaiKhongChoHuy);
            if (don.GioBatDau <= dongHo.BayGio)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.QuaGioHuy);

            if (don.TienCoc > 0m)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.DaNhanCoc);

            don.TrangThai = TrangThaiDatSan.DaHuy;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return Task.FromResult<MaLoiDatSan?>(null);
        }, cancellationToken);
    }

    public Task<KetQuaXuLyDon> GhiCocAsync(GhiCocYeuCau yeuCau, CancellationToken cancellationToken)
    {
        if (yeuCau.TongCoc <= 0 || yeuCau.TongCoc > TienToiDa
            || decimal.Truncate(yeuCau.TongCoc) != yeuCau.TongCoc)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.TienCocKhongHopLe));

        return XuLyDonCoKhoaSanAsync(yeuCau.MaDatSan, yeuCau.PhienBan, async (don, token) =>
        {
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

        var maSan = await repository.LayMaSanCuaDonAsync(maDatSan, maTaiKhoanChuDon, cancellationToken);
        if (!maSan.HasValue)
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongTonTai);

        try
        {
            return await donViCongViec.TrongGiaoDichAsync(async token =>
            {
                await KhoaSanAsync(maSan.Value, cancellationToken);

                var don = await repository.LayDonDeCapNhatAsync(maDatSan, maTaiKhoanChuDon, token);
                if (don is null)
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongTonTai);
                if (don.MaSan != maSan.Value)
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.SanDaThayDoi);
                if (!don.RowVersion.AsSpan().SequenceEqual(phienBan))
                    return KetQuaXuLyDon.ThatBai(MaLoiDatSan.PhienBanDaCu);

                var loi = await apDung(don, cancellationToken);
                if (loi is not null)
                    return KetQuaXuLyDon.ThatBai(loi.Value);

                return KetQuaXuLyDon.ThanhCongResult();
            }, x => x.ThanhCong, System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.Ma == MaLoiLuuTru.XungDotPhienBan)
        {
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.XungDotDuLieu);
        }
    }
}

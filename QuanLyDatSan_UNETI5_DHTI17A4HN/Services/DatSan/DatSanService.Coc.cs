// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Đối soát cọc, kiểm tra quyền và xác nhận đặt sân trong transaction.
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    private bool MoPhongSanSang => moiTruong.IsDevelopment() && cauHinhCoc.Value.Bat
        && !string.IsNullOrWhiteSpace(cauHinhCoc.Value.TaiKhoanNhan) && cauHinhCoc.Value.TaiKhoanNhan.Trim().Length <= 100;
    private string TaiKhoanNhan => cauHinhCoc.Value.TaiKhoanNhan.Trim().ToUpperInvariant();
    private static string NoiDungCoc(int id) => "COC " + id.ToString(CultureInfo.InvariantCulture);
    private static string ChuanHoaNoiDung(string value) => Regex.Replace(value.Trim(), @"\s+", " ").ToUpperInvariant();
    private async Task<bool> CoQuyenQuanLyAsync(CancellationToken ct)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } id) return false;
        var actor = await repository.LayTaiKhoanAsync(id, ct);
        return actor is { TrangThai: TrangThaiTaiKhoan.HoatDong, VaiTro: VaiTro.Admin or VaiTro.NhanVien };
    }
    private static GiaoDichCocDto Dto(GiaoDichCoc g) => new(g.MaGiaoDich, g.MaGiaoDichNgoai, g.NoiDung,
        g.Nguon, g.TaiKhoanNhan, g.SoTien, g.NgayNhan, g.TrangThai);
    private static BienNhanCocDto Dto(BienNhanCoc b) => new(b.MaBienNhan, b.MaDatSan, b.SoTien,
        b.MaGiaoDichNgoai, b.Nguon, b.NgayNhan, b.NgayXacNhan);

    public async Task<KetQuaCoc<ThongTinCocDto>> LayThongTinCocAsync(int maDatSan, CancellationToken ct)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } id || !await KhachDuDieuKienAsync(id, ct))
            return KetQuaCoc<ThongTinCocDto>.ThatBai(MaLoiCoc.KhongDuQuyen);
        var don = await repository.DocDonCocAsync(maDatSan, ct);
        if (don is null || don.KhachHang.MaTaiKhoan != id)
            return KetQuaCoc<ThongTinCocDto>.ThatBai(MaLoiCoc.KhongTonTai);
        return await DocThongTinCocAsync(don, ct);
    }

    private async Task<KetQuaCoc<ThongTinCocDto>> DocThongTinCocAsync(DatSan don, CancellationToken ct)
    {
        var bienNhan = await repository.LayBienNhanAsync(don.MaDatSan, ct);
        var giaoDich = await repository.LayGiaoDichCuaDonAsync(NoiDungCoc(don.MaDatSan), ct);
        var apDung = don.PhienBanChinhSachCoc == 1 && don.TienCocYeuCau > 0;
        var trangThai = bienNhan is not null || don.TienCoc > 0 ? TrangThaiCocHienThi.DaNhan
            : giaoDich.Any(x => x.TrangThai == TrangThaiGiaoDichCoc.CanXuLyNgoaiLe) ? TrangThaiCocHienThi.CanXuLyNgoaiLe
            : giaoDich.Any(x => x.TrangThai == TrangThaiGiaoDichCoc.ChuaDoiSoat) ? TrangThaiCocHienThi.ChoDoiSoat
            : !apDung ? TrangThaiCocHienThi.KhongApDung
            : !MoPhongSanSang ? TrangThaiCocHienThi.KhongSanSang : TrangThaiCocHienThi.ChuaNhan;
        var coTheChuyen = trangThai == TrangThaiCocHienThi.ChuaNhan && don.TrangThai == TrangThaiDatSan.ChoXuLy
            && dongHo.BayGio < don.GioBatDau;
        try
        {
            var tienSan = don.TrangThai == TrangThaiDatSan.HoanThanh && don.TienSan.HasValue
                ? don.TienSan.Value : tinhTien.TinhTienSan(don.DonGia, don.GioBatDau, don.GioKetThuc);
            return KetQuaCoc<ThongTinCocDto>.DaXong(new(don.MaDatSan, don.TrangThai, don.PhienBanChinhSachCoc,
                tienSan, don.TienCocYeuCau, don.TienCoc, NoiDungCoc(don.MaDatSan), trangThai, true, coTheChuyen,
                coTheChuyen ? $"DEMO:ORDER:{don.MaDatSan}" : null, bienNhan is null ? null : Dto(bienNhan)));
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        { return KetQuaCoc<ThongTinCocDto>.ThatBai(MaLoiCoc.DuLieuTienKhongHopLe); }
    }

    public async Task<KetQuaCoc<DoiSoatCocDto>> LayDoiSoatCocAsync(int maDatSan, CancellationToken ct)
    {
        if (!await CoQuyenQuanLyAsync(ct)) return KetQuaCoc<DoiSoatCocDto>.ThatBai(MaLoiCoc.KhongDuQuyen);
        var don = await repository.DocDonCocAsync(maDatSan, ct);
        if (don is null) return KetQuaCoc<DoiSoatCocDto>.ThatBai(MaLoiCoc.KhongTonTai);
        var info = await DocThongTinCocAsync(don, ct);
        if (!info.ThanhCong) return KetQuaCoc<DoiSoatCocDto>.ThatBai(info.Loi!.Value);
        var giaoDich = await repository.LayGiaoDichCuaDonAsync(NoiDungCoc(maDatSan), ct);
        return KetQuaCoc<DoiSoatCocDto>.DaXong(new(info.DuLieu!, don.RowVersion, giaoDich.Select(Dto).ToList(),
            MoPhongSanSang && don.PhienBanChinhSachCoc == 1 && don.TienCocYeuCau > 0 && don.TienCoc == 0
            && don.TrangThai == TrangThaiDatSan.ChoXuLy && dongHo.BayGio < don.GioBatDau));
    }

    public async Task<KetQuaCoc<GiaoDichCocDto>> TaoGiaoDichMoPhongAsync(TaoGiaoDichMoPhongYeuCau yc, CancellationToken ct)
    {
        if (!MoPhongSanSang) return KetQuaCoc<GiaoDichCocDto>.ThatBai(MaLoiCoc.MoPhongKhongDuocPhep);
        if (!await CoQuyenQuanLyAsync(ct)) return KetQuaCoc<GiaoDichCocDto>.ThatBai(MaLoiCoc.KhongDuQuyen);
        var maNgoai = yc.MaGiaoDichNgoai?.Trim().ToUpperInvariant();
        if (maNgoai is null || !Regex.IsMatch(maNgoai, @"\A[A-Z0-9_./-]{3,100}\z")
            || string.IsNullOrWhiteSpace(yc.NoiDung) || yc.NoiDung.Length > 100
            || yc.SoTien <= 0 || yc.SoTien > 9999999999999999.99m || decimal.Round(yc.SoTien, 2) != yc.SoTien
            || yc.NgayNhan > dongHo.BayGio)
            return KetQuaCoc<GiaoDichCocDto>.ThatBai(MaLoiCoc.DuLieuKhongHopLe);
        var noiDung = ChuanHoaNoiDung(yc.NoiDung);
        int? maDon = noiDung.StartsWith("COC ", StringComparison.Ordinal)
            && int.TryParse(noiDung.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            && id > 0 && noiDung == NoiDungCoc(id) ? id : null;
        try
        {
            var result = await donViCongViec.TrongGiaoDichAsync(async token =>
            {
                DatSan? don = null;
                if (maDon.HasValue)
                {
                    var san = await repository.LayMaSanCuaDonAsync(maDon.Value, null, token);
                    if (san.HasValue) await KhoaSanAsync(san.Value, token);
                    don = await repository.DocDonCocAsync(maDon.Value, token);
                }
                if (!await CoQuyenQuanLyAsync(token)) return KetQuaCoc<GiaoDichCoc>.ThatBai(MaLoiCoc.KhongDuQuyen);
                var gd = new GiaoDichCoc
                {
                    MaGiaoDichNgoai = maNgoai, NoiDung = noiDung, SoTien = yc.SoTien, NgayNhan = yc.NgayNhan,
                    NgayGhi = dongHo.BayGio, MaNguoiGhi = taiKhoanHienTai.MaTaiKhoan!.Value, TaiKhoanNhan = TaiKhoanNhan
                };
                // Tiền tới sau hủy/quá giờ vẫn được lưu để đối soát thủ công, không mở lại đơn.
                if (don is null || don.TrangThai != TrangThaiDatSan.ChoXuLy || don.GioBatDau <= dongHo.BayGio)
                {
                    gd.TrangThai = TrangThaiGiaoDichCoc.CanXuLyNgoaiLe;
                    gd.LoiNgoaiLe = don is null ? MaLoiCoc.GiaoDichKhongKhop : MaLoiCoc.ThoiDiemNhanKhongHopLe;
                }
                repository.ThemGiaoDich(gd);
                return KetQuaCoc<GiaoDichCoc>.DaXong(gd);
            }, x => x.ThanhCong, IsolationLevel.ReadCommitted, ct);
            return result.ThanhCong ? KetQuaCoc<GiaoDichCocDto>.DaXong(Dto(result.DuLieu!))
                : KetQuaCoc<GiaoDichCocDto>.ThatBai(result.Loi!.Value);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.TrungGiaoDichCoc)
        { return KetQuaCoc<GiaoDichCocDto>.ThatBai(MaLoiCoc.TrungMaGiaoDich); }
    }

    private sealed record CocDaXuLy(DatSan Don, BienNhanCoc BienNhan, bool DaXuLyTruocDo);
    public async Task<KetQuaCoc<XacNhanCocDto>> XacNhanCocVaDatSanAsync(XacNhanCocYeuCau yc, CancellationToken ct)
    {
        if (!await CoQuyenQuanLyAsync(ct)) return KetQuaCoc<XacNhanCocDto>.ThatBai(MaLoiCoc.KhongDuQuyen);
        if (yc.MaDatSan <= 0 || yc.MaGiaoDich <= 0 || yc.PhienBan is not { Length: 8 })
            return KetQuaCoc<XacNhanCocDto>.ThatBai(MaLoiCoc.DuLieuKhongHopLe);
        byte[]? phienBanGiaoDich = null;
        KetQuaCoc<CocDaXuLy> result;
        try
        {
            result = await donViCongViec.TrongGiaoDichAsync(async token =>
            {
                var maSan = await repository.LayMaSanCuaDonAsync(yc.MaDatSan, null, token);
                if (!maSan.HasValue) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.KhongTonTai);
                await KhoaSanAsync(maSan.Value, token);
                if (!await CoQuyenQuanLyAsync(token)) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.KhongDuQuyen);
                var don = await repository.LayDonDeCapNhatAsync(yc.MaDatSan, null, token);
                if (don is null) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.KhongTonTai);
                var gd = await repository.LayGiaoDichAsync(yc.MaGiaoDich, token);
                var bienNhan = await repository.LayBienNhanAsync(yc.MaDatSan, token);
                if (bienNhan is not null)
                {
                    if (bienNhan.MaGiaoDich != yc.MaGiaoDich) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.DonDaNhanCoc);
                    if (gd is null || gd.TrangThai != TrangThaiGiaoDichCoc.DaGanBienNhan || don.TienCoc != bienNhan.SoTien)
                        return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.DuLieuTienKhongHopLe);
                    return KetQuaCoc<CocDaXuLy>.DaXong(new(don, bienNhan, true));
                }
                if (gd is null) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.GiaoDichKhongTonTai);
                if (await repository.LayBienNhanGiaoDichAsync(gd.MaGiaoDich, token) is not null)
                    return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.GiaoDichDaSuDung);
                if (!don.RowVersion.AsSpan().SequenceEqual(yc.PhienBan)) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.PhienBanDaCu);
                if (!yc.DaKiemTraGiaoDich) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.ChuaKiemTraGiaoDich);
                if (!MoPhongSanSang) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.ChuyenKhoanChuaSanSang);
                if (don.PhienBanChinhSachCoc != 1 || don.TienCocYeuCau == 0) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.KhongApDungCoc);
                if (don.TienCoc != 0) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.DonDaNhanCoc);
                if (gd.NoiDung != NoiDungCoc(don.MaDatSan) || gd.Nguon != "MoPhong" || gd.TaiKhoanNhan != TaiKhoanNhan)
                    return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.GiaoDichKhongKhop);
                phienBanGiaoDich = gd.RowVersion.ToArray();
                if (gd.SoTien != don.TienCocYeuCau) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.SoTienKhongKhop);
                if (gd.NgayNhan < don.NgayDat || gd.NgayNhan >= don.GioBatDau || gd.NgayNhan > dongHo.BayGio
                    || dongHo.BayGio >= don.GioBatDau) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.ThoiDiemNhanKhongHopLe);
                var loi = don.MaSan != maSan.Value ? MaLoiDatSan.SanDaThayDoi : await KiemTraXacNhanAsync(don, token);
                if (loi.HasValue) return KetQuaCoc<CocDaXuLy>.ThatBai(MaLoiCoc.DieuKienDonKhongHopLe, loi);
                var bn = new BienNhanCoc
                {
                    MaDatSan = don.MaDatSan, MaGiaoDich = gd.MaGiaoDich, SoTien = gd.SoTien,
                    NgayNhan = gd.NgayNhan, NgayXacNhan = dongHo.BayGio, MaNguoiXacNhan = taiKhoanHienTai.MaTaiKhoan!.Value,
                    Nguon = gd.Nguon, TaiKhoanNhan = gd.TaiKhoanNhan, MaGiaoDichNgoai = gd.MaGiaoDichNgoai, NoiDung = gd.NoiDung
                };
                repository.ThemBienNhan(bn);
                don.TienCoc = gd.SoTien;
                don.TrangThai = TrangThaiDatSan.DangXuLy;
                don.NgayXacNhan = bn.NgayXacNhan;
                gd.TrangThai = TrangThaiGiaoDichCoc.DaGanBienNhan;
                gd.LoiNgoaiLe = null;
                return KetQuaCoc<CocDaXuLy>.DaXong(new(don, bn, false));
            }, x => x.ThanhCong && !x.DuLieu!.DaXuLyTruocDo, IsolationLevel.ReadCommitted, ct);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi is MaLoiLuuTru.XungDotPhienBan or MaLoiLuuTru.TrungBienNhanCoc)
        {
            if (!await CoQuyenQuanLyAsync(ct)) return KetQuaCoc<XacNhanCocDto>.ThatBai(MaLoiCoc.KhongDuQuyen);
            var bn = await repository.LayBienNhanAsync(yc.MaDatSan, ct);
            var don = await repository.DocDonCocAsync(yc.MaDatSan, ct);
            if (bn is not null && bn.MaGiaoDich == yc.MaGiaoDich && don is not null && don.TienCoc == bn.SoTien)
                return KetQuaCoc<XacNhanCocDto>.DaXong(new(Dto(bn), don.TrangThai, don.RowVersion, true));
            return KetQuaCoc<XacNhanCocDto>.ThatBai(bn is not null ? MaLoiCoc.DonDaNhanCoc
                : await repository.LayBienNhanGiaoDichAsync(yc.MaGiaoDich, ct) is not null ? MaLoiCoc.GiaoDichDaSuDung : MaLoiCoc.PhienBanDaCu);
        }
        if (!result.ThanhCong)
        {
            if (phienBanGiaoDich is not null && result.Loi is MaLoiCoc.SoTienKhongKhop or MaLoiCoc.ThoiDiemNhanKhongHopLe or MaLoiCoc.DieuKienDonKhongHopLe)
                await DanhDauNgoaiLeAsync(yc.MaDatSan, yc.MaGiaoDich, phienBanGiaoDich, result.Loi.Value, ct);
            return KetQuaCoc<XacNhanCocDto>.ThatBai(result.Loi!.Value, result.LoiDatSan);
        }
        var data = result.DuLieu!;
        return KetQuaCoc<XacNhanCocDto>.DaXong(new(Dto(data.BienNhan), data.Don.TrangThai, data.Don.RowVersion, data.DaXuLyTruocDo));
    }

    private async Task DanhDauNgoaiLeAsync(int maDon, int maGiaoDich, byte[] phienBan, MaLoiCoc loi, CancellationToken ct)
    {
        try
        {
            await donViCongViec.TrongGiaoDichAsync(async token =>
            {
                var san = await repository.LayMaSanCuaDonAsync(maDon, null, token);
                if (!san.HasValue) return false;
                await KhoaSanAsync(san.Value, token);
                if (!await CoQuyenQuanLyAsync(token)) return false;
                var gd = await repository.LayGiaoDichAsync(maGiaoDich, token);
                if (gd is null || gd.TrangThai == TrangThaiGiaoDichCoc.DaGanBienNhan
                    || !gd.RowVersion.AsSpan().SequenceEqual(phienBan)) return false;
                gd.TrangThai = TrangThaiGiaoDichCoc.CanXuLyNgoaiLe;
                gd.LoiNgoaiLe = loi;
                return true;
            }, x => x, IsolationLevel.ReadCommitted, ct);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan) { /* Một đối soát khác đã thắng. */ }
    }

    private async Task<MaLoiDatSan?> KiemTraXacNhanAsync(DatSan don, CancellationToken ct)
    {
        if (don.TrangThai != TrangThaiDatSan.ChoXuLy) return MaLoiDatSan.ChiXacNhanDonCho;
        if (!await KhachDuDieuKienAsync(don.KhachHang.MaTaiKhoan, ct)) return MaLoiDatSan.HoSoKhongHoatDong;
        var loiGio = KiemTraKhoangGio(don.GioBatDau, don.GioKetThuc, dongHo.BayGio);
        if (loiGio.HasValue) return AnhXaLoiKhaDung(loiGio.Value, khiTao: false);
        var san = await repository.LayKhaDungAsync(don.MaSan, don.GioBatDau, don.GioKetThuc, don.MaDatSan, ct);
        var loiSan = DanhGiaKhaDungSan(san, don.GioBatDau, don.GioKetThuc);
        return loiSan.HasValue ? AnhXaLoiKhaDung(loiSan.Value, khiTao: false) : null;
    }

    private async Task<MaLoiDatSan?> KiemTraTienTruocHuyAsync(DatSan don, CancellationToken ct)
    {
        if (don.TienCoc > 0 || await repository.LayBienNhanAsync(don.MaDatSan, ct) is not null) return MaLoiDatSan.DaNhanCoc;
        var gd = await repository.LayGiaoDichCuaDonAsync(NoiDungCoc(don.MaDatSan), ct);
        return gd.Any(x => x.TrangThai != TrangThaiGiaoDichCoc.DaGanBienNhan) ? MaLoiDatSan.GiaoDichChoDoiSoat : null;
    }
}

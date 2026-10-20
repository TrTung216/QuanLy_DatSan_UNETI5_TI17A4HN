// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: xác nhận, từ chối, khách hàng hủy, nhân viên/Admin hủy và hoàn thành đơn theo điều kiện nghiệp vụ.
// Xác nhận dùng chung quy tắc khả dụng với M2/M3, đọc lại dữ liệu sân sau khóa.
// Chặn hủy/từ chối đơn có cọc; kiểm tra quyền sở hữu khi khách hủy; điều phối khóa sân, RowVersion và transaction; lưu trạng thái, thời điểm, lý do và tiền chốt.

using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    // Chỉ chuyển đơn đang chờ; khóa sân và kiểm tra lại điều kiện đặt trước khi xác nhận.
    public Task<KetQuaXuLyDon> XacNhanDonAsync(XuLyDonYeuCau yeuCau,
        CancellationToken cancellationToken) =>
        XuLyDonCoKhoaSanAsync(yeuCau.MaDatSan, yeuCau.PhienBan, async (don, token) =>
        {
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return MaLoiDatSan.ChiXacNhanDonCho;

            var now = dongHo.BayGio;
            var loiGio = KiemTraKhoangGio(don.GioBatDau, don.GioKetThuc, now);
            if (loiGio is not null) return AnhXaLoiKhaDung(loiGio.Value, khiTao: false);
            var san = await TruyVanKhaDungSan(don.GioBatDau, don.GioKetThuc, don.MaDatSan)
                .SingleOrDefaultAsync(x => x.MaSan == don.MaSan, token);
            var loiSan = DanhGiaKhaDungSan(san, don.GioBatDau, don.GioKetThuc);
            if (loiSan is not null) return AnhXaLoiKhaDung(loiSan.Value, khiTao: false);

            don.TrangThai = TrangThaiDatSan.DangXuLy;
            don.NgayXacNhan = now;
            return null;
        }, cancellationToken);

    // Chỉ nhân viên/Admin được gọi action này ở controller; service bắt buộc lý do và chỉ từ chối đơn chờ.
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

    // Khách chỉ hủy đơn của mình khi còn chờ, chưa nhận cọc và chưa tới giờ bắt đầu.
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

    // Nhân viên/Admin hủy đơn chờ hoặc đã xác nhận khi chưa nhận cọc và chưa tới giờ bắt đầu.
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

    // Giá tiền phải do tầng tính tiền (M5) cung cấp; service này chỉ chốt tiền và trạng thái trong cùng transaction.
    public Task<KetQuaXuLyDon> HoanThanhDonAsync(HoanThanhDonYeuCau yeuCau, CancellationToken cancellationToken)
    {
        var (maDatSan, tienSan, tienDichVu, daThuDu, phienBan) = yeuCau;
        if (!daThuDu)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.ChuaThuDu));
        if (tienSan < 0m || tienDichVu < 0m)
            return Task.FromResult(KetQuaXuLyDon.ThatBai(MaLoiDatSan.TienAm));
        var tongTien = tienSan + tienDichVu;

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.TrangThai != TrangThaiDatSan.DangXuLy)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.ChiHoanThanhDonXacNhan);
            if (don.GioKetThuc > dongHo.BayGio)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.ChuaDenGioKetThuc);
            if (don.TienCoc > tongTien)
                return Task.FromResult<MaLoiDatSan?>(MaLoiDatSan.TongTienThapHonCoc);

            don.TienSan = tienSan;
            don.TienDichVu = tienDichVu;
            don.TongTien = tongTien;
            don.NgayHoanThanh = dongHo.BayGio;
            don.TrangThai = TrangThaiDatSan.HoanThanh;
            return Task.FromResult<MaLoiDatSan?>(null);
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

        var truyVan = db.DatSans.Where(x => !maTaiKhoanChuDon.HasValue
            || x.KhachHang.MaTaiKhoan == maTaiKhoanChuDon.Value);
        var maSan = await truyVan.AsNoTracking()
            .Where(x => x.MaDatSan == maDatSan)
            .Select(x => (int?)x.MaSan)
            .SingleOrDefaultAsync(cancellationToken);
        if (!maSan.HasValue)
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongTonTai);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await KhoaSanAsync(maSan.Value, cancellationToken);

        var don = await truyVan
            .Include(x => x.SanTheThao).ThenInclude(x => x.LoaiSan)
            .Include(x => x.KhachHang)
            .SingleOrDefaultAsync(x => x.MaDatSan == maDatSan, cancellationToken);
        if (don is null)
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.KhongTonTai);
        if (don.MaSan != maSan.Value)
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.SanDaThayDoi);
        if (!don.RowVersion.AsSpan().SequenceEqual(phienBan))
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.PhienBanDaCu);

        var loi = await apDung(don, cancellationToken);
        if (loi is not null)
            return KetQuaXuLyDon.ThatBai(loi.Value);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return KetQuaXuLyDon.ThanhCongResult();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            // Không để thao tác đã báo thất bại được SaveChanges ở lần gọi sau trong cùng scope.
            db.Entry(don).State = EntityState.Detached;
            return KetQuaXuLyDon.ThatBai(MaLoiDatSan.XungDotDuLieu);
        }
    }
}

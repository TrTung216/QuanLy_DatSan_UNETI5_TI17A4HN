// Phan Giang Tâm - 23103100196. M2: Thay/gỡ ảnh sân, kiểm quyền và bảo toàn file khi xung đột.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

public partial class SanTheThaoService
{
    public async Task<KetQuaAnh> CapNhatAnhAsync(int maSan, YeuCauAnh yeuCau, CancellationToken cancellationToken)
    {
        if (!taiKhoanHienTai.MaTaiKhoan.HasValue || taiKhoanHienTai.VaiTro is not (VaiTro.Admin or VaiTro.NhanVien))
            return new(MaLoiAnh.KhongDuQuyen);
        var san = await repository.LayDeCapNhatAsync(maSan, cancellationToken);
        if (san is null) return new(MaLoiAnh.KhongTonTai);
        if (!Enum.IsDefined(yeuCau.ThaoTac)
            || (yeuCau.ThaoTac == ThaoTacAnh.ThayAnh) != (yeuCau.Tep is not null))
            return new(MaLoiAnh.DuLieuAnhKhongHopLe);

        // Khóa từ form chỉ so phiên bản, không dùng để đọc/xóa file.
        var anhCu = san.DuongDanAnh;
        if (!string.Equals(anhCu, yeuCau.AnhCu, StringComparison.Ordinal)) return new(MaLoiAnh.XungDotAnh);
        if (yeuCau.ThaoTac == ThaoTacAnh.GoAnh && anhCu is null) return new(MaLoiAnh.ThanhCong);

        IDisposable khoa;
        try { khoa = await khoAnh.GiuKhoaAsync(false, cancellationToken); }
        catch (LoiAnhException ex) { return new(ex.MaLoi); }
        using var giuKhoa = khoa;
        string? anhMoi = null;
        try
        {
            if (yeuCau.Tep is not null)
                anhMoi = await khoAnh.LuuAsync(NhomAnh.San, yeuCau.Tep, cancellationToken);
        }
        catch (LoiAnhException ex) { return new(ex.MaLoi); }

        // Chỉ đổi cột ảnh; file mới đã hoàn tất trước khi UoW mở transaction DB.
        san.DuongDanAnh = anhMoi;
        try { await donViCongViec.LuuAsync(cancellationToken); }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            await donDepAnh.XoaNeuKhongThamChieuAsync(anhMoi);
            return new(MaLoiAnh.XungDotAnh);
        }
        catch
        {
            // Commit có thể đã hoàn tất: giữ file mới, không xóa dựa vào trạng thái tracking.
            logger.LogWarning("Lưu ảnh sân {MaSan} chưa xác định kết quả; cần kiểm tra tệp mồ côi.", maSan);
            throw;
        }
        return new(MaLoiAnh.ThanhCong, !await donDepAnh.XoaNeuKhongThamChieuAsync(anhCu));
    }
}

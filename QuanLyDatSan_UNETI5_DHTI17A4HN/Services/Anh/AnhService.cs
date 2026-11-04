// M1: Trần Trọng Tùng - 23103100202. Điều phối ảnh loại sân, quyền và phiên bản; UoW chỉ lưu DB.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;

public sealed class AnhService(IAnhRepository anh, ILoaiSanRepository loaiSan, IDonViCongViec uow,
    ITaiKhoanHienTai taiKhoan, IKhoAnh kho, IDonDepAnh donDep, ILogger<AnhService> logger) : IAnhService
{
    public async Task<Stream?> DocAsync(NhomAnh nhom, int id, CancellationToken ct)
    {
        if (id <= 0 || !Enum.IsDefined(nhom)) return null;
        var thongTin = await anh.LayAsync(nhom, id, ct);
        var duocXemAn = taiKhoan.MaTaiKhoan.HasValue && (taiKhoan.VaiTro == VaiTro.Admin
            || (nhom == NhomAnh.San && taiKhoan.VaiTro == VaiTro.NhanVien));
        if (thongTin?.Khoa is not { } khoa || (!thongTin.CongKhai && !duocXemAn)) return null;
        try { return await kho.MoDocAsync(khoa, ct); }
        catch (LoiAnhException) { return null; } // Cùng 404 với không ảnh/không đủ quyền.
    }

    public async Task<KetQuaAnh> CapNhatLoaiSanAsync(int id, YeuCauAnh yeuCau, CancellationToken ct)
    {
        if (!taiKhoan.MaTaiKhoan.HasValue || taiKhoan.VaiTro != VaiTro.Admin) return new(MaLoiAnh.KhongDuQuyen);
        var entity = await loaiSan.LayDeCapNhatAsync(id, ct);
        if (entity is null) return new(MaLoiAnh.KhongTonTai);
        if (!Enum.IsDefined(yeuCau.ThaoTac) || (yeuCau.ThaoTac == ThaoTacAnh.ThayAnh) != (yeuCau.Tep is not null))
            return new(MaLoiAnh.DuLieuAnhKhongHopLe);
        var cu = entity.DuongDanAnh;
        if (!string.Equals(cu, yeuCau.AnhCu, StringComparison.Ordinal)) return new(MaLoiAnh.XungDotAnh);
        if (yeuCau.ThaoTac == ThaoTacAnh.GoAnh && cu is null) return new(MaLoiAnh.ThanhCong);
        IDisposable khoa;
        try { khoa = await kho.GiuKhoaAsync(baoTri: false, ct); }
        catch (LoiAnhException ex) { return new(ex.MaLoi); }
        using var giuDenSauCommit = khoa;
        string? moi = null;
        try
        {
            // Hoàn tất file trước khi mở transaction; chỉ thay cột ảnh của entity tracking.
            if (yeuCau.Tep is not null) moi = await kho.LuuAsync(NhomAnh.LoaiSan, yeuCau.Tep, ct);
        }
        catch (LoiAnhException ex) { return new(ex.MaLoi); }
        entity.DuongDanAnh = moi;
        try { await uow.LuuAsync(ct); }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            await donDep.XoaNeuKhongThamChieuAsync(moi);
            return new(MaLoiAnh.XungDotAnh);
        }
        catch
        {
            // Kết quả commit chưa rõ (kể cả hủy request/dispose): giữ file mới để tránh DB trỏ file mất.
            // Công cụ dọn ngoại tuyến kiểm tra lại DB sau 24 giờ; không che lỗi ngoài dự kiến.
            logger.LogWarning("Lưu ảnh loại sân {Id} chưa xác định kết quả; cần kiểm tra tệp mồ côi.", id);
            throw;
        }
        return new(MaLoiAnh.ThanhCong, !await donDep.XoaNeuKhongThamChieuAsync(cu));
    }
}

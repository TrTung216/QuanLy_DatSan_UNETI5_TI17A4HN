// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách CRUD, validation và bảo toàn tham chiếu loại sân.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;

public class LoaiSanService(ILoaiSanRepository repository, IDonViCongViec donViCongViec) : ILoaiSanService
{
    public Task<TrangLoaiSan> LayDanhSachAsync(string? tuKhoa, TrangThaiLoaiSan? trangThai, string sapXep, int trang, CancellationToken cancellationToken) =>
        repository.LayDanhSachAsync(tuKhoa?.Trim(), trangThai, sapXep, trang, cancellationToken);

    public Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken) => repository.LayChiTietAsync(id, cancellationToken);

    public async Task<KetQuaLoaiSan> TaoAsync(GhiLoaiSan yeuCau, CancellationToken cancellationToken)
    {
        if (!HopLe(yeuCau)) return new(MaLoiLoaiSan.DuLieuKhongHopLe);
        if (await TrungTenAsync(yeuCau.TenLoai, null, cancellationToken)) return new(MaLoiLoaiSan.TrungTen);
        var loaiSan = new LoaiSan();
        GanThongTin(loaiSan, yeuCau);
        repository.Them(loaiSan);
        return await LuuAsync(loaiSan, cancellationToken);
    }

    public async Task<KetQuaLoaiSan> SuaAsync(int id, GhiLoaiSan yeuCau, CancellationToken cancellationToken)
    {
        var loaiSan = await repository.LayDeCapNhatAsync(id, cancellationToken);
        if (loaiSan is null) return new(MaLoiLoaiSan.KhongTonTai);
        if (!HopLe(yeuCau)) return new(MaLoiLoaiSan.DuLieuKhongHopLe);
        if (await TrungTenAsync(yeuCau.TenLoai, id, cancellationToken)) return new(MaLoiLoaiSan.TrungTen);
        // Không cập nhật giá hoặc trạng thái các sân đang tham chiếu.
        GanThongTin(loaiSan, yeuCau);
        return await LuuAsync(loaiSan, cancellationToken);
    }

    public async Task<KetQuaLoaiSan> XoaAsync(int id, CancellationToken cancellationToken)
    {
        var loaiSan = await repository.LayDeCapNhatAsync(id, cancellationToken);
        if (loaiSan is null) return new(MaLoiLoaiSan.KhongTonTai);
        if (await repository.CoSanThamChieuAsync(id, cancellationToken))
            return new(MaLoiLoaiSan.DangDuocThamChieu);
        repository.Xoa(loaiSan);
        try
        {
            // FK NoAction vẫn bảo vệ tham chiếu phát sinh sau AnyAsync.
            await donViCongViec.LuuAsync(cancellationToken);
            return new(MaLoiLoaiSan.ThanhCong, id);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.LoaiSanDangDuocThamChieu)
        {
            return new(MaLoiLoaiSan.DangDuocThamChieu);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan) { return new(MaLoiLoaiSan.KhongTonTai); }
    }

    private Task<bool> TrungTenAsync(string ten, int? id, CancellationToken cancellationToken)
    {
        ten = ten.Trim();
        // Dùng cùng collation và unique index hiện có tại database.
        return repository.TrungTenAsync(ten, id, cancellationToken);
    }

    private async Task<KetQuaLoaiSan> LuuAsync(LoaiSan loaiSan, CancellationToken cancellationToken)
    {
        try
        {
            await donViCongViec.LuuAsync(cancellationToken);
            return new(MaLoiLoaiSan.ThanhCong, loaiSan.MaLoaiSan);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.TrungTenLoaiSan)
        {
            return new(MaLoiLoaiSan.TrungTen);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan) { return new(MaLoiLoaiSan.XungDotDuLieu); }
    }

    private static bool HopLe(GhiLoaiSan x) => !string.IsNullOrWhiteSpace(x.TenLoai) && x.TenLoai.Length <= 100
        && (x.MoTa is null || x.MoTa.Length <= 1000) && x.SoNguoiToiDa > 0
        && x.DonGiaTheoGio is >= 0 and <= 9999999999999999m
        && x.DonGiaTheoGio == decimal.Truncate(x.DonGiaTheoGio) && Enum.IsDefined(x.TrangThai);

    private static void GanThongTin(LoaiSan loaiSan, GhiLoaiSan x)
    {
        loaiSan.TenLoai = x.TenLoai.Trim();
        loaiSan.MoTa = string.IsNullOrWhiteSpace(x.MoTa) ? null : x.MoTa.Trim();
        loaiSan.SoNguoiToiDa = x.SoNguoiToiDa;
        loaiSan.DonGiaTheoGio = x.DonGiaTheoGio;
        loaiSan.TrangThai = x.TrangThai;
    }

}

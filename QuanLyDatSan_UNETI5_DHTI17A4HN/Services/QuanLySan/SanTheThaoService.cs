// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// Phan Giang Tâm - 23103100196. M2: Truy vấn, kiểm tra và lưu sân theo quy ước service.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

public class SanTheThaoService(ISanTheThaoRepository repository, IDonViCongViec donViCongViec, ITaiKhoanHienTai taiKhoanHienTai) : ISanTheThaoService
{

    private bool ChiHoatDong => taiKhoanHienTai.VaiTro is not (VaiTro.Admin or VaiTro.NhanVien);
    public async Task<KetQuaSan<TrangSanDoc>> LayDanhSachAsync(TraCuuSan dieuKien, CancellationToken cancellationToken)
    {
        if (!TraCuuHopLe(dieuKien)) return KetQuaSan<TrangSanDoc>.Loi(MaLoiSan.DuLieuKhongHopLe);
        return KetQuaSan<TrangSanDoc>.Dat(await repository.LayDanhSachAsync(dieuKien, ChiHoatDong, cancellationToken));
    }
    public Task<IReadOnlyList<LoaiSanLoc>> LayLoaiSanLocAsync(CancellationToken cancellationToken) =>
        repository.LayLoaiSanLocAsync(ChiHoatDong, cancellationToken);
    private static bool TraCuuHopLe(TraCuuSan x)
        => (x.TuKhoa?.Length ?? 0) <= 100 && (x.MaLoaiSan is null || x.MaLoaiSan > 0)
        && (!x.TrangThai.HasValue || Enum.IsDefined(x.TrangThai.Value))
        && (!x.GiaTu.HasValue || GiaHopLe(x.GiaTu.Value))
        && (!x.GiaDen.HasValue || GiaHopLe(x.GiaDen.Value))
        && (!x.GiaTu.HasValue || !x.GiaDen.HasValue || x.GiaTu <= x.GiaDen)
        && x.SapXep is "ten_tang" or "ten_giam" or "gia_tang" or "gia_giam" or "gio_tang" or "gio_giam";

    public Task<SanDoc?> LayChiTietAsync(int maSan, CancellationToken cancellationToken) =>
        repository.LayChiTietAsync(maSan, ChiHoatDong, cancellationToken);
    public async Task<IReadOnlyList<LoaiSanLuaChon>> LayLuaChonAsync(int? maLoaiSanHienTai, CancellationToken cancellationToken)
    {
        var cacLoai = await repository.LayLoaiSanLuaChonAsync(maLoaiSanHienTai, cancellationToken);
        return cacLoai.Select(x =>
        {
            var gia = GiaThueSan.GiaGoiY(x.TenLoai, x.DonGiaTheoGio);
            return new LoaiSanLuaChon(x.MaLoaiSan, x.TenLoai, x.TrangThai == TrangThaiLoaiSan.HoatDong,
                gia.Thuong, gia.CaoDiem, gia.CuoiTuan);
        }).ToList();
    }

    public Task<bool> KiemTraLoaiSanAsync(int? maLoaiSan, int? maLoaiSanHienTai, CancellationToken cancellationToken) =>
        repository.KiemTraLoaiSanAsync(maLoaiSan, maLoaiSanHienTai, cancellationToken);
    public async Task<KetQuaSan<int>> TaoAsync(GhiSan yeuCau, CancellationToken cancellationToken)
    {
        if (!DuLieuHopLe(yeuCau)) return KetQuaSan<int>.Loi(MaLoiSan.DuLieuKhongHopLe);
        if (!await KiemTraLoaiSanAsync(yeuCau.MaLoaiSan, null, cancellationToken))
            return KetQuaSan<int>.Loi(MaLoiSan.LoaiSanKhongHopLe);
        var san = new SanTheThao();
        GanDuLieu(san, yeuCau);
        repository.Them(san);
        try { await donViCongViec.LuuAsync(cancellationToken); }
        catch (LoiLuuTruException ex) when (ex.MaLoi is MaLoiLuuTru.LoaiSanDangDuocThamChieu or MaLoiLuuTru.ThamChieuSan)
        {
            return KetQuaSan<int>.Loi(MaLoiSan.DuLieuLienQuanThayDoi);
        }
        return KetQuaSan<int>.Dat(san.MaSan);
    }

    public async Task<KetQuaSan<int>> SuaAsync(int maSan, GhiSan yeuCau, CancellationToken cancellationToken)
    {
        var san = await repository.LayDeCapNhatAsync(maSan, cancellationToken);
        if (san is null) return KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai);
        if (!DuLieuHopLe(yeuCau)) return KetQuaSan<int>.Loi(MaLoiSan.DuLieuKhongHopLe);
        if (!await KiemTraLoaiSanAsync(yeuCau.MaLoaiSan, san.MaLoaiSan, cancellationToken))
            return KetQuaSan<int>.Loi(MaLoiSan.LoaiSanKhongHopLe);
        GanDuLieu(san, yeuCau);
        try { await donViCongViec.LuuAsync(cancellationToken); }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            return KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi is MaLoiLuuTru.LoaiSanDangDuocThamChieu or MaLoiLuuTru.ThamChieuSan)
        {
            return KetQuaSan<int>.Loi(MaLoiSan.DuLieuLienQuanThayDoi);
        }
        return KetQuaSan<int>.Dat(san.MaSan);
    }

    public async Task<KetQuaSan<int>> XoaAsync(int maSan, CancellationToken cancellationToken)
    {
        var san = await repository.LayDeCapNhatAsync(maSan, cancellationToken);
        if (san is null) return KetQuaSan<int>.Loi(MaLoiSan.KhongTonTai);
        repository.Xoa(san);
        try { await donViCongViec.LuuAsync(cancellationToken); }
        catch (LoiLuuTruException ex) when (ex.MaLoi is MaLoiLuuTru.LoaiSanDangDuocThamChieu or MaLoiLuuTru.ThamChieuSan)
        {
            return KetQuaSan<int>.Loi(MaLoiSan.DangDuocThamChieu);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.XungDotPhienBan)
        {
            return KetQuaSan<int>.Loi(MaLoiSan.DaBiXoa);
        }
        return KetQuaSan<int>.Dat(maSan);
    }

    public async Task<KetQuaSan<decimal>> TinhGiaAsync(int maSan, DateOnly ngay,
        TimeOnly batDau, TimeOnly ketThuc, CancellationToken cancellationToken)
    {
        var san = await repository.LayGiaAsync(maSan, ChiHoatDong, cancellationToken);
        if (san is null) return KetQuaSan<decimal>.Loi(MaLoiSan.KhongTonTai);
        try { return KetQuaSan<decimal>.Dat(GiaThueSan.TinhTien(san, ngay, batDau, ketThuc)); }
        catch (ArgumentException) { return KetQuaSan<decimal>.Loi(MaLoiSan.GioThueKhongHopLe); }
    }

    private static bool DuLieuHopLe(GhiSan x)
        => !string.IsNullOrWhiteSpace(x.TenSan) && x.TenSan.Length <= 100
        && !string.IsNullOrWhiteSpace(x.DiaChi) && x.DiaChi.Length <= 255
        && (x.TienIch?.Length ?? 0) <= 1000 && (x.GhiChu?.Length ?? 0) <= 1000
        && x.MaLoaiSan > 0 && Enum.IsDefined(x.TrangThai) && x.GioDongCua > x.GioMoCua
        && GiaHopLe(x.DonGia) && GiaHopLe(x.DonGiaCaoDiem) && GiaHopLe(x.DonGiaCuoiTuan);

    private static bool GiaHopLe(decimal gia)
        => gia >= 0 && gia <= 9999999999999999.99m && gia == decimal.Truncate(gia);

    private static void GanDuLieu(SanTheThao san, GhiSan x)
    {
        san.TenSan = x.TenSan.Trim();
        san.MaLoaiSan = x.MaLoaiSan;
        san.DiaChi = x.DiaChi.Trim();
        san.TienIch = x.TienIch?.Trim();
        san.DonGia = x.DonGia;
        san.DonGiaCaoDiem = x.DonGiaCaoDiem;
        san.DonGiaCuoiTuan = x.DonGiaCuoiTuan;
        san.TrangThai = x.TrangThai;
        san.GioMoCua = x.GioMoCua;
        san.GioDongCua = x.GioDongCua;
        san.NgayBaoTri = x.NgayBaoTri;
        san.GhiChu = x.GhiChu?.Trim();
    }

}

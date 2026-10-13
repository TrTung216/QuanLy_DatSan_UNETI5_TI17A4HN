// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ hợp đồng nghiệp vụ loại sân theo quy ước 1.3.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;

public record GhiLoaiSan(string TenLoai, string? MoTa, int SoNguoiToiDa, decimal DonGiaTheoGio, TrangThaiLoaiSan TrangThai);
public record LoaiSanDoc(int MaLoaiSan, string TenLoai, string? MoTa, int SoNguoiToiDa,
    decimal DonGiaTheoGio, TrangThaiLoaiSan TrangThai, int SoSan);
public record TrangLoaiSan(IReadOnlyList<LoaiSanDoc> DanhSach, int Trang, int TongTrang, int TongSo);
public enum MaLoiLoaiSan { ThanhCong, KhongTonTai, TrungTen, DangDuocThamChieu, XungDotDuLieu, DuLieuKhongHopLe }
public record KetQuaLoaiSan(MaLoiLoaiSan MaLoi, int? MaLoaiSan = null);

// Người gọi: LoaiSanController (Admin). null khi không có bản ghi; kết quả ghi không chứa thông báo MVC.
public interface ILoaiSanService
{
    Task<TrangLoaiSan> LayDanhSachAsync(string? tuKhoa, TrangThaiLoaiSan? trangThai, string sapXep, int trang, CancellationToken cancellationToken);
    Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken);
    Task<KetQuaLoaiSan> TaoAsync(GhiLoaiSan yeuCau, CancellationToken cancellationToken);
    Task<KetQuaLoaiSan> SuaAsync(int id, GhiLoaiSan yeuCau, CancellationToken cancellationToken);
    Task<KetQuaLoaiSan> XoaAsync(int id, CancellationToken cancellationToken);
}

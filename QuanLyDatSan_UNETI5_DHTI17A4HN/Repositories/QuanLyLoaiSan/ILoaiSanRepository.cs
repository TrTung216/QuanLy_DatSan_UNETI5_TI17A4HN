// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ tách truy cập dữ liệu, giữ hợp đồng nghiệp vụ.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyLoaiSan;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyLoaiSan;

public interface ILoaiSanRepository
{
    Task<TrangLoaiSan> LayDanhSachAsync(string? tuKhoa, TrangThaiLoaiSan? trangThai, string sapXep, int trang, CancellationToken cancellationToken);
    Task<LoaiSanDoc?> LayChiTietAsync(int id, CancellationToken cancellationToken);
    Task<LoaiSan?> LayDeCapNhatAsync(int id, CancellationToken ct);
    Task<bool> TrungTenAsync(string ten, int? id, CancellationToken ct);
    Task<bool> CoSanThamChieuAsync(int id, CancellationToken ct);
    void Them(LoaiSan loaiSan);
    void Xoa(LoaiSan loaiSan);
}

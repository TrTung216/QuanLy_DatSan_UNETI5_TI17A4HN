// Codex: hợp đồng truy cập dữ liệu, không lộ kiểu EF.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan;

public interface ISanTheThaoRepository
{
    Task<TrangSanDoc> LayDanhSachAsync(TraCuuSan dieuKien, bool chiHoatDong, CancellationToken cancellationToken);
    Task<IReadOnlyList<LoaiSanLoc>> LayLoaiSanLocAsync(bool chiHoatDong, CancellationToken cancellationToken);
    Task<SanDoc?> LayChiTietAsync(int maSan, bool chiHoatDong, CancellationToken cancellationToken);
    Task<List<LoaiSan>> LayLoaiSanLuaChonAsync(int? maLoaiSanHienTai, CancellationToken cancellationToken);
    Task<bool> KiemTraLoaiSanAsync(int? maLoaiSan, int? maLoaiSanHienTai, CancellationToken cancellationToken);
    Task<SanTheThao?> LayGiaAsync(int maSan, bool chiHoatDong, CancellationToken cancellationToken);
    Task<SanTheThao?> LayDeCapNhatAsync(int maSan, CancellationToken cancellationToken);
    void Them(SanTheThao san);
    void Xoa(SanTheThao san);
}

// M5: Nguyễn Hữu Quang; MSSV: 23103100184

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

public record DanhSachDichVu(List<DichVu> DanhSach, string? TuKhoa, int Trang, int TongTrang);

public interface IDichVuService
{
    Task<DanhSachDichVu> LayDanhSachAsync(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep, int trang,
        CancellationToken cancellationToken);

    Task<DichVu?> LayChiTietAsync(int maDichVu, CancellationToken cancellationToken);

    Task<KetQuaNghiepVu> TaoAsync(string tenDichVu, string donViTinh, decimal donGia, string? moTa,
        CancellationToken cancellationToken);

    Task<KetQuaNghiepVu> SuaAsync(int maDichVu, string tenDichVu, string donViTinh, decimal donGia, string? moTa,
        CancellationToken cancellationToken);

    Task<KetQuaNghiepVu> DoiTrangThaiAsync(int maDichVu, CancellationToken cancellationToken);
}
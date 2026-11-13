// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng truy cập dữ liệu danh mục dịch vụ.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVu;

public interface IDichVuRepository
{
    Task<DanhSachDichVu> LayDanhSachAsync(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep, int trang, CancellationToken cancellationToken);
    Task<DichVuDoc?> LayChiTietAsync(int maDichVu, CancellationToken cancellationToken);
    Task<DichVu?> LayDeCapNhatAsync(int maDichVu, CancellationToken cancellationToken);
    Task<TrangThaiDichVu?> LayTrangThaiTenTrungAsync(string tenDichVu, int? maDichVuNgoaiTru, CancellationToken cancellationToken);
    void Them(DichVu dichVu);
}

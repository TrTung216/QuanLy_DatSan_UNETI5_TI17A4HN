// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Lọc, phân trang và đọc chi tiết đơn quản lý; tính tiền dự kiến qua M5.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    public async Task<DanhSachDonQuanLyDto> LayDanhSachQuanLyAsync(LocDonQuanLy loc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(loc);
        if (loc.TuNgay > loc.DenNgay || loc.MaLoaiSan <= 0 || loc.TuKhoa?.Length > 200
            || !Enum.IsDefined(loc.SapXep)
            || (loc.TrangThai.HasValue && !Enum.IsDefined(loc.TrangThai.Value)))
            throw new ArgumentException("Bộ lọc đơn quản lý không hợp lệ.", nameof(loc));

        return await repository.LayDanhSachQuanLyAsync(loc with { TuKhoa = loc.TuKhoa?.Trim() }, cancellationToken);
    }

    public async Task<ChiTietDonQuanLyDto?> LayChiTietQuanLyAsync(int id, CancellationToken cancellationToken)
    {
        var duLieu = await repository.LayChiTietQuanLyAsync(id, cancellationToken);
        if (duLieu is null) return null;
        var result = duLieu.ChiTiet;
        result.Don.TienSanDuKien = tinhTien.TinhTienSan(result.Don.DonGia, result.Don.GioBatDau, result.Don.GioKetThuc);
        result.Don.TienDichVuDuKien = tinhTien.TinhTienDichVu(duLieu.DichVu);
        result.Don.TongTienDuKien = tinhTien.TinhTongTien(result.Don.TienSanDuKien, result.Don.TienDichVuDuKien);
        return result;
    }

    public Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken) =>
        repository.LayLoaiSanLocDonAsync(cancellationToken);
}

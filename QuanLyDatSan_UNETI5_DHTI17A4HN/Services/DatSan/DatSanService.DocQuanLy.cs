// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Lọc, phân trang và đọc chi tiết đơn quản lý; tính tiền dự kiến qua M5.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Nguồn tiền hiển thị thống nhất; giữ snapshot khi hoàn thành.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

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
        var don = result.Don;
        try
        {
            var daHoanThanh = don.TrangThai == TrangThaiDatSan.HoanThanh;
            if (daHoanThanh)
            {
                if (don.TienSan is not >= 0m || don.TienDichVu is not >= 0m || !don.TongTien.HasValue
                    || tinhTien.TinhTongTien(don.TienSan.Value, don.TienDichVu.Value) != don.TongTien.Value)
                    return result with { LoiTien = MaLoiCoc.DuLieuTienKhongHopLe };
                don.TienSanDuKien = don.TienSan.Value;
                don.TienDichVuDuKien = don.TienDichVu.Value;
                don.TongTienDuKien = don.TongTien.Value;
            }
            else
            {
                don.TienSanDuKien = tinhTien.TinhTienSan(don.DonGia, don.GioBatDau, don.GioKetThuc);
                don.TienDichVuDuKien = tinhTien.TinhTienDichVu(duLieu.DichVu);
                don.TongTienDuKien = tinhTien.TinhTongTien(don.TienSanDuKien, don.TienDichVuDuKien);
            }
            var conLai = tinhTien.TinhConPhaiThu(don.TongTien ?? don.TongTienDuKien, don.TienCoc);
            return result with
            {
                TienCocToiThieu = tinhTien.TinhTienCocYeuCau(don.TienSanDuKien),
                ConPhaiThu = daHoanThanh ? 0m : conLai
            };
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            return result with { LoiTien = MaLoiCoc.DuLieuTienKhongHopLe };
        }
    }

    public Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken) =>
        repository.LayLoaiSanLocDonAsync(cancellationToken);
}

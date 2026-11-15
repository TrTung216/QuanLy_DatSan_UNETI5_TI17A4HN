// M1: Trần Trọng Tùng; MSSV: 23103100202. Tích hợp cọc, dịch vụ cá nhân và kiểm tra tiền trước nghiệm thu.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Tra cứu đơn theo chủ tài khoản và danh sách sân cho form đặt sân.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    public Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai,
        int trang, CancellationToken cancellationToken)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;
        return repository.LayDanhSachCaNhanAsync(maTaiKhoan, trangThai, trang, cancellationToken);
    }
    public async Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken)
    {
        var don = await repository.LayChiTietCaNhanAsync(maTaiKhoan, id, cancellationToken);
        if (don is not null)
        {
            don.TienSanDuKien = don.TrangThai == TrangThaiDatSan.HoanThanh && don.TienSan.HasValue
                ? don.TienSan.Value : tinhTien.TinhTienSan(don.DonGia, don.GioBatDau, don.GioKetThuc);
            don.DichVu = don.DichVu.Select(x => x with
            {
                ThanhTien = tinhTien.TinhTienDichVu([new(x.SoLuong, x.DonGia)])
            }).ToArray();
        }
        return don;
    }
    public Task<IReadOnlyList<SanChonDto>> LayLuaChonSanAsync(CancellationToken cancellationToken) =>
        repository.LayLuaChonSanAsync(cancellationToken);
}

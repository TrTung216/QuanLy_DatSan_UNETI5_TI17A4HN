// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng thêm/sửa/hủy dịch vụ trong đơn đặt sân.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;

public record DongDichVuTrongDon(int MaChiTiet, int MaDichVu, string TenDichVu, string DonViTinh, int SoLuong,
    decimal DonGia)
{
    public decimal ThanhTien => SoLuong * DonGia;
}

public record ChiTietDichVuTrongDon(int MaDatSan, TrangThaiDatSan TrangThai, bool ChoPhepSua, decimal TongTienDichVu,
    List<DongDichVuTrongDon> Dong, List<DichVuDoc> DichVuCoTheThem);

public interface IDichVuTrongDonService
{
    Task<ChiTietDichVuTrongDon?> LayTheoDonAsync(int maDatSan, CancellationToken cancellationToken);

    Task<KetQuaNghiepVu> ThemAsync(int maDatSan, int maDichVu, int soLuong, CancellationToken cancellationToken);

    Task<KetQuaNghiepVu> SuaSoLuongAsync(int maDatSan, int maChiTiet, int soLuong, CancellationToken cancellationToken);

    Task<KetQuaNghiepVu> HuyAsync(int maDatSan, int maChiTiet, CancellationToken cancellationToken);
}
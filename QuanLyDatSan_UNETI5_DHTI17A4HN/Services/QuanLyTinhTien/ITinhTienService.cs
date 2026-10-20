// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng tính tiền đơn đặt sân (tiền sân, tiền dịch vụ, tổng tiền).
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

// Một dòng dịch vụ trong đơn: số lượng và đơn giá đã chụp lúc thêm.
public record DongDichVu(int SoLuong, decimal DonGia);

public interface ITinhTienService
{
    decimal TinhTienSan(decimal donGia, DateTime batDau, DateTime ketThuc);

    decimal TinhTienDichVu(IEnumerable<DongDichVu> cacDong);

    decimal TinhTongTien(decimal tienSan, decimal tienDichVu);
}
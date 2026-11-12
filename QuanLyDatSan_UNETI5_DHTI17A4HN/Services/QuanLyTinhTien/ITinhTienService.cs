// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: hợp đồng tính tiền đơn đặt sân (tiền sân, tiền dịch vụ, tổng tiền).
// M1: Trần Trọng Tùng; MSSV: 23103100202. Tích hợp phương thức cọc/còn phải thu theo hợp đồng M3/M4/M5.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

// Một dòng dịch vụ trong đơn: số lượng và đơn giá đã chụp lúc thêm.
public record DongDichVu(int SoLuong, decimal DonGia);

public interface ITinhTienService
{
    decimal TinhTienSan(decimal donGia, DateTime batDau, DateTime ketThuc);

    decimal TinhTienDichVu(IEnumerable<DongDichVu> cacDong);

    decimal TinhTongTien(decimal tienSan, decimal tienDichVu);

    decimal TinhTienCocYeuCau(decimal tienSan);

    decimal TinhConPhaiThu(decimal tongTien, decimal tienCoc);
}

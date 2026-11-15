// M1: Trần Trọng Tùng; MSSV: 23103100202. Tích hợp cọc, dịch vụ cá nhân và kiểm tra tiền trước nghiệm thu.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Tính tiền qua M5 từ giá đã lưu trên đơn và dịch vụ; tổng tiền chưa trừ cọc.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    private const decimal TienToiDa = 9999999999999999m;

    private async Task<(decimal San, decimal DichVu, decimal Tong)> TinhTienDonAsync(
        DatSan don, CancellationToken cancellationToken)
    {
        var dong = await repository.LayDongTinhTienAsync(don.MaDatSan, cancellationToken);
        try
        {
            var san = tinhTien.TinhTienSan(don.DonGia, don.GioBatDau, don.GioKetThuc);
            var dichVu = tinhTien.TinhTienDichVu(dong);
            return (san, dichVu, tinhTien.TinhTongTien(san, dichVu));
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException)
        {
            // Chỉ ánh xạ lỗi công thức dự kiến; lỗi repository/SQL vẫn đi qua handler hệ thống.
            throw new LoiTienDonException(ex);
        }
    }

    private sealed class LoiTienDonException(Exception inner) : Exception("Dữ liệu tiền không hợp lệ.", inner);
}

// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-04 - tính tiền từ giá đã lưu qua service M5; giới hạn decimal(18,2).
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    private const decimal TienToiDa = 9999999999999999m;

    private async Task<(decimal San, decimal DichVu, decimal Tong)> TinhTienDonAsync(
        DatSan don, CancellationToken cancellationToken)
    {
        var dong = await db.ChiTietDatSans.AsNoTracking().Where(x => x.MaDatSan == don.MaDatSan)
            .Select(x => new DongDichVu(x.SoLuong, x.DonGia)).ToListAsync(cancellationToken);
        var san = tinhTien.TinhTienSan(don.DonGia, don.GioBatDau, don.GioKetThuc);
        var dichVu = tinhTien.TinhTienDichVu(dong);
        return (san, dichVu, tinhTien.TinhTongTien(san, dichVu));
    }
}

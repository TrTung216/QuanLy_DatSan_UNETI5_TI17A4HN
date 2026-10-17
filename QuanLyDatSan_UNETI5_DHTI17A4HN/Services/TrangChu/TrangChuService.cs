// M1: Trần Trọng Tùng; MSSV: 23103100202. Đọc sân theo loại cho trang chủ, không thay nghiệp vụ M2.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.TrangChu;

public class TrangChuService(ApplicationDbContext db) : ITrangChuService
{
    public async Task<IReadOnlyList<NhomSanTrangChu>> LaySanTheoLoaiAsync(CancellationToken cancellationToken)
    {
        // Trang công khai chỉ giới thiệu sân/loại đang hoạt động, kể cả khi Admin truy cập.
        // Giới hạn tại database; xem toàn bộ và phân trang qua màn hình tra cứu M2.
        return await db.LoaiSans.AsNoTracking()
            .Where(loai => loai.TrangThai == TrangThaiLoaiSan.HoatDong
                && loai.DanhSachSan.Any(san => san.TrangThai == TrangThaiSan.HoatDong))
            .OrderBy(loai => loai.TenLoai).ThenBy(loai => loai.MaLoaiSan)
            .Select(loai => new NhomSanTrangChu(loai.MaLoaiSan, loai.TenLoai,
                loai.DanhSachSan.Where(san => san.TrangThai == TrangThaiSan.HoatDong)
                    .OrderBy(san => san.TenSan).ThenBy(san => san.MaSan).Take(6)
                    .Select(san => new SanTrangChu(san.MaSan, san.TenSan, san.DiaChi,
                        san.DonGia, san.GioMoCua, san.GioDongCua)).ToList()))
            .ToListAsync(cancellationToken);
    }
}

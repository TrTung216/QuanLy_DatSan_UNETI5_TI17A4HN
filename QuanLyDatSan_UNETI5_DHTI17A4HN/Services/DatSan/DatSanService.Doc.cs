// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: truy vấn lịch đặt cá nhân có lọc trạng thái, sắp xếp, phân trang; lấy chi tiết đơn theo chủ sở hữu.
// Trả RowVersion để M3 gọi hủy đơn; tính tiền sân dự kiến và lấy danh sách sân đang hoạt động để tạo đơn.

using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    private const int SoDongMoiTrang = 10;
    public async Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai,
        int trang, CancellationToken cancellationToken)
    {
        if (trangThai.HasValue && !Enum.IsDefined(trangThai.Value)) trangThai = null;

        var truyVan = db.DatSans.AsNoTracking().Where(x => x.KhachHang.MaTaiKhoan == maTaiKhoan);
        if (trangThai.HasValue) truyVan = truyVan.Where(x => x.TrangThai == trangThai.Value);

        var tongSo = await truyVan.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);

        var dong = await truyVan
            .OrderByDescending(x => x.GioBatDau).ThenByDescending(x => x.MaDatSan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)
            .Select(x => new DonDatDongDto
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.SanTheThao.TenSan,
                TenLoai = x.SanTheThao.LoaiSan.TenLoai,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai
            })
            .ToListAsync(cancellationToken);

        return new DanhSachDonDto
        {
            Dong = dong, TrangThai = trangThai, Trang = trang, TongTrang = tongTrang, TongSo = tongSo
        };
    }

    public async Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken)
    {
        var don = await db.DatSans.AsNoTracking()
            .Where(x => x.MaDatSan == id && x.KhachHang.MaTaiKhoan == maTaiKhoan)
            .Select(x => new ChiTietDonDto
            {
                PhienBan = x.RowVersion,
                MaDatSan = x.MaDatSan,
                TenSan = x.SanTheThao.TenSan,
                TenLoai = x.SanTheThao.LoaiSan.TenLoai,
                DiaChi = x.SanTheThao.DiaChi,
                NgayDat = x.NgayDat,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TienCoc = x.TienCoc,
                TrangThai = x.TrangThai,
                NgayXacNhan = x.NgayXacNhan,
                NgayHuy = x.NgayHuy,
                NgayHoanThanh = x.NgayHoanThanh,
                LyDoHuy = x.LyDoHuy,
                TienSan = x.TienSan,
                TienDichVu = x.TienDichVu,
                TongTien = x.TongTien
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (don is null) return null;

        don.TienSanDuKien = DatSanService.TinhTienSanDuKien(don.DonGia, don.GioBatDau, don.GioKetThuc);
        return don;
    }

    public async Task<IReadOnlyList<SanChonDto>> LayLuaChonSanAsync(CancellationToken cancellationToken)
    {
        return await db.SanTheThaos.AsNoTracking()
            .Where(x => x.TrangThai == TrangThaiSan.HoatDong && x.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong)
            .OrderBy(x => x.LoaiSan.TenLoai).ThenBy(x => x.TenSan)
            .Select(x => new SanChonDto(x.MaSan, x.TenSan, x.LoaiSan.TenLoai, x.DonGia, x.GioMoCua, x.GioDongCua))
            .ToListAsync(cancellationToken);
    }
}

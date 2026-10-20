// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-02/03 - tìm, lọc, sắp xếp, phân trang đơn quản lý; đọc chi tiết khách/sân/tiền và RowVersion hiện tại.

using Microsoft.EntityFrameworkCore;

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

        var query = db.DatSans.AsNoTracking();
        var tuKhoa = loc.TuKhoa?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
            query = query.Where(x => x.KhachHang.HoTen.Contains(tuKhoa) || x.SanTheThao.TenSan.Contains(tuKhoa));
        if (loc.MaLoaiSan.HasValue)
            query = query.Where(x => x.SanTheThao.MaLoaiSan == loc.MaLoaiSan.Value);
        if (loc.TrangThai.HasValue)
            query = query.Where(x => x.TrangThai == loc.TrangThai.Value);
        if (loc.TuNgay.HasValue)
        {
            var tuNgay = loc.TuNgay.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.GioBatDau >= tuNgay);
        }
        if (loc.DenNgay.HasValue)
        {
            var denNgay = loc.DenNgay.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.GioBatDau.Date <= denNgay);
        }

        var tongSo = await query.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSo / (double)SoDongMoiTrang));
        var trang = Math.Clamp(loc.Trang, 1, tongTrang);
        var ordered = loc.SapXep switch
        {
            SapXepDonQuanLy.NgayTang => query.OrderBy(x => x.GioBatDau),
            SapXepDonQuanLy.KhachTang => query.OrderBy(x => x.KhachHang.HoTen),
            SapXepDonQuanLy.KhachGiam => query.OrderByDescending(x => x.KhachHang.HoTen),
            SapXepDonQuanLy.SanTang => query.OrderBy(x => x.SanTheThao.TenSan),
            SapXepDonQuanLy.SanGiam => query.OrderByDescending(x => x.SanTheThao.TenSan),
            _ => query.OrderByDescending(x => x.GioBatDau)
        };
        var dong = await ordered.ThenByDescending(x => x.MaDatSan)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang)
            .Select(x => new DonQuanLyDongDto(x.MaDatSan, x.KhachHang.HoTen,
                x.SanTheThao.TenSan, x.SanTheThao.LoaiSan.TenLoai, x.NgayDat,
                x.GioBatDau, x.GioKetThuc, x.TrangThai, x.TienCoc))
            .ToListAsync(cancellationToken);
        return new(dong, trang, tongTrang, tongSo);
    }

    public async Task<ChiTietDonQuanLyDto?> LayChiTietQuanLyAsync(int id, CancellationToken cancellationToken)
    {
        // Projection chỉ lấy dữ liệu màn hình cần; không trả entity hay thông tin đăng nhập.
        var result = await db.DatSans.AsNoTracking().Where(x => x.MaDatSan == id)
            .Select(x => new ChiTietDonQuanLyDto(new ChiTietDonDto
            {
                PhienBan = x.RowVersion,
                MaDatSan = x.MaDatSan, TenSan = x.SanTheThao.TenSan,
                TenLoai = x.SanTheThao.LoaiSan.TenLoai, DiaChi = x.SanTheThao.DiaChi,
                NgayDat = x.NgayDat, GioBatDau = x.GioBatDau, GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia, TienCoc = x.TienCoc, TrangThai = x.TrangThai,
                NgayXacNhan = x.NgayXacNhan, NgayHuy = x.NgayHuy, NgayHoanThanh = x.NgayHoanThanh,
                LyDoHuy = x.LyDoHuy, TienSan = x.TienSan, TienDichVu = x.TienDichVu, TongTien = x.TongTien
            }, x.MaKhachHang, x.KhachHang.HoTen, x.KhachHang.SoDienThoai, x.KhachHang.Email))
            .SingleOrDefaultAsync(cancellationToken);
        if (result is not null)
            result.Don.TienSanDuKien = TinhTienSanDuKien(result.Don.DonGia, result.Don.GioBatDau, result.Don.GioKetThuc);
        return result;
    }

    public async Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken) =>
        // Bao gồm loại sân ngừng hoạt động để tra cứu được lịch sử.
        await db.LoaiSans.AsNoTracking().OrderBy(x => x.TenLoai).ThenBy(x => x.MaLoaiSan)
            .Select(x => new LoaiSanLocDto(x.MaLoaiSan, x.TenLoai)).ToListAsync(cancellationToken);
}

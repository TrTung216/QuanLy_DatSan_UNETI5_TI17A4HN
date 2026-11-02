// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Truy vấn đơn, kiểm tra trùng lịch và khóa sân; việc lưu do đơn vị công việc quản lý.

using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;

public class DatSanRepository(ApplicationDbContext db) : IDatSanRepository
{
    private const int SoDongMoiTrang = 10;
    public async Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai,
        int trang, CancellationToken cancellationToken)
    {

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
            Dong = dong,
            TrangThai = trangThai,
            Trang = trang,
            TongTrang = tongTrang,
            TongSo = tongSo
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
    public async Task<DanhSachDonQuanLyDto> LayDanhSachQuanLyAsync(LocDonQuanLy loc,
        CancellationToken cancellationToken)
    {
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

    public async Task<DuLieuChiTietDonQuanLy?> LayChiTietQuanLyAsync(int id, CancellationToken cancellationToken)
    {
        var duLieu = await db.DatSans.AsNoTracking().Where(x => x.MaDatSan == id)
            .Select(x => new DuLieuChiTietDonQuanLy(new ChiTietDonQuanLyDto(new ChiTietDonDto
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
            }, x.MaKhachHang, x.KhachHang.HoTen, x.KhachHang.SoDienThoai, x.KhachHang.Email),
                x.ChiTietDatSans.Select(d => new DongDichVu(d.SoLuong, d.DonGia)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        if (duLieu is null) return null;
        return duLieu;
    }

    public async Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken) =>
        await db.LoaiSans.AsNoTracking().OrderBy(x => x.TenLoai).ThenBy(x => x.MaLoaiSan)
            .Select(x => new LoaiSanLocDto(x.MaLoaiSan, x.TenLoai)).ToListAsync(cancellationToken);
    private IQueryable<DatSan> DonTrungLich(DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan) =>
        db.DatSans.AsNoTracking().Where(x =>
            x.TrangThai != TrangThaiDatSan.DaHuy && x.TrangThai != TrangThaiDatSan.TuChoi
            && (boQuaMaDatSan == null || x.MaDatSan != boQuaMaDatSan)
            && batDau < x.GioKetThuc && ketThuc > x.GioBatDau);

    private IQueryable<DuLieuKhaDungSan> TruyVanKhaDungSan(
        DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan)
    {
        var donTrung = DonTrungLich(batDau, ketThuc, boQuaMaDatSan);
        return db.SanTheThaos.AsNoTracking().Select(san => new DuLieuKhaDungSan
        {
            MaSan = san.MaSan,
            DonGia = san.DonGia,
            HoatDong = san.TrangThai == TrangThaiSan.HoatDong
                && san.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong,
            GioMoCua = san.GioMoCua,
            GioDongCua = san.GioDongCua,
            NgayBaoTri = san.NgayBaoTri,
            TrungLich = donTrung.Any(don => don.MaSan == san.MaSan)
        });
    }

    public Task<bool> KhachDuDieuKienAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
        db.KhachHangs.AsNoTracking().AnyAsync(x => x.MaTaiKhoan == maTaiKhoan
            && x.TrangThai == TrangThaiKhachHang.HoatDong
            && x.TaiKhoan.TrangThai == TrangThaiTaiKhoan.HoatDong
            && x.TaiKhoan.VaiTro == VaiTro.KhachHang, cancellationToken);

    public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT 1 FROM SanTheThao WITH (UPDLOCK, ROWLOCK) WHERE MaSan = {maSan}", cancellationToken);

    public Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken) => DonTrungLich(batDau, ketThuc, boQuaMaDatSan)
            .AnyAsync(x => x.MaSan == maSan, cancellationToken);
    public Task<KhachHang?> LayKhachTaoDonAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
        db.KhachHangs.AsNoTracking().Include(x => x.TaiKhoan)
            .SingleOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan, cancellationToken);
    public Task<DuLieuKhaDungSan?> LayKhaDungAsync(int maSan, DateTime batDau, DateTime ketThuc,
        int? boQuaMaDatSan, CancellationToken cancellationToken) =>
        TruyVanKhaDungSan(batDau, ketThuc, boQuaMaDatSan).SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
    public Task<Dictionary<int, DuLieuKhaDungSan>> LayKhaDungTheoLoAsync(int[] ids, DateTime batDau, DateTime ketThuc,
        CancellationToken cancellationToken) => TruyVanKhaDungSan(batDau, ketThuc, null)
            .Where(x => ids.Contains(x.MaSan)).ToDictionaryAsync(x => x.MaSan, cancellationToken);
    public Task<int?> LayMaSanCuaDonAsync(int maDatSan, int? maTaiKhoanChuDon, CancellationToken cancellationToken) =>
        db.DatSans.Where(x => x.MaDatSan == maDatSan && (!maTaiKhoanChuDon.HasValue
            || x.KhachHang.MaTaiKhoan == maTaiKhoanChuDon.Value)).Select(x => (int?)x.MaSan).SingleOrDefaultAsync(cancellationToken);
    public async Task<DatSan?> LayDonDeCapNhatAsync(int maDatSan, int? maTaiKhoanChuDon, CancellationToken cancellationToken)
    {
        var cu = db.DatSans.Local.SingleOrDefault(x => x.MaDatSan == maDatSan);
        if (cu is not null) db.Entry(cu).State = EntityState.Detached;
        return await db.DatSans.Include(x => x.SanTheThao).ThenInclude(x => x.LoaiSan).Include(x => x.KhachHang)
            .SingleOrDefaultAsync(x => x.MaDatSan == maDatSan && (!maTaiKhoanChuDon.HasValue
                || x.KhachHang.MaTaiKhoan == maTaiKhoanChuDon.Value), cancellationToken);
    }
    public Task<List<DongDichVu>> LayDongTinhTienAsync(int maDatSan, CancellationToken cancellationToken) =>
        db.ChiTietDatSans.AsNoTracking().Where(x => x.MaDatSan == maDatSan)
            .Select(x => new DongDichVu(x.SoLuong, x.DonGia)).ToListAsync(cancellationToken);
    public void Them(DatSan don) => db.DatSans.Add(don);
}

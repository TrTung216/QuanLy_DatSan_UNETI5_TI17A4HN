// M5: Nguyễn Hữu Quang; MSSV: 23103100184

using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

public class DichVuService(ApplicationDbContext db) : IDichVuService
{
    private const int SoDongMoiTrang = 10;

    public async Task<DanhSachDichVu> LayDanhSachAsync(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep,
        int trang, CancellationToken cancellationToken)
    {
        var truyVan = db.DichVus.AsNoTracking().AsQueryable();

        tuKhoa = tuKhoa?.Trim();
        if (!string.IsNullOrEmpty(tuKhoa))
            truyVan = truyVan.Where(x => x.TenDichVu.Contains(tuKhoa));
        if (trangThai.HasValue)
            truyVan = truyVan.Where(x => x.TrangThai == trangThai.Value);

        IOrderedQueryable<DichVu> sapXepTruyVan = sapXep switch
        {
            "ten_giam" => truyVan.OrderByDescending(x => x.TenDichVu),
            "gia_tang" => truyVan.OrderBy(x => x.DonGia),
            "gia_giam" => truyVan.OrderByDescending(x => x.DonGia),
            _ => truyVan.OrderBy(x => x.TenDichVu)
        };

        var tongSoDong = await sapXepTruyVan.CountAsync(cancellationToken);
        var tongTrang = Math.Max(1, (int)Math.Ceiling(tongSoDong / (double)SoDongMoiTrang));
        trang = Math.Clamp(trang, 1, tongTrang);

        var danhSach = await sapXepTruyVan.ThenBy(x => x.MaDichVu)
            .Skip((trang - 1) * SoDongMoiTrang).Take(SoDongMoiTrang).ToListAsync(cancellationToken);
        return new DanhSachDichVu(danhSach, tuKhoa, trang, tongTrang);
    }

    public Task<DichVu?> LayChiTietAsync(int maDichVu, CancellationToken cancellationToken)
        => db.DichVus.AsNoTracking().SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);

    public async Task<KetQuaNghiepVu> TaoAsync(string tenDichVu, string donViTinh, decimal donGia, string? moTa,
        CancellationToken cancellationToken)
    {
        var loiDonGia = KiemTraDonGia(donGia);
        if (loiDonGia is not null) return loiDonGia;

        db.DichVus.Add(new DichVu
        {
            TenDichVu = tenDichVu,
            DonViTinh = donViTinh,
            DonGia = donGia,
            MoTa = ChuanHoaMoTa(moTa),
            TrangThai = TrangThaiDichVu.HoatDong // do server quyết định, không nhận từ form
        });
        await db.SaveChangesAsync(cancellationToken);

        return KetQuaNghiepVu.ThanhCongVoi("Đã thêm dịch vụ.");
    }

    public async Task<KetQuaNghiepVu> SuaAsync(int maDichVu, string tenDichVu, string donViTinh, decimal donGia,
        string? moTa, CancellationToken cancellationToken)
    {
        var loiDonGia = KiemTraDonGia(donGia);
        if (loiDonGia is not null) return loiDonGia;

        var dichVu = await db.DichVus.SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.KhongThay();

        // Chỉ ảnh hưởng đơn mới: ChiTietDatSan sẽ lưu bản chụp DonGia lúc thêm.
        dichVu.TenDichVu = tenDichVu;
        dichVu.DonViTinh = donViTinh;
        dichVu.DonGia = donGia;
        dichVu.MoTa = ChuanHoaMoTa(moTa);
        await db.SaveChangesAsync(cancellationToken);

        return KetQuaNghiepVu.ThanhCongVoi("Đã cập nhật dịch vụ.");
    }

    // Không xóa cứng: chỉ chuyển HoatDong <-> NgungHoatDong.
    public async Task<KetQuaNghiepVu> DoiTrangThaiAsync(int maDichVu, CancellationToken cancellationToken)
    {
        var dichVu = await db.DichVus.SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.KhongThay();

        dichVu.TrangThai = dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? TrangThaiDichVu.NgungHoatDong
            : TrangThaiDichVu.HoatDong;
        await db.SaveChangesAsync(cancellationToken);

        return KetQuaNghiepVu.ThanhCongVoi(dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? "Đã kích hoạt dịch vụ." : "Đã ngừng hoạt động dịch vụ.");
    }

    // Quy ước: giá nhập theo đồng nguyên.
    private static KetQuaNghiepVu? KiemTraDonGia(decimal donGia)
        => donGia != decimal.Truncate(donGia)
            ? KetQuaNghiepVu.BaoLoi("Đơn giá phải là số đồng nguyên.", nameof(DichVu.DonGia))
            : null;

    private static string? ChuanHoaMoTa(string? moTa) => string.IsNullOrWhiteSpace(moTa) ? null : moTa;
}
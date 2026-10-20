// M5: Nguyễn Hữu Quang; MSSV: 23103100184
// Nội dung: nghiệp vụ dịch vụ trong đơn; mỗi thao tác tính lại DatSan.TienDichVu và kiểm tra RowVersion của đơn.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;

public class DichVuTrongDonService(ApplicationDbContext db, ITinhTienService tinhTien,
    ILogger<DichVuTrongDonService> logger) : IDichVuTrongDonService
{
    private const int SoLuongToiDa = 100;

    // Chỉ đơn đang chờ xử lý hoặc đã xác nhận mới được đổi dịch vụ.
    private static bool ChoPhepSua(TrangThaiDatSan trangThai)
        => trangThai is TrangThaiDatSan.ChoXuLy or TrangThaiDatSan.DangXuLy;

    public async Task<ChiTietDichVuTrongDon?> LayTheoDonAsync(int maDatSan, CancellationToken cancellationToken)
    {
        var don = await db.DatSans.AsNoTracking()
            .Where(x => x.MaDatSan == maDatSan)
            .Select(x => new { x.MaDatSan, x.TrangThai })
            .SingleOrDefaultAsync(cancellationToken);
        if (don is null) return null;

        var dong = await db.ChiTietDatSans.AsNoTracking()
            .Where(x => x.MaDatSan == maDatSan)
            .OrderBy(x => x.MaChiTiet)
            .Select(x => new DongDichVuTrongDon(x.MaChiTiet, x.MaDichVu, x.DichVu.TenDichVu, x.DichVu.DonViTinh,
                x.SoLuong, x.DonGia))
            .ToListAsync(cancellationToken);

        var choPhepSua = ChoPhepSua(don.TrangThai);
        var coTheThem = choPhepSua
            ? await db.DichVus.AsNoTracking().Where(x => x.TrangThai == TrangThaiDichVu.HoatDong)
                .OrderBy(x => x.TenDichVu).ToListAsync(cancellationToken)
            : new List<DichVu>();

        var tongTien = tinhTien.TinhTienDichVu(dong.Select(x => new DongDichVu(x.SoLuong, x.DonGia)));
        return new ChiTietDichVuTrongDon(don.MaDatSan, don.TrangThai, choPhepSua, tongTien, dong, coTheThem);
    }

    public async Task<KetQuaNghiepVu> ThemAsync(int maDatSan, int maDichVu, int soLuong,
        CancellationToken cancellationToken)
    {
        if (soLuong < 1 || soLuong > SoLuongToiDa)
            return KetQuaNghiepVu.BaoLoi($"Số lượng phải từ 1 đến {SoLuongToiDa}.", "SoLuong");

        var (don, loi) = await TaiDonAsync(maDatSan, cancellationToken);
        if (don is null) return loi!;

        var dichVu = await db.DichVus.SingleOrDefaultAsync(x => x.MaDichVu == maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.BaoLoi("Dịch vụ không tồn tại.", "MaDichVu");
        if (dichVu.TrangThai != TrangThaiDichVu.HoatDong)
            return KetQuaNghiepVu.BaoLoi("Dịch vụ đã ngừng hoạt động.", "MaDichVu");

        var dongCoSan = don.ChiTietDatSans.SingleOrDefault(x => x.MaDichVu == maDichVu);
        if (dongCoSan is null)
        {
            // Chụp giá hiện tại của dịch vụ vào dòng chi tiết.
            don.ChiTietDatSans.Add(new ChiTietDatSan { MaDichVu = maDichVu, SoLuong = soLuong, DonGia = dichVu.DonGia });
        }
        else
        {
            // Đã có dịch vụ này trong đơn: cộng dồn số lượng, giữ nguyên giá đã chụp.
            if (dongCoSan.SoLuong + soLuong > SoLuongToiDa)
                return KetQuaNghiepVu.BaoLoi($"Tổng số lượng không được vượt quá {SoLuongToiDa}.", "SoLuong");
            dongCoSan.SoLuong += soLuong;
        }

        return await LuuAsync(don, "Đã thêm dịch vụ vào đơn.", cancellationToken);
    }

    public async Task<KetQuaNghiepVu> SuaSoLuongAsync(int maDatSan, int maChiTiet, int soLuong,
        CancellationToken cancellationToken)
    {
        if (soLuong < 1 || soLuong > SoLuongToiDa)
            return KetQuaNghiepVu.BaoLoi($"Số lượng phải từ 1 đến {SoLuongToiDa}.", "SoLuong");

        var (don, loi) = await TaiDonAsync(maDatSan, cancellationToken);
        if (don is null) return loi!;

        var dong = don.ChiTietDatSans.SingleOrDefault(x => x.MaChiTiet == maChiTiet);
        if (dong is null) return KetQuaNghiepVu.KhongThay();

        dong.SoLuong = soLuong;
        return await LuuAsync(don, "Đã cập nhật số lượng dịch vụ.", cancellationToken);
    }

    // Hủy = xóa dòng chi tiết khỏi đơn.
    public async Task<KetQuaNghiepVu> HuyAsync(int maDatSan, int maChiTiet, CancellationToken cancellationToken)
    {
        var (don, loi) = await TaiDonAsync(maDatSan, cancellationToken);
        if (don is null) return loi!;

        var dong = don.ChiTietDatSans.SingleOrDefault(x => x.MaChiTiet == maChiTiet);
        if (dong is null) return KetQuaNghiepVu.KhongThay();

        db.ChiTietDatSans.Remove(dong);
        return await LuuAsync(don, "Đã hủy dịch vụ khỏi đơn.", cancellationToken);
    }

    private async Task<(DatSan? Don, KetQuaNghiepVu? Loi)> TaiDonAsync(int maDatSan,
        CancellationToken cancellationToken)
    {
        var don = await db.DatSans.Include(x => x.ChiTietDatSans)
            .SingleOrDefaultAsync(x => x.MaDatSan == maDatSan, cancellationToken);
        if (don is null) return (null, KetQuaNghiepVu.KhongThay());
        if (!ChoPhepSua(don.TrangThai))
            return (null, KetQuaNghiepVu.BaoLoi("Đơn ở trạng thái hiện tại không được sửa dịch vụ."));
        return (don, null);
    }

    private async Task<KetQuaNghiepVu> LuuAsync(DatSan don, string thongBao, CancellationToken cancellationToken)
    {
        var cacDong = don.ChiTietDatSans
            .Where(x => db.Entry(x).State != EntityState.Deleted)
            .Select(x => new DongDichVu(x.SoLuong, x.DonGia));
        don.TienDichVu = tinhTien.TinhTienDichVu(cacDong);
        // Luôn UPDATE đơn để RowVersion được kiểm tra, tránh ghi đè khi M4 đổi trạng thái cùng lúc.
        db.Entry(don).Property(x => x.TienDichVu).IsModified = true;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return KetQuaNghiepVu.BaoLoi("Đơn vừa được cập nhật bởi người khác. Vui lòng tải lại và thử lại.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không lưu được dịch vụ của đơn {MaDatSan}.", don.MaDatSan);
            return KetQuaNghiepVu.BaoLoi("Không lưu được dịch vụ. Vui lòng thử lại.");
        }
        return KetQuaNghiepVu.ThanhCongVoi(thongBao);
    }
}
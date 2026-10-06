// M3: Nguyễn Văn Quý; MSSV: 23103100181. kiểm tra nghiệp vụ, khóa theo sân và tạo đơn đặt sân; M4 dùng chung KhoaSanAsync/CoTrungLichAsync.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public record KetQuaTaoDon(int? MaDatSan, string? Loi = null, string? TruongLoi = null)
{
    public bool ThanhCong => MaDatSan.HasValue;
}

public interface IDatSanService
{
    Task<KetQuaTaoDon> TaoDonAsync(int maTaiKhoan, int maSan, DateOnly ngaySuDung, TimeOnly gioBatDau,
        TimeOnly gioKetThuc, CancellationToken cancellationToken);

    Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken);

    Task KhoaSanAsync(int maSan, CancellationToken cancellationToken);
}

public class DatSanService(ApplicationDbContext db, IDongHo dongHo, ILogger<DatSanService> logger) : IDatSanService
{
    public async Task<KetQuaTaoDon> TaoDonAsync(int maTaiKhoan, int maSan, DateOnly ngaySuDung, TimeOnly gioBatDau,
        TimeOnly gioKetThuc, CancellationToken cancellationToken)
    {
        var khachHang = await db.KhachHangs.Include(x => x.TaiKhoan)
            .SingleOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan, cancellationToken);
        if (khachHang is null || khachHang.TrangThai != TrangThaiKhachHang.HoatDong
            || khachHang.TaiKhoan.TrangThai != TrangThaiTaiKhoan.HoatDong)
        {
            return Loi("Hồ sơ hoặc tài khoản không đủ điều kiện để đặt sân.");
        }

        var gioBatDauChuan = new TimeOnly(gioBatDau.Hour, gioBatDau.Minute);
        var gioKetThucChuan = new TimeOnly(gioKetThuc.Hour, gioKetThuc.Minute);
        var batDau = ngaySuDung.ToDateTime(gioBatDauChuan);
        var ketThuc = ngaySuDung.ToDateTime(gioKetThucChuan);
        if (batDau <= dongHo.BayGio) return Loi("Thời gian bắt đầu phải ở tương lai.", "GioBatDau");
        if (ketThuc <= batDau) return Loi("Giờ kết thúc phải sau giờ bắt đầu.", "GioKetThuc");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await KhoaSanAsync(maSan, cancellationToken);

        var san = await db.SanTheThaos.Include(x => x.LoaiSan)
            .SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        if (san is null) return Loi("Sân không tồn tại.", "MaSan");
        if (san.TrangThai != TrangThaiSan.HoatDong || san.LoaiSan.TrangThai != TrangThaiLoaiSan.HoatDong)
        {
            return Loi("Sân hiện không nhận đặt.", "MaSan");
        }
        if (gioBatDauChuan < san.GioMoCua || gioKetThucChuan > san.GioDongCua)
        {
            return Loi($"Sân chỉ mở cửa từ {san.GioMoCua:HH:mm} đến {san.GioDongCua:HH:mm}.", "GioBatDau");
        }
        if (san.NgayBaoTri == ngaySuDung) return Loi("Sân bảo trì vào ngày này.", "NgaySuDung");
        if (await CoTrungLichAsync(maSan, batDau, ketThuc, null, cancellationToken))
        {
            return Loi("Khung giờ này đã có người đặt. Vui lòng chọn khung giờ hoặc sân khác.", "GioBatDau");
        }

        var don = new DatSan
        {
            MaKhachHang = khachHang.MaKhachHang,
            MaSan = san.MaSan,
            NgayDat = dongHo.BayGio,
            GioBatDau = batDau,
            GioKetThuc = ketThuc,
            DonGia = san.DonGia,
            TienCoc = 0m,
            TrangThai = TrangThaiDatSan.ChoXuLy
        };
        db.DatSans.Add(don);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không lưu được đơn đặt sân của tài khoản {MaTaiKhoan}.", maTaiKhoan);
            return Loi("Không lưu được đơn đặt sân. Vui lòng thử lại.");
        }
        return new KetQuaTaoDon(don.MaDatSan);
    }

    // Hai lịch giao nhau khi batDauMoi < ketThucCu và ketThucMoi > batDauCu; DaHuy và TuChoi không chiếm chỗ.
    public Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken) => db.DatSans.AnyAsync(x => x.MaSan == maSan
            && x.TrangThai != TrangThaiDatSan.DaHuy && x.TrangThai != TrangThaiDatSan.TuChoi
            && (boQuaMaDatSan == null || x.MaDatSan != boQuaMaDatSan)
            && batDau < x.GioKetThuc && ketThuc > x.GioBatDau, cancellationToken);

    // Khóa dòng của sân đến hết transaction hiện tại; mọi luồng tạo/xác nhận đơn cùng sân phải gọi hàm này.
    public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT 1 FROM SanTheThao WITH (UPDLOCK, ROWLOCK) WHERE MaSan = {maSan}", cancellationToken);

    // Công thức tiền sân theo mục 5 quy ước: số phút / 60 x đơn giá, làm tròn đến đồng.
    public static decimal TinhTienSanDuKien(decimal donGia, DateTime batDau, DateTime ketThuc)
    {
        decimal soPhut = (ketThuc - batDau).Ticks / TimeSpan.TicksPerMinute;
        return Math.Round(soPhut * donGia / 60m, 0, MidpointRounding.AwayFromZero);
    }

    private static KetQuaTaoDon Loi(string loi, string? truongLoi = null) => new(null, loi, truongLoi);
}

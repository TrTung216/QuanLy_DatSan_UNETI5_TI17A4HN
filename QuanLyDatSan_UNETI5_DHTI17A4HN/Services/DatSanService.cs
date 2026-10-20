// M3: Nguyễn Văn Quý; MSSV: 23103100181. kiểm tra nghiệp vụ, khóa theo sân và tạo đơn đặt sân; M4 dùng chung KhoaSanAsync/CoTrungLichAsync.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung M4: chuẩn hóa DTO và mã lỗi tạo đơn; dùng chung quy tắc khả dụng M2/M3/M4, kiểm tra lại sau khóa sân.
// File này còn cung cấp công thức tính tiền sân dự kiến theo thời lượng và đơn giá trên đơn.

using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService(ApplicationDbContext db, IDongHo dongHo) : IDatSanService
{
    private Task<bool> KhachDuDieuKienAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
        db.KhachHangs.AsNoTracking().AnyAsync(x => x.MaTaiKhoan == maTaiKhoan
            && x.TrangThai == TrangThaiKhachHang.HoatDong
            && x.TaiKhoan.TrangThai == TrangThaiTaiKhoan.HoatDong
            && x.TaiKhoan.VaiTro == VaiTro.KhachHang, cancellationToken);

    public async Task<KetQuaTaoDon> TaoDonAsync(TaoDonYeuCau yeuCau, CancellationToken cancellationToken)
    {
        var (maTaiKhoan, maSan, ngaySuDung, gioBatDau, gioKetThuc) = yeuCau;
        if (!await KhachDuDieuKienAsync(maTaiKhoan, cancellationToken))
            return Loi(MaLoiDatSan.HoSoKhongHoatDong);

        var (batDau, ketThuc) = ChuanHoaKhoangGio(new(ngaySuDung, gioBatDau, gioKetThuc));
        var loiGio = KiemTraKhoangGio(batDau, ketThuc, dongHo.BayGio);
        if (loiGio is not null) return Loi(AnhXaLoiKhaDung(loiGio.Value, khiTao: true));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await KhoaSanAsync(maSan, cancellationToken);

        // M3: đọc lại điều kiện khách và giờ hiện tại sau khi chờ khóa sân.
        var khachHang = await db.KhachHangs.AsNoTracking().Include(x => x.TaiKhoan)
            .SingleOrDefaultAsync(x => x.MaTaiKhoan == maTaiKhoan, cancellationToken);
        if (khachHang is null || khachHang.TrangThai != TrangThaiKhachHang.HoatDong
            || khachHang.TaiKhoan.TrangThai != TrangThaiTaiKhoan.HoatDong
            || khachHang.TaiKhoan.VaiTro != VaiTro.KhachHang)
            return Loi(MaLoiDatSan.HoSoKhongHoatDong);

        var bayGio = dongHo.BayGio;
        loiGio = KiemTraKhoangGio(batDau, ketThuc, bayGio);
        if (loiGio is not null) return Loi(AnhXaLoiKhaDung(loiGio.Value, khiTao: true));

        var san = await TruyVanKhaDungSan(batDau, ketThuc, null)
            .SingleOrDefaultAsync(x => x.MaSan == maSan, cancellationToken);
        var loiSan = DanhGiaKhaDungSan(san, batDau, ketThuc);
        if (loiSan is not null)
            return KetQuaTaoDon.ThatBai(new(AnhXaLoiKhaDung(loiSan.Value, khiTao: true),
                san?.GioMoCua, san?.GioDongCua));

        var don = new DatSan
        {
            MaKhachHang = khachHang.MaKhachHang,
            MaSan = san!.MaSan,
            NgayDat = bayGio,
            GioBatDau = batDau,
            GioKetThuc = ketThuc,
            DonGia = san.DonGia,
            TienCoc = 0m,
            TrangThai = TrangThaiDatSan.ChoXuLy
        };
        db.DatSans.Add(don);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return KetQuaTaoDon.DaTao(don.MaDatSan);
    }

    // Hai lịch giao nhau khi batDauMoi < ketThucCu và ketThucMoi > batDauCu; DaHuy và TuChoi không chiếm chỗ.
    public Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken) => DonTrungLich(batDau, ketThuc, boQuaMaDatSan)
            .AnyAsync(x => x.MaSan == maSan, cancellationToken);

    // Khóa dòng của sân đến hết transaction hiện tại; mọi luồng tạo/xác nhận đơn cùng sân phải gọi hàm này.
    public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT 1 FROM SanTheThao WITH (UPDLOCK, ROWLOCK) WHERE MaSan = {maSan}", cancellationToken);

    // Công thức tiền sân theo mục 5 quy ước: số phút / 60 x đơn giá, làm tròn đến đồng.
    public static decimal TinhTienSanDuKien(decimal donGia, DateTime batDau, DateTime ketThuc)
    {
        decimal soPhut = (ketThuc - batDau).Ticks / TimeSpan.TicksPerMinute;
        return Math.Round(soPhut * donGia / 60m, 0, MidpointRounding.AwayFromZero);
    }

    private static KetQuaTaoDon Loi(MaLoiDatSan ma) => KetQuaTaoDon.ThatBai(new(ma));
}

// M3: Nguyễn Văn Quý; MSSV: 23103100181. kiểm tra nghiệp vụ, khóa theo sân và tạo đơn đặt sân; M4 dùng chung KhoaSanAsync/CoTrungLichAsync.
//M4: Phạm Thành Nghĩa; MSSV: 23103100308, nghiệp vụ xác nhận, từ chối, khách hàng hủy, nhân viên/Admin hủy và hoàn thành đơn
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public record KetQuaTaoDon(int? MaDatSan, string? Loi = null, string? TruongLoi = null)
{
    public bool ThanhCong => MaDatSan.HasValue;
}

public sealed record KetQuaXuLyDon(bool ThanhCong, string? Loi = null)
{
    public static KetQuaXuLyDon ThanhCongResult() => new(true);
    public static KetQuaXuLyDon ThatBai(string loi) => new(false, loi);
}
public interface IDatSanService
{
    Task<KetQuaTaoDon> TaoDonAsync(int maTaiKhoan, int maSan, DateOnly ngaySuDung, TimeOnly gioBatDau,
        TimeOnly gioKetThuc, CancellationToken cancellationToken);

    Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken);

    Task KhoaSanAsync(int maSan, CancellationToken cancellationToken);

    Task<KetQuaXuLyDon> XacNhanDonAsync(int maDatSan, byte[] phienBan, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> TuChoiDonAsync(int maDatSan, string? lyDo, byte[] phienBan, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> KhachHangHuyDonAsync(int maDatSan, int maTaiKhoan, string? lyDo,
        byte[] phienBan, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> NhanVienHuyDonAsync(int maDatSan, string? lyDo, byte[] phienBan,
        CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> HoanThanhDonAsync(int maDatSan, decimal tienSan, decimal tienDichVu, bool daThuDu,
        byte[] phienBan, CancellationToken cancellationToken);
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

    // Chỉ chuyển đơn đang chờ; khóa sân và kiểm tra lại điều kiện đặt trước khi xác nhận.
    public Task<KetQuaXuLyDon> XacNhanDonAsync(int maDatSan, byte[] phienBan,
        CancellationToken cancellationToken) =>
        XuLyDonCoKhoaSanAsync(maDatSan, phienBan, async (don, token) =>
        {
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return "Chỉ có thể xác nhận đơn đang chờ xử lý.";

            var now = dongHo.BayGio;
            var san = don.SanTheThao;
            if (don.GioBatDau <= now)
                return "Không thể xác nhận vì giờ bắt đầu của đơn đã qua.";
            if (san.TrangThai != TrangThaiSan.HoatDong
                || san.LoaiSan.TrangThai != TrangThaiLoaiSan.HoatDong)
                return "Sân hoặc loại sân hiện không hoạt động.";
            if (TimeOnly.FromDateTime(don.GioBatDau) < san.GioMoCua
                || TimeOnly.FromDateTime(don.GioKetThuc) > san.GioDongCua)
                return "Thời gian đặt nằm ngoài giờ hoạt động của sân.";
            if (san.NgayBaoTri == DateOnly.FromDateTime(don.GioBatDau))
                return "Sân được lên lịch bảo trì vào ngày này.";
            if (await CoTrungLichAsync(don.MaSan, don.GioBatDau, don.GioKetThuc,
                    don.MaDatSan, token))
                return "Khung giờ này đã bị một đơn khác giữ. Không thể xác nhận.";

            don.TrangThai = TrangThaiDatSan.DangXuLy;
            don.NgayXacNhan = now;
            return null;
        }, cancellationToken);

    // Chỉ nhân viên/Admin được gọi action này ở controller; service bắt buộc lý do và chỉ từ chối đơn chờ.
    public Task<KetQuaXuLyDon> TuChoiDonAsync(int maDatSan, string? lyDo, byte[] phienBan,
        CancellationToken cancellationToken)
    {
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai("Lý do từ chối là bắt buộc và tối đa 1000 ký tự."));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return Task.FromResult<string?>("Chỉ có thể từ chối đơn đang chờ xử lý.");

            don.TrangThai = TrangThaiDatSan.TuChoi;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return Task.FromResult<string?>(null);
        }, cancellationToken);
    }

    // Khách chỉ được hủy đơn của mình khi đơn còn chờ và giờ bắt đầu chưa tới.
    public Task<KetQuaXuLyDon> KhachHangHuyDonAsync(int maDatSan, int maTaiKhoan, string? lyDo,
        byte[] phienBan, CancellationToken cancellationToken)
    {
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai("Lý do hủy là bắt buộc và tối đa 1000 ký tự."));
        if (maTaiKhoan <= 0)
            return Task.FromResult(KetQuaXuLyDon.ThatBai("Tài khoản không hợp lệ."));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.KhachHang.MaTaiKhoan != maTaiKhoan)
                return Task.FromResult<string?>("Không tìm thấy đơn đặt sân.");
            if (don.TrangThai != TrangThaiDatSan.ChoXuLy)
                return Task.FromResult<string?>("Bạn chỉ có thể hủy đơn đang chờ xử lý.");
            if (don.GioBatDau <= dongHo.BayGio)
                return Task.FromResult<string?>("Không thể hủy sau khi giờ bắt đầu đã tới.");

            don.TrangThai = TrangThaiDatSan.DaHuy;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return Task.FromResult<string?>(null);
        }, cancellationToken);
    }

    // Nhân viên/Admin có thể hủy đơn chờ; đơn đã xác nhận chỉ hủy trước giờ bắt đầu và khi chưa nhận cọc.
    public Task<KetQuaXuLyDon> NhanVienHuyDonAsync(int maDatSan, string? lyDo, byte[] phienBan,
        CancellationToken cancellationToken)
    {
        var lyDoChuan = lyDo?.Trim();
        if (string.IsNullOrWhiteSpace(lyDoChuan) || lyDoChuan.Length > 1000)
            return Task.FromResult(KetQuaXuLyDon.ThatBai("Lý do hủy là bắt buộc và tối đa 1000 ký tự."));

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.TrangThai is not (TrangThaiDatSan.ChoXuLy or TrangThaiDatSan.DangXuLy))
                return Task.FromResult<string?>("Trạng thái hiện tại không cho phép hủy đơn.");
            if (don.GioBatDau <= dongHo.BayGio)
                return Task.FromResult<string?>("Không thể hủy sau khi giờ bắt đầu đã tới.");
            if (don.TrangThai == TrangThaiDatSan.DangXuLy && don.TienCoc > 0m)
                return Task.FromResult<string?>("Đơn đã nhận cọc không thể hủy trong phiên bản hiện tại.");

            don.TrangThai = TrangThaiDatSan.DaHuy;
            don.NgayHuy = dongHo.BayGio;
            don.LyDoHuy = lyDoChuan;
            return Task.FromResult<string?>(null);
        }, cancellationToken);
    }

    // Giá tiền phải do tầng tính tiền (M5) cung cấp; service này chỉ chốt tiền và trạng thái trong cùng transaction.
    public Task<KetQuaXuLyDon> HoanThanhDonAsync(int maDatSan, decimal tienSan, decimal tienDichVu,
        bool daThuDu, byte[] phienBan, CancellationToken cancellationToken)
    {
        if (!daThuDu)
            return Task.FromResult(KetQuaXuLyDon.ThatBai("Cần xác nhận đã thu đủ tiền trước khi hoàn thành đơn."));
        if (tienSan < 0m || tienDichVu < 0m)
            return Task.FromResult(KetQuaXuLyDon.ThatBai("Các khoản tiền không được âm."));
        var tongTien = tienSan + tienDichVu;

        return XuLyDonCoKhoaSanAsync(maDatSan, phienBan, (don, _) =>
        {
            if (don.TrangThai != TrangThaiDatSan.DangXuLy)
                return Task.FromResult<string?>("Chỉ có thể hoàn thành đơn đã xác nhận.");
            if (don.GioKetThuc > dongHo.BayGio)
                return Task.FromResult<string?>("Chưa đến giờ kết thúc sử dụng sân.");
            if (don.TienCoc > tongTien)
                return Task.FromResult<string?>("Tổng tiền không được thấp hơn tiền cọc đã nhận.");

            don.TienSan = tienSan;
            don.TienDichVu = tienDichVu;
            don.TongTien = tongTien;
            don.NgayHoanThanh = dongHo.BayGio;
            don.TrangThai = TrangThaiDatSan.HoanThanh;
            return Task.FromResult<string?>(null);
        }, cancellationToken);
    }

    private async Task<KetQuaXuLyDon> XuLyDonCoKhoaSanAsync(int maDatSan, byte[] phienBan,
        Func<DatSan, CancellationToken, Task<string?>> apDung,
        CancellationToken cancellationToken)
    {
        if (maDatSan <= 0)
            return KetQuaXuLyDon.ThatBai("Mã đơn không hợp lệ.");
        if (phienBan is not { Length: 8 })
            return KetQuaXuLyDon.ThatBai("Thiếu phiên bản đơn hợp lệ. Hãy tải lại trang.");

        var maSan = await db.DatSans.AsNoTracking()
            .Where(x => x.MaDatSan == maDatSan)
            .Select(x => (int?)x.MaSan)
            .SingleOrDefaultAsync(cancellationToken);
        if (!maSan.HasValue)
            return KetQuaXuLyDon.ThatBai("Không tìm thấy đơn đặt sân.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await KhoaSanAsync(maSan.Value, cancellationToken);

        var don = await db.DatSans
            .Include(x => x.SanTheThao).ThenInclude(x => x.LoaiSan)
            .Include(x => x.KhachHang)
            .SingleOrDefaultAsync(x => x.MaDatSan == maDatSan, cancellationToken);
        if (don is null)
            return KetQuaXuLyDon.ThatBai("Không tìm thấy đơn đặt sân.");
        if (don.MaSan != maSan.Value)
            return KetQuaXuLyDon.ThatBai("Thông tin sân của đơn đã thay đổi. Hãy tải lại trang.");
        if (!don.RowVersion.AsSpan().SequenceEqual(phienBan))
            return KetQuaXuLyDon.ThatBai("Đơn đã được cập nhật ở nơi khác. Hãy tải lại trang rồi thử lại.");

        var loi = await apDung(don, cancellationToken);
        if (loi is not null)
            return KetQuaXuLyDon.ThatBai(loi);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return KetQuaXuLyDon.ThanhCongResult();
        }
        catch (DbUpdateConcurrencyException)
        {
            return KetQuaXuLyDon.ThatBai("Đơn vừa được người khác cập nhật. Hãy tải lại trang.");
        }
    }
    private static KetQuaTaoDon Loi(string loi, string? truongLoi = null) => new(null, loi, truongLoi);
}

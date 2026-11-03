// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Khai báo yêu cầu, DTO, trạng thái và mã lỗi đặt cọc.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public enum MaLoiCoc
{
    DuLieuKhongHopLe = 1, KhongDuQuyen = 2, KhongTonTai = 3,
    ChuyenKhoanChuaSanSang = 4, MoPhongKhongDuocPhep = 5, KhongApDungCoc = 6,
    YeuCauDoiSoatCoc = 7, GiaoDichKhongTonTai = 8, GiaoDichKhongKhop = 9,
    SoTienKhongKhop = 10, ThoiDiemNhanKhongHopLe = 11, GiaoDichDaSuDung = 12,
    DonDaNhanCoc = 13, ChuaKiemTraGiaoDich = 14, PhienBanDaCu = 15,
    DieuKienDonKhongHopLe = 16, DuLieuTienKhongHopLe = 17,
    GiaoDichChoDoiSoat = 18, TrungMaGiaoDich = 19
}
public enum TrangThaiCocHienThi { KhongApDung, ChuaNhan, ChoDoiSoat, DaNhan, CanXuLyNgoaiLe, KhongSanSang }
public enum TrangThaiGiaoDichCoc { ChuaDoiSoat, DaGanBienNhan, CanXuLyNgoaiLe }

public sealed record KetQuaCoc<T> where T : class
{
    private KetQuaCoc(T? duLieu, MaLoiCoc? loi, MaLoiDatSan? loiDatSan, string? truongLoi)
        => (DuLieu, Loi, LoiDatSan, TruongLoi) = (duLieu, loi, loiDatSan, truongLoi);
    public bool ThanhCong => Loi is null;
    public T? DuLieu { get; }
    public MaLoiCoc? Loi { get; }
    public MaLoiDatSan? LoiDatSan { get; }
    public string? TruongLoi { get; }
    public static KetQuaCoc<T> DaXong(T duLieu) => new(duLieu ?? throw new ArgumentNullException(nameof(duLieu)), null, null, null);
    public static KetQuaCoc<T> ThatBai(MaLoiCoc loi, MaLoiDatSan? loiDatSan = null, string? truongLoi = null)
    {
        if (!Enum.IsDefined(loi) || (loi == MaLoiCoc.DieuKienDonKhongHopLe) != loiDatSan.HasValue
            || (loiDatSan.HasValue && !Enum.IsDefined(loiDatSan.Value))) throw new ArgumentException("Mã lỗi không hợp lệ.");
        return new(null, loi, loiDatSan, truongLoi);
    }
}

public sealed record XacNhanCocYeuCau(int MaDatSan, int MaGiaoDich, byte[] PhienBan, bool DaKiemTraGiaoDich);
public sealed record TaoGiaoDichMoPhongYeuCau(string MaGiaoDichNgoai, string NoiDung, decimal SoTien, DateTime NgayNhan);
public sealed record ThongTinCocDto(int MaDatSan, TrangThaiDatSan TrangThai, int PhienBanChinhSachCoc,
    decimal TienSanDuKien, decimal TienCocYeuCau, decimal TienCocDaNhan, string NoiDungChuyenKhoan,
    TrangThaiCocHienThi TrangThaiCoc, bool MoPhong, bool CoTheChuyen, string? NoiDungQrMoPhong, BienNhanCocDto? BienNhan);
public sealed record DoiSoatCocDto(ThongTinCocDto Don, byte[] PhienBan, IReadOnlyList<GiaoDichCocDto> GiaoDich, bool CoTheXacNhan);
public sealed record GiaoDichCocDto(int MaGiaoDich, string MaGiaoDichNgoai, string NoiDung, string Nguon,
    string TaiKhoanNhan, decimal SoTien, DateTime NgayNhan, TrangThaiGiaoDichCoc TrangThai);
public sealed record BienNhanCocDto(int MaBienNhan, int MaDatSan, decimal SoTien, string MaGiaoDichNgoai,
    string Nguon, DateTime NgayNhan, DateTime NgayXacNhan);
public sealed record XacNhanCocDto(BienNhanCocDto BienNhan, TrangThaiDatSan TrangThai, byte[] PhienBanMoi, bool DaXuLyTruocDo);

public sealed class CocMoPhongOptions
{
    public bool Bat { get; set; }
    public string TaiKhoanNhan { get; set; } = "DEMO-UNETI";
}

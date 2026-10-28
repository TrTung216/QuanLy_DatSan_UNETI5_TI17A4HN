// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// DTO, yêu cầu xử lý và mã lỗi đặt sân; dùng RowVersion để phát hiện dữ liệu cũ.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public enum MaLoiDatSan
{
    HoSoKhongHoatDong = 1,
    BatDauKhongTuongLai = 2,
    KetThucKhongHopLe = 3,
    SanKhongTonTai = 4,
    SanKhongNhanDat = 5,
    SanBaoTriKhiTao = 6,
    TrungLichKhiTao = 7,
    ChiXacNhanDonCho = 8,
    QuaGioXacNhan = 9,
    SanKhongHoatDong = 10,
    NgoaiGioHoatDong = 11,
    SanBaoTri = 12,
    TrungLich = 13,
    LyDoTuChoiKhongHopLe = 14,
    ChiTuChoiDonCho = 15,
    LyDoHuyKhongHopLe = 16,
    TaiKhoanKhongHopLe = 17,
    KhongTonTai = 18,
    KhachChiHuyDonCho = 19,
    QuaGioHuy = 20,
    TrangThaiKhongChoHuy = 21,
    DaNhanCoc = 22,
    ChuaThuDu = 23,
    TienAm = 24,
    ChiHoanThanhDonXacNhan = 25,
    ChuaDenGioKetThuc = 26,
    TongTienThapHonCoc = 27,
    MaDonKhongHopLe = 28,
    PhienBanKhongHopLe = 29,
    SanDaThayDoi = 30,
    PhienBanDaCu = 31,
    XungDotDuLieu = 32,
    ChiGhiCocDonXacNhan = 33,
    TienCocKhongHopLe = 34,
    CocKhongTang = 35,
    CocVuotTongTien = 36,
    TienVuotGioiHan = 37,
    NgoaiGioKhiTao = 100
}

public sealed record LoiDatSan(MaLoiDatSan Ma, TimeOnly? GioMoCua = null, TimeOnly? GioDongCua = null);

public sealed class KetQuaTaoDon
{
    private KetQuaTaoDon(int? maDatSan, LoiDatSan? loi) { MaDatSan = maDatSan; Loi = loi; }
    public int? MaDatSan { get; }
    public LoiDatSan? Loi { get; }
    public bool ThanhCong => MaDatSan.HasValue;
    public static KetQuaTaoDon DaTao(int maDatSan) => maDatSan > 0
        ? new(maDatSan, null) : throw new ArgumentOutOfRangeException(nameof(maDatSan));
    public static KetQuaTaoDon ThatBai(LoiDatSan loi)
    {
        ArgumentNullException.ThrowIfNull(loi);
        if (!Enum.IsDefined(loi.Ma)) throw new ArgumentOutOfRangeException(nameof(loi));
        return new(null, loi);
    }
}

public sealed class KetQuaXuLyDon
{
    private KetQuaXuLyDon(MaLoiDatSan? loi) { Loi = loi; }
    public MaLoiDatSan? Loi { get; }
    public bool ThanhCong => Loi is null;
    public static KetQuaXuLyDon ThanhCongResult() => new(null);
    public static KetQuaXuLyDon ThatBai(MaLoiDatSan loi) => Enum.IsDefined(loi)
        ? new(loi) : throw new ArgumentOutOfRangeException(nameof(loi));
}

public sealed record TaoDonYeuCau(int MaTaiKhoan, int MaSan, DateOnly NgaySuDung,
    TimeOnly GioBatDau, TimeOnly GioKetThuc);
public sealed record XuLyDonYeuCau(int MaDatSan, byte[] PhienBan);
public sealed record HuyDonYeuCau(int MaDatSan, string? LyDo, byte[] PhienBan);
public sealed record GhiCocYeuCau(int MaDatSan, decimal TongCoc, byte[] PhienBan);
public sealed record HoanThanhDonYeuCau(int MaDatSan, bool DaThuDu, byte[] PhienBan);
public sealed record SanChonDto(int MaSan, string TenSan, string TenLoai, decimal DonGia,
    TimeOnly GioMoCua, TimeOnly GioDongCua);
public sealed class DanhSachDonDto
{
    public IReadOnlyList<DonDatDongDto> Dong { get; init; } = [];
    public TrangThaiDatSan? TrangThai { get; init; }
    public int Trang { get; init; }
    public int TongTrang { get; init; }
    public int TongSo { get; init; }
}
public class DonDatDongDto
{
    public int MaDatSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public TrangThaiDatSan TrangThai { get; set; }
}

public class ChiTietDonDto
{
    public byte[] PhienBan { get; set; } = [];
    public int MaDatSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public DateTime NgayDat { get; set; }
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public decimal TienCoc { get; set; }
    public TrangThaiDatSan TrangThai { get; set; }
    public DateTime? NgayXacNhan { get; set; }
    public DateTime? NgayHuy { get; set; }
    public DateTime? NgayHoanThanh { get; set; }
    public string? LyDoHuy { get; set; }
    public decimal? TienSan { get; set; }
    public decimal? TienDichVu { get; set; }
    public decimal? TongTien { get; set; }
    public decimal TienSanDuKien { get; set; }
    public decimal TienDichVuDuKien { get; set; }
    public decimal TongTienDuKien { get; set; }
}

// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Khung giờ và kết quả kiểm tra khả dụng theo lô cho M2; không xác nhận giữ chỗ.

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public sealed record KhoangGioTraCuuSan(DateOnly NgaySuDung, TimeOnly GioBatDau, TimeOnly GioKetThuc);

public enum MaLoiKhaDungSan
{
    BatDauKhongTuongLai = 1,
    KhoangGioKhongHopLe,
    SanKhongTonTai,
    SanKhongNhanDat,
    NgoaiGioHoatDong,
    SanBaoTri,
    TrungLich
}

public sealed record KhaDungSanDto(int MaSan, MaLoiKhaDungSan? Loi,
    TimeOnly? GioMoCua = null, TimeOnly? GioDongCua = null)
{
    public bool KhaDung => Loi is null;
}

public sealed record KetQuaKhaDungSan(DateTime BatDau, DateTime KetThuc,
    MaLoiKhaDungSan? Loi, IReadOnlyList<KhaDungSanDto> DanhSach);

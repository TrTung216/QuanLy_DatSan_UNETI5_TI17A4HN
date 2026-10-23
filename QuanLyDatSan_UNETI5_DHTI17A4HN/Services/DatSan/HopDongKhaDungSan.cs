// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: hợp đồng tra cứu khả dụng theo lô cho M2; khoảng giờ, mã lý do và kết quả từng sân.
// Kết quả chỉ phản ánh thời điểm đọc, không giữ chỗ; không chứa dữ liệu khách hàng hay đơn đặt sân.
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

// Loi ở cấp kết quả là lỗi khoảng giờ; khi đó DanhSach rỗng.
// Nếu khoảng giờ hợp lệ, từng sân có kết quả riêng, kể cả mã sân không tồn tại.
public sealed record KetQuaKhaDungSan(DateTime BatDau, DateTime KetThuc,
    MaLoiKhaDungSan? Loi, IReadOnlyList<KhaDungSanDto> DanhSach);

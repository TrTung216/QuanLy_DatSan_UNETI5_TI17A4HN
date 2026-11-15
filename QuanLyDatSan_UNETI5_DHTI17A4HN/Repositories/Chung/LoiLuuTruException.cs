// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Bổ sung mã lỗi trùng giao dịch và biên nhận cọc.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ giữ nguyên phân loại lỗi lưu M1.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

public enum MaLoiLuuTru
{
    XungDotPhienBan,
    TrungTenLoaiSan,
    TrungTenDangNhap,
    LoaiSanDangDuocThamChieu,
    Deadlock,
    ThamChieuSan,
    // Hợp đồng lỗi cho service cọc M4; hạ tầng lưu trữ tích hợp mapping riêng.
    TrungGiaoDichCoc,
    TrungBienNhanCoc,
    TrungTenDichVu
}

// Không đưa Message/InnerException ra HTTP. Service chỉ ánh xạ MaLoi sang hợp đồng nghiệp vụ.
public sealed class LoiLuuTruException(MaLoiLuuTru maLoi, Exception innerException)
    : Exception("Thao tác lưu dữ liệu gặp xung đột đã nhận diện.", innerException)
{
    public MaLoiLuuTru MaLoi { get; } = maLoi;
}

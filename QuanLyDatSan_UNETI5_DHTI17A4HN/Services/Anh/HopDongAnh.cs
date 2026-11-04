// M1: Trần Trọng Tùng - 23103100202. Hợp đồng ảnh dùng chung, không phụ thuộc MVC/EF.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;

public enum NhomAnh { LoaiSan, San }
public enum ThaoTacAnh { ThayAnh, GoAnh }
public enum MaLoiAnh
{
    ThanhCong, DuLieuAnhKhongHopLe, DinhDangAnhKhongHoTro, AnhQuaDungLuong,
    AnhQuaKichThuoc, AnhKhongDocDuoc, XungDotAnh, LuuTruAnhChuaSanSang,
    KhongTonTai, KhongDuQuyen
}
// Người gọi sở hữu và dispose stream sau await; không tin TenTep/DoDai để cấp đường dẫn/bộ nhớ.
public sealed record AnhTaiLen(Stream NoiDung, string TenTep, long DoDai);
public sealed record YeuCauAnh(ThaoTacAnh ThaoTac, string? AnhCu, AnhTaiLen? Tep);
public sealed record KetQuaAnh(MaLoiAnh MaLoi, bool CanDonTep = false);
public sealed record ThongTinAnh(string? Khoa, bool CongKhai);
public sealed record TepAnhCu(string Khoa, DateTime SuaLanCuoiUtc);
public sealed class LoiAnhException(MaLoiAnh maLoi) : Exception("Không xử lý được ảnh.")
{
    public MaLoiAnh MaLoi { get; } = maLoi;
}

public interface IKhoAnh
{
    // Giữ suốt thao tác ảnh (file + commit DB). Bảo trì lấy khóa độc quyền, request thường dùng khóa chia sẻ.
    Task<IDisposable> GiuKhoaAsync(bool baoTri, CancellationToken ct);
    Task<string> LuuAsync(NhomAnh nhom, AnhTaiLen tep, CancellationToken ct);
    Task<Stream?> MoDocAsync(string khoa, CancellationToken ct);
    Task XoaAsync(string khoa, CancellationToken ct);
    IEnumerable<TepAnhCu> LietKeTepCu(DateTime truocUtc);
}
public interface IAnhService
{
    Task<Stream?> DocAsync(NhomAnh nhom, int id, CancellationToken ct);
    Task<KetQuaAnh> CapNhatLoaiSanAsync(int id, YeuCauAnh yeuCau, CancellationToken ct);
}
public interface IDonDepAnh
{
    // Chỉ khóa đã đọc từ DB hoặc khóa mới do IKhoAnh cấp, không nhận đường dẫn client.
    Task<bool> XoaNeuKhongThamChieuAsync(string? khoa);
}

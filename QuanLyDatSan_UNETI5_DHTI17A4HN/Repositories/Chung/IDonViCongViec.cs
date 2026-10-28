// Hợp đồng lưu dữ liệu và thực thi transaction dùng chung cho các repository.

using System.Data;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

public interface IDonViCongViec
{
    Task LuuAsync(CancellationToken cancellationToken);
    Task<T> TrongGiaoDichAsync<T>(Func<CancellationToken, Task<T>> xuLy, Func<T, bool> duocLuu,
        IsolationLevel isolationLevel, CancellationToken cancellationToken);
}

public enum MaLoiLuuTru { XungDotPhienBan, TrungTenLoaiSan, TrungTenTaiKhoan, ThamChieuLoaiSan, ThamChieuSan, Deadlock }

public sealed class LoiLuuTruException(MaLoiLuuTru ma, Exception inner) : Exception("Thao tác lưu trữ thất bại.", inner)
{
    public MaLoiLuuTru Ma { get; } = ma;
}

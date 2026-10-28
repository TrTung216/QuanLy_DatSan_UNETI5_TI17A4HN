// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ hợp đồng lưu và transaction không phụ thuộc EF.
using System.Data;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

public sealed record QuyetDinhLuu<T>(bool DuocLuu, T KetQua);

public interface IDonViCongViec
{
    // Lưu các thay đổi đã chuẩn bị trong scope; không gọi bên trong transaction khác.
    Task LuuAsync(CancellationToken cancellationToken);

    // Callback bắt đầu với context không có thay đổi chờ lưu. Chỉ DuocLuu=true mới lưu/commit.
    // Service con chuẩn bị dữ liệu, không gọi lại Unit of Work. Tác vụ sau commit do caller thực hiện.
    Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> congViec,
        IsolationLevel mucCoLap, CancellationToken cancellationToken);
}

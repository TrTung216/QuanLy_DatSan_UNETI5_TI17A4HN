// Giữ cách gọi transaction của các module hiện có trên hợp đồng dùng chung từ M1.
using System.Data;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;

public static class DonViCongViecExtensions
{
    public static Task<T> TrongGiaoDichAsync<T>(this IDonViCongViec donViCongViec,
        Func<CancellationToken, Task<T>> xuLy, Func<T, bool> duocLuu,
        IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
        donViCongViec.ThucHienTrongGiaoDichAsync(async token =>
        {
            var ketQua = await xuLy(token);
            return new QuyetDinhLuu<T>(duocLuu(ketQua), ketQua);
        }, isolationLevel, cancellationToken);
}

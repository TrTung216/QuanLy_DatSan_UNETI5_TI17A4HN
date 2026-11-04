// M1: Trần Trọng Tùng - 23103100202. Test nghiệp vụ cũ không thao tác hệ thống file.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan.M103.Tests;
internal sealed class DonDepAnhGia : IDonDepAnh
{
    public Task<bool> XoaNeuKhongThamChieuAsync(string? khoa) => Task.FromResult(true);
}

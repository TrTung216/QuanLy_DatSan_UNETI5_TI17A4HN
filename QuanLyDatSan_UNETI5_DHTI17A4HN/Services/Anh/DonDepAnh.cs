// M1: Trần Trọng Tùng - 23103100202. Đọc DB ở scope mới, không dùng entity tracking sau lỗi commit.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;

public sealed class DonDepAnh(IServiceScopeFactory scopes, IKhoAnh kho, ILogger<DonDepAnh> logger) : IDonDepAnh
{
    public async Task<bool> XoaNeuKhongThamChieuAsync(string? khoa)
    {
        if (khoa is null) return true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var scope = scopes.CreateAsyncScope();
            if (await scope.ServiceProvider.GetRequiredService<IAnhRepository>().CoThamChieuAsync(khoa, timeout.Token))
                return true;
            await kho.XoaAsync(khoa, timeout.Token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Chưa dọn được ảnh sau lưu DB; giữ tệp để kiểm tra sau. Loại lỗi: {LoaiLoi}", ex.GetType().Name);
            return false;
        }
    }
}

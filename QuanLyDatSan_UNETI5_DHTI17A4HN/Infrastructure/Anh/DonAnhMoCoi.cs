// M1: Trần Trọng Tùng - 23103100202. Công cụ ngoại tuyến, mặc định chỉ báo cáo, không gọi từ HTTP.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;

public static class DonAnhMoCoi
{
    public static async Task<int> ChayAsync(IServiceProvider services, bool xoa)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var kho = scope.ServiceProvider.GetRequiredService<IKhoAnh>();
            var repository = scope.ServiceProvider.GetRequiredService<IAnhRepository>();
            using var khoa = await kho.GiuKhoaAsync(baoTri: true, CancellationToken.None);
            var count = 0;
            foreach (var tep in kho.LietKeTepCu(DateTime.UtcNow.AddHours(-24)))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                if (await repository.CoThamChieuAsync(tep.Khoa, timeout.Token)) continue;
                Console.WriteLine($"{(xoa ? "Xóa" : "Dự kiến xóa")}: {tep.Khoa}");
                if (xoa) await kho.XoaAsync(tep.Khoa, timeout.Token);
                count++;
            }
            Console.WriteLine($"Tổng: {count} tệp không tham chiếu, cũ hơn 24 giờ. Chế độ: {(xoa ? "xóa" : "chỉ kiểm tra")}.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Dừng dọn ảnh; kiểm tra cấu hình/kết nối/quyền hoặc thao tác ảnh đang chạy. Loại lỗi: {ex.GetType().Name}");
            return 1;
        }
    }
}

// M1: Trần Trọng Tùng - 23103100202. Trả 413 dễ đọc trước antiforgery/model binding, kể cả chunked body.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;

public sealed class GioiHanAnhMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var gioiHan = context.GetEndpoint()?.Metadata.GetMetadata<GioiHanTaiAnhAttribute>();
        if (HttpMethods.IsPost(context.Request.Method) && gioiHan is not null)
        {
            var quaLon = context.Request.ContentLength > ChuanHoaAnh.GioiHanRequest;
            if (!quaLon)
            {
                context.Request.EnableBuffering(bufferThreshold: 64 * 1024, bufferLimit: ChuanHoaAnh.GioiHanRequest + 81920);
                var buffer = new byte[81920];
                long tong = 0;
                int soByte;
                while ((soByte = await context.Request.Body.ReadAsync(buffer, context.RequestAborted)) > 0)
                {
                    tong += soByte;
                    if (tong > ChuanHoaAnh.GioiHanRequest) { quaLon = true; break; }
                }
                context.Request.Body.Position = 0;
            }
            if (quaLon)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync("""
                    <!doctype html><html lang="vi"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
                    <title>Ảnh quá lớn</title><h1>Yêu cầu tải ảnh quá lớn</h1>
                    <p>Chọn một ảnh tối đa 5 MiB; toàn bộ biểu mẫu tối đa 6 MiB. Ảnh hiện tại chưa bị thay đổi.</p>
                    <p>Quay lại trang trước và chọn ảnh nhỏ hơn.</p></html>
                    """, context.RequestAborted);
                return;
            }
        }
        await next(context);
    }
}

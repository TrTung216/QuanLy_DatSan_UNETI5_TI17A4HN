// M1: Trần Trọng Tùng - 23103100202. Mọi ảnh đi qua service kiểm tra quyền, không public thư mục kho.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[AllowAnonymous]
public sealed class AnhController(IAnhService service) : Controller
{
    [HttpGet]
    public Task<IActionResult> LoaiSan(int id, CancellationToken ct) => Doc(NhomAnh.LoaiSan, id, ct);
    [HttpGet]
    public Task<IActionResult> San(int id, CancellationToken ct) => Doc(NhomAnh.San, id, ct);

    private async Task<IActionResult> Doc(NhomAnh nhom, int id, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        var stream = await service.DocAsync(nhom, id, ct);
        // FileStreamResult sở hữu/dispose stream; không bật range hoặc public cache.
        return stream is null ? NotFound() : File(stream, "image/jpeg");
    }
}

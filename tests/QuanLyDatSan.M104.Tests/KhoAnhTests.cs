// M1: Trần Trọng Tùng - 23103100202. Ảnh tổng hợp chỉ trong test/temp, không phải dữ liệu demo người dùng.
using ImageMagick;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
using Xunit;
namespace QuanLyDatSan.M104.Tests;

public sealed class ThuMucAnhTest : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "qlds-anh-test-" + Guid.NewGuid().ToString("N"));
    public void Dispose()
    {
        // Đường dẫn bất biến tự sinh dưới temp, không nhận từ config hoặc người dùng.
        if (!Path.GetFullPath(Root).StartsWith(Path.Combine(Path.GetTempPath(), "qlds-anh-test-"), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Sai thư mục test.");
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
    }
}

public class KhoAnhTests
{
    internal static byte[] TaoAnh(MagickFormat format = MagickFormat.Png, uint width = 40, uint height = 20)
    {
        ChuanHoaAnh.KhoiTao();
        // PNG RGBA 1x1 trong suốt; không mở quyền coder XC chỉ để tạo fixture.
        using var anh = new MagickImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR4nGNgAAIAAAUAAXpeqz8AAAAASUVORK5CYII="));
        anh.Resize(new MagickGeometry(width, height) { IgnoreAspectRatio = true });
        anh.SetAttribute("comment", "Du lieu rieng khong duoc giu");
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)6);
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        anh.SetProfile(exif);
        anh.Orientation = Orientation.RightTop;
        return anh.ToByteArray(format);
    }

    [Theory]
    [InlineData(MagickFormat.Jpeg, "jpg")]
    [InlineData(MagickFormat.Png, "PNG")]
    [InlineData(MagickFormat.WebP, "webp")]
    public async Task ChuanHoaDinhDangXoaMetadataVaKhongDongStream(MagickFormat format, string extension)
    {
        var bytes = TaoAnh(format);
        using var stream = new MemoryStream(bytes);
        var result = await ChuanHoaAnh.TaoJpegAsync(new(stream, "anh." + extension, bytes.Length), default);
        Assert.True(stream.CanRead);
        using var doc = new MagickImage(result);
        Assert.Equal(MagickFormat.Jpeg, doc.Format);
        Assert.Null(doc.GetExifProfile());
        Assert.Null(doc.GetAttribute("comment"));
        Assert.Equal(20U, doc.Width); Assert.Equal(40U, doc.Height); // AutoOrient trước khi strip EXIF.
        Assert.False(doc.HasAlpha);
        if (format != MagickFormat.Jpeg)
        {
            using var pixels = doc.GetPixels();
            var pixel = pixels.GetPixel(0, 0).ToColor()!;
            Assert.True(pixel.R >= 250 && pixel.G >= 250 && pixel.B >= 250); // Alpha → trắng.
        }
    }

    [Fact]
    public async Task ResizeGiuTiLeVaKhongPhongAnhNho()
    {
        var b = TaoAnh(width: 2000, height: 1000);
        using var stream = new MemoryStream(b);
        using var anh = new MagickImage(await ChuanHoaAnh.TaoJpegAsync(new(stream, "a.png", b.Length), default));
        Assert.Equal(800U, anh.Width); Assert.Equal(1600U, anh.Height);
    }

    [Theory]
    [InlineData(4097, 1)]
    [InlineData(4096, 4096)]
    public async Task ChanKichThuocTruocGiaiMaPixel(uint width, uint height)
    {
        var b = TaoAnh(width: width, height: height);
        using var stream = new MemoryStream(b);
        var ex = await Assert.ThrowsAsync<LoiAnhException>(() => ChuanHoaAnh.TaoJpegAsync(new(stream, "a.png", b.Length), default));
        Assert.Equal(MaLoiAnh.AnhQuaKichThuoc, ex.MaLoi);
    }

    [Theory]
    [InlineData("svg")]
    [InlineData("gif")]
    [InlineData("exe")]
    [InlineData("jpg")]
    public async Task ChanDuoiKhongKhopNoiDung(string extension)
    {
        var b = TaoAnh(); using var stream = new MemoryStream(b);
        var ex = await Assert.ThrowsAsync<LoiAnhException>(() => ChuanHoaAnh.TaoJpegAsync(new(stream, "a." + extension, b.Length), default));
        Assert.Equal(MaLoiAnh.DinhDangAnhKhongHoTro, ex.MaLoi);
    }

    [Fact]
    public async Task ChanRongGiaDungLuongVaAnhHong()
    {
        using var rong = new MemoryStream();
        Assert.Equal(MaLoiAnh.DuLieuAnhKhongHopLe, (await Assert.ThrowsAsync<LoiAnhException>(
            () => ChuanHoaAnh.TaoJpegAsync(new(rong, "a.png", 10), default))).MaLoi);
        using var quaLon = new MemoryStream(new byte[ChuanHoaAnh.DungLuongToiDa + 1]);
        Assert.Equal(MaLoiAnh.AnhQuaDungLuong, (await Assert.ThrowsAsync<LoiAnhException>(
            () => ChuanHoaAnh.TaoJpegAsync(new(quaLon, "a.png", 10), default))).MaLoi);
        using var hong = new MemoryStream([255, 216, 255, 1, 2, 3]);
        Assert.Equal(MaLoiAnh.AnhKhongDocDuoc, (await Assert.ThrowsAsync<LoiAnhException>(
            () => ChuanHoaAnh.TaoJpegAsync(new(hong, "a.jpg", 6), default))).MaLoi);
    }

    [Fact]
    public async Task ChanApngVaWebpDong()
    {
        var png = TaoAnh();
        // Chèn acTL vào chuỗi chunk, phải từ chối trước khi decoder coi thành PNG một frame.
        byte[] apng = [.. png[..8], 0, 0, 0, 0, 97, 99, 84, 76, 0, 0, 0, 0, .. png[8..]];
        byte[] webp = [.. "RIFF"u8.ToArray(), 13, 0, 0, 0, .. "WEBPVP8X"u8.ToArray(), 1, 0, 0, 0, 2];
        foreach (var (bytes, name) in new[] { (apng, "a.png"), (webp, "a.webp") })
        {
            using var stream = new MemoryStream(bytes);
            Assert.Equal(MaLoiAnh.DinhDangAnhKhongHoTro, (await Assert.ThrowsAsync<LoiAnhException>(
                () => ChuanHoaAnh.TaoJpegAsync(new(stream, name, bytes.Length), default))).MaLoi);
        }
    }

    internal static KhoAnhDiaPhuong Kho(string? root) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["LuuTruAnh:ThuMucGoc"] = root }).Build(),
        new MoiTruongTest(), NullLogger<KhoAnhDiaPhuong>.Instance);

    [Fact]
    public async Task KhoLuuDocXoaVaKhongNhanKhoaClient()
    {
        using var dir = new ThuMucAnhTest(); var kho = Kho(dir.Root);
        using var lease = await kho.GiuKhoaAsync(false, default);
        var b = TaoAnh(); using var stream = new MemoryStream(b);
        var key = await kho.LuuAsync(NhomAnh.LoaiSan, new(stream, "../../a.png", b.Length), default);
        Assert.Matches(@"^loai-san/[a-f0-9]{32}\.jpg$", key);
        using (var doc = await kho.MoDocAsync(key, default)) Assert.True(doc!.Length > 0);
        foreach (var sai in new[] { "../a.jpg", "loai-san/../../a.jpg", "C:/a.jpg", "san/test.jpg", key + "/x" })
            await Assert.ThrowsAsync<LoiAnhException>(() => kho.MoDocAsync(sai, default));
        await kho.XoaAsync(key, default);
        Assert.Null(await kho.MoDocAsync(key, default));
    }

    [Fact]
    public async Task ThieuCauHinhVaRootTrongSourceBiChan()
    {
        foreach (var root in new string?[] { null, "relative", AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            Assert.Equal(MaLoiAnh.LuuTruAnhChuaSanSang, (await Assert.ThrowsAsync<LoiAnhException>(
                () => Kho(root).GiuKhoaAsync(false, default))).MaLoi);
    }

    [Fact]
    public async Task BaoTriKhongChayKhiDangCapNhatAnh()
    {
        using var dir = new ThuMucAnhTest(); var kho = Kho(dir.Root);
        using (await kho.GiuKhoaAsync(false, default))
        {
            using var ghiKhac = await kho.GiuKhoaAsync(false, default);
            await Assert.ThrowsAsync<LoiAnhException>(() => kho.GiuKhoaAsync(true, default));
        }
        using (await kho.GiuKhoaAsync(true, default))
            await Assert.ThrowsAsync<LoiAnhException>(() => kho.GiuKhoaAsync(false, default));
    }

    private sealed class MoiTruongTest : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Test";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

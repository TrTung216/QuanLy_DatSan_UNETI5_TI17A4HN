// M1: Trần Trọng Tùng - 23103100202. Chỉ giải mã JPEG/PNG/WebP đã xác minh, xuất JPEG sạch metadata.
using System.Buffers.Binary;
using ImageMagick;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;

public static class ChuanHoaAnh
{
    public const int DungLuongToiDa = 5 * 1024 * 1024;
    public const int GioiHanRequest = 6 * 1024 * 1024;
    // Chặn đồng thời quá nhiều bộ giải mã native trong một tiến trình.
    private static readonly SemaphoreSlim HangDoi = new(2);
    static ChuanHoaAnh()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Anh");
        if (!File.Exists(Path.Combine(folder, "policy.xml"))) throw new InvalidOperationException("Missing image policy.");
        MagickNET.SetEnvironmentVariable("MAGICK_CONFIGURE_PATH", folder);
    }
    public static void KhoiTao() { }

    public static async Task<byte[]> TaoJpegAsync(AnhTaiLen tep, CancellationToken ct)
    {
        if (tep.DoDai <= 0) throw new LoiAnhException(MaLoiAnh.DuLieuAnhKhongHopLe);
        if (tep.DoDai > DungLuongToiDa) throw new LoiAnhException(MaLoiAnh.AnhQuaDungLuong);
        await HangDoi.WaitAsync(ct);
        try
        {
            using var duLieu = new MemoryStream();
            var buffer = new byte[81920];
            int soByte;
            while ((soByte = await tep.NoiDung.ReadAsync(buffer, ct)) != 0)
            {
                if (duLieu.Length + soByte > DungLuongToiDa) throw new LoiAnhException(MaLoiAnh.AnhQuaDungLuong);
                duLieu.Write(buffer, 0, soByte);
            }
            if (duLieu.Length == 0) throw new LoiAnhException(MaLoiAnh.DuLieuAnhKhongHopLe);
            var bytes = duLieu.ToArray();
            var format = NhanDien(bytes, Path.GetExtension(tep.TenTep).ToLowerInvariant());
            var settings = new MagickReadSettings { Format = format, FrameCount = 2 };
            try
            {
                using var thongTin = new MagickImageCollection();
                thongTin.Ping(bytes, settings); // Chỉ header; chưa cấp pixel cache.
                if (thongTin.Count != 1) throw new LoiAnhException(MaLoiAnh.DinhDangAnhKhongHoTro);
                var info = thongTin[0];
                if (info.Width == 0 || info.Height == 0 || info.Width > 4096 || info.Height > 4096
                    || (long)info.Width * info.Height > 16_000_000)
                    throw new LoiAnhException(MaLoiAnh.AnhQuaKichThuoc);
                ct.ThrowIfCancellationRequested();
                using var anh = new MagickImage(bytes, new MagickReadSettings { Format = format, FrameCount = 1 });
                anh.AutoOrient();
                if (anh.Width > 1600 || anh.Height > 1600) anh.Resize(new MagickGeometry(1600, 1600));
                anh.BackgroundColor = MagickColors.White;
                anh.Alpha(AlphaAction.Remove);
                anh.Strip();
                anh.Quality = 85;
                var ketQua = anh.ToByteArray(MagickFormat.Jpeg);
                ct.ThrowIfCancellationRequested();
                return ketQua;
            }
            catch (MagickException) { throw new LoiAnhException(MaLoiAnh.AnhKhongDocDuoc); }
        }
        finally { HangDoi.Release(); }
    }

    private static MagickFormat NhanDien(byte[] b, string extension)
    {
        if (b.Length >= 3 && b[0] == 0xff && b[1] == 0xd8 && b[2] == 0xff && extension is ".jpg" or ".jpeg")
            return MagickFormat.Jpeg;
        if (b.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && extension == ".png")
        {
            // APNG không được âm thầm chuyển thành frame đầu, kể cả khi decoder chỉ đọc PNG tĩnh.
            for (long pos = 8; pos + 12 <= b.Length;)
            {
                var i = (int)pos;
                if (b.AsSpan(i + 4, 4).SequenceEqual("acTL"u8)) throw new LoiAnhException(MaLoiAnh.DinhDangAnhKhongHoTro);
                pos += 12L + BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(i, 4));
            }
            return MagickFormat.Png;
        }
        if (b.Length >= 21 && b.AsSpan(0, 4).SequenceEqual("RIFF"u8) && b.AsSpan(8, 4).SequenceEqual("WEBP"u8)
            && extension == ".webp")
        {
            if (b.AsSpan(12, 4).SequenceEqual("VP8X"u8) && (b[20] & 2) != 0)
                throw new LoiAnhException(MaLoiAnh.DinhDangAnhKhongHoTro);
            return MagickFormat.WebP;
        }
        throw new LoiAnhException(MaLoiAnh.DinhDangAnhKhongHoTro);
    }
}

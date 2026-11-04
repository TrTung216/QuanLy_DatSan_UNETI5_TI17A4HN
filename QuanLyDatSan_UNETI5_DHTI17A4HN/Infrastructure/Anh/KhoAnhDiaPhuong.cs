// M1: Trần Trọng Tùng - 23103100202. Kho ảnh ngoài source/publish, khóa do server sinh, không static public.
using System.Text.RegularExpressions;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Infrastructure.Anh;

public sealed partial class KhoAnhDiaPhuong(IConfiguration config, IWebHostEnvironment env,
    ILogger<KhoAnhDiaPhuong> logger) : IKhoAnh
{
    [GeneratedRegex(@"\A(?:loai-san|san)/[a-f0-9]{32}\.jpg\z", RegexOptions.CultureInvariant)]
    private static partial Regex MauKhoa();

    public Task<IDisposable> GiuKhoaAsync(bool baoTri, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var root = ThuMucGoc();
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, ".ql-anh.lock");
            KiemTraLienKet(path);
            IDisposable khoa = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                baoTri ? FileShare.None : FileShare.ReadWrite);
            return Task.FromResult(khoa);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { CanhBao(ex); throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang); }
    }

    public async Task<string> LuuAsync(NhomAnh nhom, AnhTaiLen tep, CancellationToken ct)
    {
        var thuMuc = nhom switch { NhomAnh.LoaiSan => "loai-san", NhomAnh.San => "san", _ => throw new ArgumentOutOfRangeException(nameof(nhom)) };
        var khoa = $"{thuMuc}/{Guid.NewGuid():N}.jpg";
        var path = DuongDan(khoa);
        var jpeg = await ChuanHoaAnh.TaoJpegAsync(tep, ct);
        var daTao = false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            KiemTraLienKet(path);
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous))
            {
                daTao = true;
                await stream.WriteAsync(jpeg, ct);
                await stream.FlushAsync(ct);
            }
            return khoa;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        {
            if (daTao) { try { File.Delete(path); } catch (Exception cleanup) when (cleanup is IOException or UnauthorizedAccessException) { CanhBao(cleanup); } }
            if (ex is OperationCanceledException) throw;
            CanhBao(ex);
            throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang);
        }
    }

    public Task<Stream?> MoDocAsync(string khoa, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            var path = DuongDan(khoa);
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return Task.FromResult<Stream?>(stream);
        }
        catch (FileNotFoundException) { return Task.FromResult<Stream?>(null); }
        catch (DirectoryNotFoundException) { return Task.FromResult<Stream?>(null); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { CanhBao(ex); throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang); }
    }

    public Task XoaAsync(string khoa, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try { File.Delete(DuongDan(khoa)); }
        catch (DirectoryNotFoundException) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { CanhBao(ex); throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang); }
        return Task.CompletedTask;
    }

    public IEnumerable<TepAnhCu> LietKeTepCu(DateTime truocUtc)
    {
        var root = ThuMucGoc();
        foreach (var nhom in new[] { "loai-san", "san" })
        {
            var folder = Path.Combine(root, nhom);
            KiemTraLienKet(folder);
            if (!Directory.Exists(folder)) continue;
            foreach (var path in Directory.EnumerateFiles(folder, "*.jpg", SearchOption.TopDirectoryOnly))
            {
                var khoa = nhom + "/" + Path.GetFileName(path);
                if (!MauKhoa().IsMatch(khoa)) continue;
                KiemTraLienKet(path);
                var lucSua = File.GetLastWriteTimeUtc(path);
                if (lucSua < truocUtc) yield return new(khoa, lucSua);
            }
        }
    }

    private string DuongDan(string khoa)
    {
        if (!MauKhoa().IsMatch(khoa)) throw new LoiAnhException(MaLoiAnh.DuLieuAnhKhongHopLe);
        var root = ThuMucGoc();
        var path = Path.GetFullPath(Path.Combine(root, khoa.Replace('/', Path.DirectorySeparatorChar)));
        if (!NamTrong(path, root)) throw new LoiAnhException(MaLoiAnh.DuLieuAnhKhongHopLe);
        KiemTraLienKet(path);
        return path;
    }

    private string ThuMucGoc()
    {
        try
        {
            var value = config["LuuTruAnh:ThuMucGoc"];
            if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value)) throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang);
            var root = Path.GetFullPath(value);
            var source = env.ContentRootPath;
            for (var folder = new DirectoryInfo(source); folder is not null; folder = folder.Parent)
                if (Directory.Exists(Path.Combine(folder.FullName, ".git")) || Directory.EnumerateFiles(folder.FullName, "*.slnx").Any())
                { source = folder.FullName; break; }
            foreach (var cam in new[] { source, env.ContentRootPath, AppContext.BaseDirectory })
                if (NamTrong(root, cam) || NamTrong(cam, root)) throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang);
            KiemTraLienKet(root);
            return root;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        { CanhBao(ex); throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang); }
    }

    private static bool NamTrong(string path, string root) =>
        (Path.TrimEndingDirectorySeparator(path) + Path.DirectorySeparatorChar).StartsWith(
            Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private static void KiemTraLienKet(string path)
    {
        // Không đi qua symlink/junction. Thư mục do người vận hành quản lý, không cấp quyền tạo link cho người upload.
        for (string? item = path; item is not null; item = Path.GetDirectoryName(item))
            if ((File.Exists(item) || Directory.Exists(item)) && (File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0)
                throw new LoiAnhException(MaLoiAnh.LuuTruAnhChuaSanSang);
    }
    private void CanhBao(Exception ex) => logger.LogWarning("Kho ảnh chưa sẵn sàng. Loại lỗi: {LoaiLoi}", ex.GetType().Name);
}

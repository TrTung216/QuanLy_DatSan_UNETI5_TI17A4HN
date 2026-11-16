// M1: Trần Trọng Tùng; MSSV 23103100202. Kiểm thử tích hợp hợp đồng M3/M5, không dùng database thật.
using System.Data;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVuTrongDon;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class KhachHangThemDichVuTests
{
    [Theory]
    [InlineData(3, VaiTro.KhachHang, TrangThaiTaiKhoan.HoatDong, TrangThaiKhachHang.HoatDong, true)]
    [InlineData(1, VaiTro.KhachHang, TrangThaiTaiKhoan.HoatDong, TrangThaiKhachHang.HoatDong, false)]
    [InlineData(3, VaiTro.Admin, TrangThaiTaiKhoan.HoatDong, TrangThaiKhachHang.HoatDong, false)]
    [InlineData(3, VaiTro.KhachHang, TrangThaiTaiKhoan.BiKhoa, TrangThaiKhachHang.HoatDong, false)]
    [InlineData(3, VaiTro.KhachHang, TrangThaiTaiKhoan.HoatDong, TrangThaiKhachHang.BiKhoa, false)]
    public async Task RepositoryThatLocChuDonTaiKhoanVaHoSo(int caller, VaiTro role, TrangThaiTaiKhoan accountState,
        TrangThaiKhachHang profileState, bool allowed)
    {
        using var app = new UngDungKiemThu(); await app.KhoiTaoAsync();
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Schema chỉ trong SQLite RAM của fixture, không tạo/cập nhật database ứng dụng.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE DatSan (MaDatSan INTEGER PRIMARY KEY, MaKhachHang INTEGER, MaSan INTEGER,
                NgayDat TEXT, GioBatDau TEXT, GioKetThuc TEXT, DonGia TEXT, TienCoc TEXT,
                PhienBanChinhSachCoc INTEGER, TienCocYeuCau TEXT, TrangThai INTEGER, NgayXacNhan TEXT,
                NgayHoanThanh TEXT, NgayHuy TEXT, LyDoHuy TEXT, TienSan TEXT, TienDichVu TEXT,
                TongTien TEXT, RowVersion BLOB);
            CREATE TABLE DichVu (MaDichVu INTEGER PRIMARY KEY, TenDichVu TEXT, DonViTinh TEXT,
                DonGia TEXT, MoTa TEXT, TrangThai INTEGER);
            CREATE TABLE ChiTietDatSan (MaChiTiet INTEGER PRIMARY KEY, MaDatSan INTEGER,
                MaDichVu INTEGER, SoLuong INTEGER, DonGia TEXT);
            """);
        var account = await db.TaiKhoans.SingleAsync(x => x.MaTaiKhoan == 3);
        account.VaiTro = role; account.TrangThai = accountState;
        var profile = new KhachHang { MaTaiKhoan = 3, HoTen = "Khách", Email = "khach@example.test",
            SoDienThoai = "0900000000", TrangThai = profileState };
        db.KhachHangs.Add(profile); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO DatSan (MaDatSan, MaKhachHang, MaSan, NgayDat, GioBatDau, GioKetThuc,
                DonGia, TienCoc, PhienBanChinhSachCoc, TienCocYeuCau, TrangThai, RowVersion)
            VALUES (10, {profile.MaKhachHang}, 20, '2030-01-01', '2030-01-01 10:00:00',
                '2030-01-01 11:30:00', '200000', '150000', 1, '150000', 1, {BitConverter.GetBytes(1L)});
            """);
        db.ChangeTracker.Clear();
        var r = new DichVuTrongDonRepository(db);
        Assert.Equal(allowed, await r.LaChuDonAsync(10, caller, default));
        var order = await r.LayDonKhachHangDeCapNhatAsync(10, caller, default);
        Assert.Equal(allowed, order is not null);
        if (allowed) Assert.Equal(EntityState.Unchanged, db.Entry(order!).State);
        Assert.False(await r.LaChuDonAsync(999, caller, default));
    }

    [Fact]
    public async Task ThemGiuGiaSnapshotCocVaChanGuiLap()
    {
        var r = new KhoGia(); var s = Service(r);
        var version = r.Version;
        Assert.True((await s.KhachHangThemAsync(10, 5, 2, version, default)).ThanhCong);
        Assert.Equal(60000m, r.Don.TienDichVu);
        Assert.Equal(150000m, r.Don.TienCoc);
        Assert.False((await s.KhachHangThemAsync(10, 5, 2, version, default)).ThanhCong);
        r.DichVu.DonGia = 90000m;
        Assert.True((await s.KhachHangThemAsync(10, 5, 1, r.Version, default)).ThanhCong);
        var line = Assert.Single(r.Don.ChiTietDatSans);
        Assert.Equal(3, line.SoLuong); Assert.Equal(30000m, line.DonGia);
        Assert.Equal(90000m, r.Don.TienDichVu); Assert.Null(r.Don.TongTien);
        Assert.Equal(2, r.SoLanLuu);
    }

    [Theory]
    [InlineData(TrangThaiDatSan.ChoXuLy)]
    [InlineData(TrangThaiDatSan.HoanThanh)]
    [InlineData(TrangThaiDatSan.DaHuy)]
    [InlineData(TrangThaiDatSan.TuChoi)]
    public async Task ChanTrangThaiKhongDuocThem(TrangThaiDatSan state)
    {
        var r = new KhoGia(); r.Don.TrangThai = state;
        Assert.False((await Service(r).KhachHangThemAsync(10, 5, 1, r.Version, default)).ThanhCong);
        Assert.Empty(r.Don.ChiTietDatSans); Assert.Equal(0, r.SoLanLuu);
        Assert.False((await Service(r).LayChoKhachHangAsync(10, default))!.ChoPhepSua);
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(101)]
    public async Task ChanSoLuongSai(int quantity)
    {
        var r = new KhoGia();
        Assert.False((await Service(r).KhachHangThemAsync(10, 5, quantity, r.Version, default)).ThanhCong);
        Assert.Empty(r.Don.ChiTietDatSans); Assert.Equal(0, r.SoLanLuu);
    }

    [Fact]
    public async Task ChanVuotTongSoLuongDichVuNgungVaPhienBanSai()
    {
        var r = new KhoGia(); var s = Service(r);
        Assert.True((await s.KhachHangThemAsync(10, 5, 100, r.Version, default)).ThanhCong);
        Assert.False((await s.KhachHangThemAsync(10, 5, 1, r.Version, default)).ThanhCong);
        r.DichVu.TrangThai = TrangThaiDichVu.NgungHoatDong;
        Assert.False((await s.KhachHangThemAsync(10, 5, 1, r.Version, default)).ThanhCong);
        Assert.False((await s.KhachHangThemAsync(10, 999, 1, r.Version, default)).ThanhCong);
        Assert.False((await s.KhachHangThemAsync(10, 5, 1, "sai", default)).ThanhCong);
        Assert.Equal(100, Assert.Single(r.Don.ChiTietDatSans).SoLuong);
        Assert.Equal(1, r.SoLanLuu);
    }

    [Fact]
    public async Task KhongDocGhiDonNguoiKhacVaKiemTraLaiQuyenSauKhoa()
    {
        var r = new KhoGia(); var s = Service(r);
        Assert.Null(await s.LayChoKhachHangAsync(11, default));
        Assert.True((await s.KhachHangThemAsync(11, 5, 1, r.Version, default)).KhongTimThay);
        r.SauKhoa = () => r.ChuDon = 8;
        Assert.True((await s.KhachHangThemAsync(10, 5, 1, r.Version, default)).KhongTimThay);
        Assert.Equal(0, r.SoLanLuu); Assert.Empty(r.Don.ChiTietDatSans);
    }

    [Fact]
    public async Task KhachKhongDuocDungHamQuanLyNhanVienVanThemDuoc()
    {
        var r = new KhoGia();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(r).ThemAsync(10, 5, 1, r.Version, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(r).SuaSoLuongAsync(10, 1, 1, r.Version, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(r).HuyAsync(10, 1, r.Version, default));
        Assert.True((await Service(r, 2).ThemAsync(10, 5, 1, r.Version, default)).ThanhCong);
    }

    [Theory]
    [InlineData("DichVu")] [InlineData("ThemDichVu")]
    public void MenuLichCaNhanVanActive(string action)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "KhachHang")], "test"));
        Assert.Equal("Index", Assert.Single(MenuKhachHang.Lay(user), x => x.KhopTrang("DonDat", action)).Action);
    }

    [Fact]
    public async Task HttpFormAntiforgeryQuyenGiaVaGuiLap()
    {
        using var root = new UngDungKiemThu();
        await root.KhoiTaoAsync();
        var r = new KhoGia();
        using var app = root.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IDichVuTrongDonRepository>(); s.AddSingleton<IDichVuTrongDonRepository>(r);
            s.RemoveAll<IDonViCongViec>(); s.AddSingleton<IDonViCongViec>(r);
        }));
        using var client = app.CreateClient(new() { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/DonDat/DichVu/10")).StatusCode);
        var html = await client.GetStringAsync("/TaiKhoan/DangNhap");
        var token = Token(html);
        var login = await client.PostAsync("/TaiKhoan/DangNhap", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["TenDangNhap"] = "khachhang", ["MatKhau"] = root.MatKhau
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        html = await client.GetStringAsync("/DonDat/DichVu/10");
        Assert.Contains("Form.MaDichVu", html);
        Assert.Contains("Thêm dịch vụ", WebUtility.HtmlDecode(html));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/DonDat/DichVu/11")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/DichVuTrongDon?maDatSan=10")).StatusCode);
        var form = new Dictionary<string, string>
        {
            ["Form.MaDatSan"] = "10", ["Form.MaDichVu"] = "5", ["Form.SoLuong"] = "2",
            ["Form.PhienBan"] = r.Version, ["DonGia"] = "1", ["Form.DonGia"] = "1", ["MaTaiKhoan"] = "8"
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/DonDat/ThemDichVu", new FormUrlEncodedContent(form))).StatusCode);
        form["__RequestVerificationToken"] = Token(html);
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/DonDat/ThemDichVu", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Equal(60000m, r.Don.TienDichVu);
        var repeat = await client.PostAsync("/DonDat/ThemDichVu", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Contains("vừa được cập nhật", WebUtility.HtmlDecode(await repeat.Content.ReadAsStringAsync()));
        Assert.Equal(2, Assert.Single(r.Don.ChiTietDatSans).SoLuong);
        form["Form.PhienBan"] = r.Version;
        form["Form.SoLuong"] = "0";
        var invalid = await client.PostAsync("/DonDat/ThemDichVu", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        Assert.Contains("Số lượng phải từ 1 đến 100", WebUtility.HtmlDecode(await invalid.Content.ReadAsStringAsync()));
        form["Form.MaDatSan"] = "11";
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/DonDat/ThemDichVu", new FormUrlEncodedContent(form))).StatusCode);
        Assert.Equal(1, r.SoLanLuu);
    }

    private static string Token(string html) => WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(html,
        "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    private static DichVuTrongDonService Service(KhoGia r, int user = 3) => new(r, r, new TinhTienService(), new TaiKhoanGia(user));
    private sealed record TaiKhoanGia(int Id) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => Id;
        public string? HoTen => "Khách kiểm thử";
        public VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.KhachHang;
    }

    // Chỉ thay hạ tầng: service/tính tiền/controller/Razor/auth/antiforgery thật.
    // Không dùng fake này để khẳng định khóa/RowVersion SQL Server đã được kiểm thử.
    private sealed class KhoGia : IDichVuTrongDonRepository, IDonViCongViec
    {
        public int ChuDon = 3;
        public int SoLanLuu;
        public Action? SauKhoa;
        public DatSan Don { get; } = new() { MaDatSan = 10, MaSan = 20, DonGia = 200000,
            GioBatDau = new(2030, 1, 1, 10, 0, 0), GioKetThuc = new(2030, 1, 1, 11, 30, 0),
            TienCoc = 150000, TrangThai = TrangThaiDatSan.DangXuLy, RowVersion = BitConverter.GetBytes(1L) };
        public DichVu DichVu { get; } = new() { MaDichVu = 5, TenDichVu = "Nước uống", DonViTinh = "Chai", DonGia = 30000 };
        public string Version => Convert.ToBase64String(Don.RowVersion);
        public Task<bool> LaChuDonAsync(int id, int user, CancellationToken ct) => Task.FromResult(id == 10 && user == ChuDon);
        public Task<bool> CoQuyenQuanLyAsync(int user, CancellationToken ct) => Task.FromResult(user == 2);
        public Task<TrangThaiDatSan?> LayTrangThaiAsync(int id, CancellationToken ct) => Task.FromResult<TrangThaiDatSan?>(Don.TrangThai);
        public Task<byte[]?> LayRowVersionAsync(int id, CancellationToken ct) => Task.FromResult<byte[]?>(Don.RowVersion);
        public Task<int?> LayMaSanAsync(int id, CancellationToken ct) => Task.FromResult<int?>(Don.MaSan);
        public Task KhoaSanAsync(int id, CancellationToken ct) { SauKhoa?.Invoke(); return Task.CompletedTask; }
        public Task<DatSan?> LayDonDeCapNhatAsync(int id, CancellationToken ct) => Task.FromResult<DatSan?>(Don);
        public Task<DatSan?> LayDonKhachHangDeCapNhatAsync(int id, int user, CancellationToken ct) => Task.FromResult<DatSan?>(id == 10 && user == ChuDon ? Don : null);
        public Task<DichVu?> LayDichVuAsync(int id, CancellationToken ct) => Task.FromResult<DichVu?>(id == 5 ? DichVu : null);
        public Task<List<DichVuDoc>> LayDichVuHoatDongAsync(CancellationToken ct) => Task.FromResult(new List<DichVuDoc>
            { new(5, DichVu.TenDichVu, DichVu.DonViTinh, DichVu.DonGia, null, DichVu.TrangThai) });
        public Task<List<DongDichVuTrongDon>> LayDongAsync(int id, CancellationToken ct) => Task.FromResult(Don.ChiTietDatSans.Select(x =>
            new DongDichVuTrongDon(x.MaChiTiet, x.MaDichVu, DichVu.TenDichVu, DichVu.DonViTinh, x.SoLuong, x.DonGia, false, null, null)).ToList());
        public Task<List<DongDichVuTrongDon>> LayLichSuHuyAsync(int id, CancellationToken ct) => Task.FromResult(new List<DongDichVuTrongDon>());
        public Task GhiLichSuHuyVaXoaDongAsync(ChiTietDatSan line, int user, CancellationToken ct) => throw new NotSupportedException();
        public IEnumerable<DongDichVu> LayDongTinhTien(DatSan don) => don.ChiTietDatSans.Select(x => new DongDichVu(x.SoLuong, x.DonGia));
        public void DanhDauCapNhatTienDichVu(DatSan don) { }
        public void DatRowVersionGoc(DatSan don, byte[] version) { }
        public Task LuuAsync(CancellationToken ct) => throw new NotSupportedException();
        public async Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> work, IsolationLevel level, CancellationToken ct)
        {
            var result = await work(ct);
            if (result.DuocLuu) { SoLanLuu++; Don.RowVersion = BitConverter.GetBytes(SoLanLuu + 1L); }
            return result.KetQua;
        }
    }
}

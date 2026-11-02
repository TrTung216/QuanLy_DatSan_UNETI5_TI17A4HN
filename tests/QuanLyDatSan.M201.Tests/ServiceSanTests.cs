// Phan Giang Tâm - 23103100196. M2: Kiểm thử service, dropdown và CRUD sau khi tách tầng.
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

public partial class TrangThaiSanTests
{
    private static GhiSan YeuCauHopLe() => new("Sân mới", 1, "Hà Nội", null,
        200000, 350000, 450000, TrangThaiSan.HoatDong, new(6, 0), new(22, 0), null, null);

    [Theory]
    [InlineData("ten")]
    [InlineData("ten_dai")]
    [InlineData("dia_chi")]
    [InlineData("tien_ich")]
    [InlineData("ghi_chu")]
    [InlineData("gia_am")]
    [InlineData("gia_le")]
    [InlineData("cao_diem")]
    [InlineData("cuoi_tuan")]
    [InlineData("gia_lon")]
    [InlineData("gio")]
    [InlineData("trang_thai")]
    public async Task GoiServiceTrucTiepVoiDuLieuSaiKhongGhiDatabase(string truong)
    {
        using var app = await TaoAppAsync();
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISanTheThaoService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var x = YeuCauHopLe();
        x = truong switch
        {
            "ten" => x with { TenSan = "  " },
            "ten_dai" => x with { TenSan = new string('a', 101) },
            "dia_chi" => x with { DiaChi = "" },
            "tien_ich" => x with { TienIch = new string('a', 1001) },
            "ghi_chu" => x with { GhiChu = new string('a', 1001) },
            "gia_am" => x with { DonGia = -1 },
            "gia_le" => x with { DonGia = 100.5m },
            "cao_diem" => x with { DonGiaCaoDiem = -1 },
            "cuoi_tuan" => x with { DonGiaCuoiTuan = 1.5m },
            "gia_lon" => x with { DonGia = 10000000000000000m },
            "gio" => x with { GioDongCua = new(5, 0) },
            "trang_thai" => x with { TrangThai = (TrangThaiSan)99 },
            _ => throw new InvalidOperationException()
        };
        Assert.Equal(MaLoiSan.DuLieuKhongHopLe, (await service.TaoAsync(x, default)).MaLoi);
        Assert.Equal(MaLoiSan.DuLieuKhongHopLe, (await service.SuaAsync(1, x, default)).MaLoi);
        await db.SaveChangesAsync(); // Không để lại thay đổi chờ lưu sau thất bại.
        Assert.Equal(2, await db.SanTheThaos.CountAsync());
        Assert.Equal("Sân bảo trì", (await db.SanTheThaos.AsNoTracking().SingleAsync(s => s.MaSan == 1)).TenSan);
    }

    [Fact]
    public async Task ServiceTuKiemTraLoaiSanVaTraMaSauKhiLuu()
    {
        using var app = await TaoAppAsync();
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISanTheThaoService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var x = YeuCauHopLe();
        Assert.Equal(MaLoiSan.LoaiSanKhongHopLe, (await service.TaoAsync(x with { MaLoaiSan = 999 }, default)).MaLoi);
        var loai = await db.LoaiSans.SingleAsync();
        loai.TrangThai = TrangThaiLoaiSan.NgungHoatDong;
        await db.SaveChangesAsync();
        Assert.Equal(MaLoiSan.LoaiSanKhongHopLe, (await service.TaoAsync(x, default)).MaLoi);
        Assert.True((await service.SuaAsync(1, x, default)).ThanhCong); // Được giữ loại hiện tại.
        Assert.Empty(await service.LayLuaChonAsync(null, default));
        Assert.False(Assert.Single(await service.LayLuaChonAsync(1, default)).DangHoatDong);
        loai.TrangThai = TrangThaiLoaiSan.HoatDong;
        await db.SaveChangesAsync();
        var tao = await service.TaoAsync(x with { TenSan = "  Sân đã lưu  " }, default);
        Assert.True(tao.ThanhCong);
        Assert.Equal("Sân đã lưu", (await db.SanTheThaos.AsNoTracking().SingleAsync(s => s.MaSan == tao.DuLieu)).TenSan);
        Assert.Equal(MaLoiSan.KhongTonTai, (await service.SuaAsync(999, x, default)).MaLoi);
        Assert.Equal(MaLoiSan.KhongTonTai, (await service.XoaAsync(999, default)).MaLoi);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("nhanvien")]
    public async Task QuanLyXoaSanQuaManHinhXacNhan(string ten)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client, ten);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/SanTheThao/Delete/1"));
        Assert.Contains("Sân bảo trì", html);
        Assert.Contains("Bóng đá 5 người", html);
        var response = await PostAsync(client, "/SanTheThao/Delete/1", new());
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/SanTheThao", response.Headers.Location!.OriginalString);
        using var scope = app.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SanTheThaos.AnyAsync(x => x.MaSan == 1));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/SanTheThao/Delete/1")).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("khachhang")]
    public async Task KhachKhongDuocMoFormHoacGhiSan(string? ten)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        if (ten is not null) await DangNhapAsync(app, client, ten);
        var token = Token(await client.GetStringAsync(ten is null ? "/TaiKhoan/DangNhap" : "/TaiKhoan/ThongTin"));
        foreach (var path in new[] { "/SanTheThao/Create", "/SanTheThao/Edit/1", "/SanTheThao/Delete/1" })
        {
            var get = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
            Assert.Contains(ten is null ? "DangNhap" : "TuChoiTruyCap", get.Headers.Location!.OriginalString);
            var form = Form("1"); form["MaSan"] = "1"; form["__RequestVerificationToken"] = token;
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(path, new FormUrlEncodedContent(form))).StatusCode);
        }
        Assert.Equal("Sân bảo trì", (await DocSanAsync(app, 1)).TenSan);
    }

    [Fact]
    public async Task PostThieuAntiforgeryBiChan()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        foreach (var path in new[] { "/SanTheThao/Create", "/SanTheThao/Edit/1", "/SanTheThao/Delete/1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(path, new FormUrlEncodedContent(Form("1")))).StatusCode);
        Assert.Equal("Sân bảo trì", (await DocSanAsync(app, 1)).TenSan);
    }

    [Fact]
    public async Task FormLoiGiuDuLieuVaNapLaiLoaiCuDaNgungHoatDong()
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.LoaiSans.SingleAsync()).TrangThai = TrangThaiLoaiSan.NgungHoatDong;
            await db.SaveChangesAsync();
        }
        await DangNhapAsync(app, client);
        var form = Form("2"); form["MaSan"] = "1"; form["DonGia"] = "-1";
        var response = await PostAsync(client, "/SanTheThao/Edit/1", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Bóng đá 5 người (ngừng hoạt động)", html);
        Assert.Contains("Sân kiểm thử", html);
        Assert.Contains("field-validation-error", html);
        Assert.Equal("Sân bảo trì", (await DocSanAsync(app, 1)).TenSan);
        var create = await PostAsync(client, "/SanTheThao/Create", Form("1"));
        Assert.Contains("Vui lòng chọn loại sân đang hoạt động.", WebUtility.HtmlDecode(await create.Content.ReadAsStringAsync()));
    }

    [Theory]
    [InlineData("2026-10-12", "17:00", "19:00", 550000)]
    [InlineData("2026-10-12", "18:00", "21:00", 1050000)]
    [InlineData("2026-10-12", "21:00", "22:00", 200000)]
    [InlineData("2026-10-17", "17:00", "19:00", 900000)]
    [InlineData("2026-10-18", "06:00", "07:00", 450000)]
    public async Task ServiceTinhGiaGiuDungBienKhungGio(string ngay, string dau, string cuoi, int tien)
    {
        using var app = await TaoAppAsync(); using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISanTheThaoService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        service = new SanTheThaoService(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan.SanTheThaoRepository(db), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(db), new TaiKhoanThu());
        var gia = await service.TinhGiaAsync(1, DateOnly.Parse(ngay), TimeOnly.Parse(dau), TimeOnly.Parse(cuoi), default);
        Assert.True(gia.ThanhCong);
        Assert.Equal((decimal)tien, gia.DuLieu);
        Assert.Equal(MaLoiSan.KhongTonTai, (await new SanTheThaoService(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan.SanTheThaoRepository(db), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(db), new TaiKhoanThu { VaiTro = null }).TinhGiaAsync(1, DateOnly.Parse(ngay), TimeOnly.Parse(dau), TimeOnly.Parse(cuoi), default)).MaLoi);
    }

    [Theory]
    [InlineData("05:00", "07:00")]
    [InlineData("21:00", "23:00")]
    [InlineData("18:00", "18:00")]
    public async Task GioTinhGiaSaiTraLoiTrenForm(string dau, string cuoi)
    {
        using var app = await TaoAppAsync(); using var client = app.TaoClient();
        await DangNhapAsync(app, client);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync($"/SanTheThao/Details/1?ngay=2026-10-12&batDau={dau}&ketThuc={cuoi}"));
        Assert.Contains("Giờ thuê phải trong giờ phục vụ và kết thúc sau giờ bắt đầu trong cùng ngày.", html);
        Assert.DoesNotContain("Tiền thuê dự kiến:", html);
    }

    [Fact]
    public void KetQuaKhongChoThatBaiRongHoacDocDuLieuChuaLuu()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => KetQuaSan<int>.Loi(MaLoiSan.ThanhCong));
        Assert.Throws<ArgumentOutOfRangeException>(() => KetQuaSan<int>.Loi((MaLoiSan)999));
        Assert.Throws<ArgumentNullException>(() => KetQuaSan<string>.Dat(null!));
        foreach (var ma in Enum.GetValues<MaLoiSan>().Where(x => x != MaLoiSan.ThanhCong))
        {
            var ketQua = KetQuaSan<int>.Loi(ma);
            Assert.False(ketQua.ThanhCong);
            Assert.Throws<InvalidOperationException>(() => ketQua.DuLieu);
        }
        Assert.Equal(0m, KetQuaSan<decimal>.Dat(0m).DuLieu); // Giá bằng 0 vẫn là kết quả thành công hợp lệ.
    }

    private sealed class TaiKhoanThu : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => VaiTro.HasValue ? 1 : null;
        public string? HoTen => "Tài khoản kiểm thử";
        public VaiTro? VaiTro { get; init; } = QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.Admin;
    }

    [Fact]
    public async Task LoiHeThongKhiLuuTruyenLenVaKhongBaoThanhCong()
    {
        using var app = await TaoAppAsync(); using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(db.Database.GetDbConnection()).AddInterceptors(new LoiLuuThu()).Options;
        await using var dbLoi = new ApplicationDbContext(options);
        var service = new SanTheThaoService(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLySan.SanTheThaoRepository(dbLoi), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(dbLoi), new TaiKhoanThu());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TaoAsync(YeuCauHopLe(), default));
        Assert.Equal(2, await db.SanTheThaos.CountAsync());
    }

    private sealed class LoiLuuThu : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Lỗi giả lập trước khi lưu.");
    }
}

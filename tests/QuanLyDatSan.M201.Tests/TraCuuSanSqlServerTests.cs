// Phan Giang Tâm - 23103100196. M2: Kiểm tra lọc/sắp xếp tiền decimal trên SQL Server.
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;
using Xunit;

namespace QuanLyDatSan.M201.Tests;

// Bật riêng bằng QLDS_M2_TEST_SQLSERVER=1; cần SQL Server LocalDB trên Windows.
// Database có tên ngẫu nhiên của test, không đọc User Secrets/database cá nhân.
[Trait("Category", "SqlServer")]
public class TraCuuSanSqlServerTests(TraCuuSanSqlServerFixture fixture) : IClassFixture<TraCuuSanSqlServerFixture>
{
    private sealed class TaiKhoanQuanLy : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => 1;
        public string? HoTen => "Kiểm thử";
        public VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.Admin;
    }

    private SanTheThaoService TaoService(ApplicationDbContext db) => new(db, new TaiKhoanQuanLy());

    [M2SqlServerTheory]
    [InlineData("gia_tang", "80000,100000,150000,200000,250000,300000,350000,400000,450000,500000")]
    [InlineData("gia_giam", "650000,600000,550000,500000,450000,400000,350000,300000,250000,200000")]
    public async Task SapXepGiaVaPhanTrangChayTaiSqlServer(string sapXep, string giaDung)
    {
        await using var db = fixture.TaoDb();
        var result = await TaoService(db).LayDanhSachAsync(new(GiaDen: 700000, SapXep: sapXep), default);
        Assert.True(result.ThanhCong);
        Assert.Equal(13, result.DuLieu.TongSoSan);
        Assert.Equal(2, result.DuLieu.TongTrang);
        Assert.Equal(giaDung.Split(',').Select(decimal.Parse), result.DuLieu.DanhSach.Select(x => x.DonGia));
    }

    [M2SqlServerTheory]
    [InlineData("gia_tang", "9999999999999998,9999999999999999")]
    [InlineData("gia_giam", "9999999999999999,9999999999999998")]
    public async Task GiaLonKhongBiMatDoChinhXac(string sapXep, string giaDung)
    {
        await using var db = fixture.TaoDb();
        var result = await TaoService(db).LayDanhSachAsync(new(GiaTu: 9999999999999998m, SapXep: sapXep), default);
        Assert.Equal(giaDung.Split(',').Select(decimal.Parse), result.DuLieu.DanhSach.Select(x => x.DonGia));
    }

    [M2SqlServerFact]
    public async Task LocKhoangGiaKetHopTenLoaiVaTrangThai()
    {
        await using var db = fixture.TaoDb();
        var result = await TaoService(db).LayDanhSachAsync(new(TuKhoa: "BÓNG ĐÁ", MaLoaiSan: 1,
            TrangThai: TrangThaiSan.HoatDong, GiaTu: 100000, GiaDen: 200000, SapXep: "gia_tang"), default);
        Assert.Equal(new decimal[] { 100000, 150000, 200000 }, result.DuLieu.DanhSach.Select(x => x.DonGia));
        Assert.Equal(3, result.DuLieu.TongSoSan);
    }

    [M2SqlServerFact]
    public async Task TimDiaChiTiengVietKhongPhanBietChuHoaThuong()
    {
        await using var db = fixture.TaoDb();
        var result = await TaoService(db).LayDanhSachAsync(new(TuKhoa: "  ĐÀ NẴNG  "), default);
        Assert.True(result.ThanhCong);
        Assert.Equal(["Sân kiểm thử 04"], result.DuLieu.DanhSach.Select(x => x.TenSan));
        Assert.Equal(1, result.DuLieu.TongSoSan);
    }

    [M2SqlServerFact]
    public async Task HaiTrangKhongLapHoacBoSotSan()
    {
        await using var db = fixture.TaoDb(); var service = TaoService(db);
        var dieuKien = new TraCuuSan(GiaDen: 700000, SapXep: "gio_tang");
        var mot = (await service.LayDanhSachAsync(dieuKien, default)).DuLieu;
        var hai = (await service.LayDanhSachAsync(dieuKien with { Trang = 2 }, default)).DuLieu;
        Assert.Equal(10, mot.DanhSach.Count); Assert.Equal(3, hai.DanhSach.Count);
        Assert.Equal(13, mot.DanhSach.Concat(hai.DanhSach).Select(x => x.MaSan).Distinct().Count());
        var lapLai = (await service.LayDanhSachAsync(dieuKien, default)).DuLieu;
        Assert.Equal(mot.DanhSach.Select(x => x.MaSan), lapLai.DanhSach.Select(x => x.MaSan));
    }
}

public sealed class TraCuuSanSqlServerFixture : IAsyncLifetime
{
    private readonly string tenDatabase = "QLDS_M2_TraCuu_Test_" + Guid.NewGuid().ToString("N");
    private bool daTao;
    public ApplicationDbContext TaoDb()
    {
        // Chỉ dùng LocalDB và database do fixture này sở hữu.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={tenDatabase};Integrated Security=True;Encrypt=False;Connect Timeout=15;")
            .Options;
        return new(options);
    }

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M2_TEST_SQLSERVER") != "1") return;
        await using var db = TaoDb();
        daTao = await db.Database.EnsureCreatedAsync();
        if (!daTao) throw new InvalidOperationException("Database thử đã tồn tại; không dùng lại dữ liệu ngoài fixture.");
        for (var i = 1; i <= 15; i++)
        {
            var gia = i == 1 ? 9999999999999998m : i == 2 ? 9999999999999999m : i == 3 ? 80000m : (i - 2) * 50000m;
            db.SanTheThaos.Add(new SanTheThao
            {
                TenSan = $"Sân kiểm thử {i:00}", MaLoaiSan = 1, DiaChi = i == 4 ? "Đà Nẵng" : "Hà Nội",
                DonGia = gia, DonGiaCaoDiem = gia, DonGiaCuoiTuan = gia,
                TrangThai = TrangThaiSan.HoatDong, GioMoCua = new(6 + i % 3, 0), GioDongCua = new(22, 0)
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (!daTao) return;
        if (!Regex.IsMatch(tenDatabase, "^QLDS_M2_TraCuu_Test_[0-9a-f]{32}$"))
            throw new InvalidOperationException("Tên database không thuộc phạm vi kiểm thử M2.");
        await using var db = TaoDb();
        await db.Database.EnsureDeletedAsync();
    }
}

public sealed class M2SqlServerFactAttribute : FactAttribute
{
    public M2SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M2_TEST_SQLSERVER") != "1")
            Skip = "Bật QLDS_M2_TEST_SQLSERVER=1 để kiểm thử trên LocalDB riêng.";
    }
}

public sealed class M2SqlServerTheoryAttribute : TheoryAttribute
{
    public M2SqlServerTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("QLDS_M2_TEST_SQLSERVER") != "1")
            Skip = "Bật QLDS_M2_TEST_SQLSERVER=1 để kiểm thử trên LocalDB riêng.";
    }
}

// M1: Trần Trọng Tùng; MSSV: 23103100202. Nâng cấp schema cọc trên database test riêng, giữ nguyên lịch sử đơn cũ.
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class CocMigrationTests
{
    [Fact]
    public async Task NangCapGiuTienTrangThaiDichVuCuVaKhongTaoBienNhanGia()
    {
        var database = "QuanLyDatSan_M1CocMigrationTest_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder
        {
            DataSource = Environment.GetEnvironmentVariable("M4_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB",
            InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = true, ConnectTimeout = 15
        };
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("20261014060258_ThemChiTietDatSan");
            var account = new TaiKhoan { TenDangNhap = "migration-coc", MatKhau = "hash-test", HoTen = "Test", Email = "test@example.test" };
            var khach = new KhachHang { TaiKhoan = account, HoTen = "Test", Email = account.Email, SoDienThoai = "0900000000" };
            var san = new SanTheThao { TenSan = "Test", MaLoaiSan = 1, DiaChi = "Test", DonGia = 200000m, GioMoCua = new(6, 0), GioDongCua = new(22, 0) };
            var dv = new DichVu { TenDichVu = "Test", DonViTinh = "Chai", DonGia = 10000m };
            db.AddRange(khach, san, dv); await db.SaveChangesAsync();
            // Chỉ INSERT các cột tồn tại ở schema cũ, không dùng entity DatSan với hai cột mới.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO DatSan (MaKhachHang, MaSan, NgayDat, GioBatDau, GioKetThuc, DonGia, TienCoc,
                    TrangThai, NgayXacNhan, NgayHoanThanh, TienSan, TienDichVu, TongTien)
                VALUES ({khach.MaKhachHang}, {san.MaSan}, '20300101', '20300102 10:00', '20300102 11:00',
                    200000, 50000, {(int)TrangThaiDatSan.HoanThanh}, '20300101', '20300102 11:00', 200000, 10000, 210000)
                """);
            var maDon = await db.Database.SqlQuery<int>($"SELECT MaDatSan AS Value FROM DatSan").SingleAsync();
            db.ChiTietDatSans.Add(new ChiTietDatSan { MaDatSan = maDon, MaDichVu = dv.MaDichVu, SoLuong = 1, DonGia = 10000m });
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            var don = await db.DatSans.AsNoTracking().SingleAsync(x => x.MaDatSan == maDon);
            Assert.Equal(0, don.PhienBanChinhSachCoc);
            Assert.Equal(0m, don.TienCocYeuCau);
            Assert.Equal(50000m, don.TienCoc);
            Assert.Equal(TrangThaiDatSan.HoanThanh, don.TrangThai);
            Assert.Equal(200000m, don.DonGia);
            Assert.Equal(200000m, don.TienSan);
            Assert.Equal(10000m, don.TienDichVu);
            Assert.Equal(210000m, don.TongTien);
            Assert.Equal(new DateTime(2030, 1, 2, 11, 0, 0), don.NgayHoanThanh);
            Assert.Single(await db.ChiTietDatSans.Where(x => x.MaDatSan == maDon).ToListAsync());
            Assert.Empty(await db.GiaoDichCocs.ToListAsync());
            Assert.Empty(await db.BienNhanCocs.ToListAsync());

            // Chứng minh bảng mới trên schema migrate thật có RowVersion, không chỉ EnsureCreated.
            var gd = new GiaoDichCoc { MaGiaoDichNgoai = "MIGRATION-TEST", NoiDung = "COC 999", Nguon = "MoPhong",
                TaiKhoanNhan = "DEMO-UNETI", SoTien = 1m, NgayNhan = new(2030, 1, 1), NgayGhi = new(2030, 1, 1), MaNguoiGhi = account.MaTaiKhoan };
            db.GiaoDichCocs.Add(gd); await db.SaveChangesAsync();
            Assert.Equal(8, gd.RowVersion.Length);
        }
        finally
        {
            var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString()!);
            if (actual.InitialCatalog != database || !database.StartsWith("QuanLyDatSan_M1CocMigrationTest_", StringComparison.Ordinal))
                throw new InvalidOperationException("Database cleanup guard failed.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}

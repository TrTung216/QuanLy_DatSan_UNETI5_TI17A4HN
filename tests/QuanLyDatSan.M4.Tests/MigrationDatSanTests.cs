// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: áp dụng chuỗi migration thực tế trên database ngẫu nhiên riêng cho M4-01.
// Kiểm tra đọc model sân/đơn và hợp đồng khả dụng sau migrate; chỉ dọn đúng database do bài test tạo.
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using Xunit;

namespace QuanLyDatSan.M4.Tests;

public sealed class MigrationDatSanTests
{
    [Fact]
    public async Task MigrationTuDatabaseMoiPhucVuDuocHopDongKhaDung()
    {
        var database = "QuanLyDatSan_M4MigrationTest_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder
        {
            DataSource = Environment.GetEnvironmentVariable("M4_TEST_SQL_SERVER") ?? @"(localdb)\MSSQLLocalDB",
            InitialCatalog = database,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            ConnectTimeout = 15
        };
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
        try
        {
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
            await db.DatSans.AsNoTracking().Take(1).ToListAsync();
            await db.SanTheThaos.AsNoTracking().Include(x => x.LoaiSan).Take(1).ToListAsync();
            var clock = new DongHoTest();
            var result = await new DatSanService(new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan.DatSanRepository(db), new QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung.DonViCongViec(db), clock, new QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien.TinhTienService()).KiemTraKhaDungAsync(
                [int.MaxValue], new(clock.HomNay, new(10, 0), new(11, 0)), default);
            Assert.Equal(MaLoiKhaDungSan.SanKhongTonTai, Assert.Single(result.DanhSach).Loi);
        }
        finally
        {
            var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString()!);
            if (actual.InitialCatalog != database
                || !database.StartsWith("QuanLyDatSan_M4MigrationTest_", StringComparison.Ordinal))
                throw new InvalidOperationException("Database cleanup guard failed.");
            await db.Database.EnsureDeletedAsync();
        }
    }
}

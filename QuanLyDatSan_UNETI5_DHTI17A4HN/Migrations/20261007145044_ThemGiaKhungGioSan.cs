using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemGiaKhungGioSan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DonGiaCaoDiem",
                table: "SanTheThao",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DonGiaCuoiTuan",
                table: "SanTheThao",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // Sân đã có giữ nguyên giá thường; bổ sung mức cao điểm và cuối tuần.
            migrationBuilder.Sql("""
                EXEC(N'UPDATE s SET
                    DonGiaCaoDiem = CASE
                        WHEN l.TenLoai LIKE N''Bóng đá%'' THEN 350000
                        WHEN l.TenLoai = N''Cầu lông'' THEN 150000
                        WHEN l.TenLoai = N''Bóng rổ'' THEN 300000
                        WHEN l.TenLoai LIKE N''Quần vợt%'' AND s.DonGia = 120000 THEN 180000
                        ELSE s.DonGia END,
                    DonGiaCuoiTuan = CASE
                        WHEN l.TenLoai LIKE N''Bóng đá%'' THEN 450000
                        WHEN l.TenLoai = N''Cầu lông'' THEN 200000
                        WHEN l.TenLoai = N''Bóng rổ'' THEN 350000
                        WHEN l.TenLoai LIKE N''Quần vợt%'' AND s.DonGia = 120000 THEN 200000
                        ELSE s.DonGia END
                FROM SanTheThao s INNER JOIN LoaiSan l ON s.MaLoaiSan = l.MaLoaiSan;');
                """);

            // Thực thi sau khi hai cột đã được tạo, kể cả khi chạy bằng SQL script một batch.
            migrationBuilder.Sql("""
                EXEC(N'ALTER TABLE [SanTheThao] ADD CONSTRAINT [CK_SanTheThao_GiaKhungGio]
                CHECK (([DonGiaCaoDiem] IS NULL OR [DonGiaCaoDiem] >= 0)
                    AND ([DonGiaCuoiTuan] IS NULL OR [DonGiaCuoiTuan] >= 0));');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SanTheThao_GiaKhungGio",
                table: "SanTheThao");

            migrationBuilder.DropColumn(
                name: "DonGiaCaoDiem",
                table: "SanTheThao");

            migrationBuilder.DropColumn(
                name: "DonGiaCuoiTuan",
                table: "SanTheThao");
        }
    }
}

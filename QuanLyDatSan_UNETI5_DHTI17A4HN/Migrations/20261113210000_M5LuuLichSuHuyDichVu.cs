using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261113210000_M5LuuLichSuHuyDichVu")]
public partial class M5LuuLichSuHuyDichVu : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LichSuHuyDichVu",
            columns: table => new
            {
                MaLichSuHuy = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                MaDatSan = table.Column<int>(type: "int", nullable: false),
                MaChiTietNguon = table.Column<int>(type: "int", nullable: false),
                MaDichVu = table.Column<int>(type: "int", nullable: false),
                TenDichVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                DonViTinh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                SoLuong = table.Column<int>(type: "int", nullable: false),
                DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                NgayHuy = table.Column<DateTime>(type: "datetime2", nullable: false),
                MaTaiKhoanHuy = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LichSuHuyDichVu", x => x.MaLichSuHuy);
                table.CheckConstraint("CK_LichSuHuyDichVu_DonGia", "[DonGia] >= 0");
                table.CheckConstraint("CK_LichSuHuyDichVu_SoLuong", "[SoLuong] > 0");
            });

        migrationBuilder.CreateIndex(
            name: "IX_LichSuHuyDichVu_MaDatSan_NgayHuy",
            table: "LichSuHuyDichVu",
            columns: new[] { "MaDatSan", "NgayHuy" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "LichSuHuyDichVu");
}

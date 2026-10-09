using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemDanhMucDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DichVu",
                columns: table => new
                {
                    MaDichVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDichVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Vietnamese_100_CI_AS"),
                    DonViTinh = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVu", x => x.MaDichVu);
                    table.CheckConstraint("CK_DichVu_DonGia", "[DonGia] >= 0");
                    table.CheckConstraint("CK_DichVu_DonViTinh", "LEN(LTRIM(RTRIM([DonViTinh]))) > 0");
                    table.CheckConstraint("CK_DichVu_TenDichVu", "LEN(LTRIM(RTRIM([TenDichVu]))) > 0");
                    table.CheckConstraint("CK_DichVu_TrangThai", "[TrangThai] IN (0, 1)");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DichVu");
        }
    }
}

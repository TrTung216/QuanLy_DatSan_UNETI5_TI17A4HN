using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemChiTietDatSan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChiTietDatSan",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDatSan = table.Column<int>(type: "int", nullable: false),
                    MaDichVu = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDatSan", x => x.MaChiTiet);
                    table.CheckConstraint("CK_ChiTietDatSan_DonGia", "[DonGia] >= 0");
                    table.CheckConstraint("CK_ChiTietDatSan_SoLuong", "[SoLuong] > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietDatSan_DatSan_MaDatSan",
                        column: x => x.MaDatSan,
                        principalTable: "DatSan",
                        principalColumn: "MaDatSan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDatSan_DichVu_MaDichVu",
                        column: x => x.MaDichVu,
                        principalTable: "DichVu",
                        principalColumn: "MaDichVu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatSan_MaDatSan_MaDichVu",
                table: "ChiTietDatSan",
                columns: new[] { "MaDatSan", "MaDichVu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDatSan_MaDichVu",
                table: "ChiTietDatSan",
                column: "MaDichVu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDatSan");
        }
    }
}

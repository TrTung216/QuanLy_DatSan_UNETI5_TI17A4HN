using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemSanTheThao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SanTheThao",
                columns: table => new
                {
                    MaSan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenSan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaLoaiSan = table.Column<int>(type: "int", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TienIch = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<byte>(type: "tinyint", nullable: false),
                    GioMoCua = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    GioDongCua = table.Column<TimeOnly>(type: "time(0)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayBaoTri = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanTheThao", x => x.MaSan);
                    table.CheckConstraint("CK_SanTheThao_DonGia", "[DonGia] >= 0");
                    table.CheckConstraint("CK_SanTheThao_Gio", "[GioDongCua] > [GioMoCua]");
                    table.CheckConstraint("CK_SanTheThao_TenSan", "LEN(LTRIM(RTRIM([TenSan]))) > 0");
                    table.CheckConstraint("CK_SanTheThao_TrangThai", "[TrangThai] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_SanTheThao_LoaiSan_MaLoaiSan",
                        column: x => x.MaLoaiSan,
                        principalTable: "LoaiSan",
                        principalColumn: "MaLoaiSan");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SanTheThao_MaLoaiSan",
                table: "SanTheThao",
                column: "MaLoaiSan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SanTheThao");
        }
    }
}

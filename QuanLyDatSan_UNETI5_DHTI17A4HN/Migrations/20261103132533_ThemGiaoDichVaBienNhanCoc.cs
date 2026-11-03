using System;
using Microsoft.EntityFrameworkCore.Migrations;

// M1: Trần Trọng Tùng; MSSV: 23103100202. Tích hợp schema cọc M4; giữ tiền và trạng thái đơn cũ, mặc định chính sách 0.

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemGiaoDichVaBienNhanCoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhienBanChinhSachCoc",
                table: "DatSan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TienCocYeuCau",
                table: "DatSan",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "GiaoDichCoc",
                columns: table => new
                {
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nguon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaiKhoanNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaGiaoDichNgoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NgayNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayGhi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaNguoiGhi = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    LoiNgoaiLe = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichCoc", x => x.MaGiaoDich);
                    table.CheckConstraint("CK_GiaoDichCoc_LoiNgoaiLe", "[LoiNgoaiLe] IS NULL OR [LoiNgoaiLe] BETWEEN 1 AND 19");
                    table.CheckConstraint("CK_GiaoDichCoc_SoTien", "[SoTien] > 0");
                    table.CheckConstraint("CK_GiaoDichCoc_ThongTin", "LEN(LTRIM(RTRIM([Nguon]))) > 0 AND LEN(LTRIM(RTRIM([TaiKhoanNhan]))) > 0 AND LEN(LTRIM(RTRIM([MaGiaoDichNgoai]))) >= 3 AND LEN(LTRIM(RTRIM([NoiDung]))) > 0");
                    table.CheckConstraint("CK_GiaoDichCoc_TrangThai", "[TrangThai] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_GiaoDichCoc_TaiKhoan_MaNguoiGhi",
                        column: x => x.MaNguoiGhi,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BienNhanCoc",
                columns: table => new
                {
                    MaBienNhan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDatSan = table.Column<int>(type: "int", nullable: false),
                    MaGiaoDich = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaNguoiXacNhan = table.Column<int>(type: "int", nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nguon = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaiKhoanNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaGiaoDichNgoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BienNhanCoc", x => x.MaBienNhan);
                    table.CheckConstraint("CK_BienNhanCoc_SoTien", "[SoTien] > 0");
                    table.CheckConstraint("CK_BienNhanCoc_ThongTin", "LEN(LTRIM(RTRIM([Nguon]))) > 0 AND LEN(LTRIM(RTRIM([TaiKhoanNhan]))) > 0 AND LEN(LTRIM(RTRIM([MaGiaoDichNgoai]))) >= 3 AND LEN(LTRIM(RTRIM([NoiDung]))) > 0");
                    table.ForeignKey(
                        name: "FK_BienNhanCoc_DatSan_MaDatSan",
                        column: x => x.MaDatSan,
                        principalTable: "DatSan",
                        principalColumn: "MaDatSan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BienNhanCoc_GiaoDichCoc_MaGiaoDich",
                        column: x => x.MaGiaoDich,
                        principalTable: "GiaoDichCoc",
                        principalColumn: "MaGiaoDich",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BienNhanCoc_TaiKhoan_MaNguoiXacNhan",
                        column: x => x.MaNguoiXacNhan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatSan_ChinhSachCoc",
                table: "DatSan",
                sql: "[PhienBanChinhSachCoc] IN (0, 1) AND [TienCocYeuCau] >= 0 AND ([PhienBanChinhSachCoc] = 1 OR [TienCocYeuCau] = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_BienNhanCoc_MaDatSan",
                table: "BienNhanCoc",
                column: "MaDatSan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BienNhanCoc_MaGiaoDich",
                table: "BienNhanCoc",
                column: "MaGiaoDich",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BienNhanCoc_MaNguoiXacNhan",
                table: "BienNhanCoc",
                column: "MaNguoiXacNhan");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichCoc_MaNguoiGhi",
                table: "GiaoDichCoc",
                column: "MaNguoiGhi");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichCoc_Nguon_TaiKhoanNhan_MaGiaoDichNgoai",
                table: "GiaoDichCoc",
                columns: new[] { "Nguon", "TaiKhoanNhan", "MaGiaoDichNgoai" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichCoc_NoiDung",
                table: "GiaoDichCoc",
                column: "NoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BienNhanCoc");

            migrationBuilder.DropTable(
                name: "GiaoDichCoc");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatSan_ChinhSachCoc",
                table: "DatSan");

            migrationBuilder.DropColumn(
                name: "PhienBanChinhSachCoc",
                table: "DatSan");

            migrationBuilder.DropColumn(
                name: "TienCocYeuCau",
                table: "DatSan");
        }
    }
}

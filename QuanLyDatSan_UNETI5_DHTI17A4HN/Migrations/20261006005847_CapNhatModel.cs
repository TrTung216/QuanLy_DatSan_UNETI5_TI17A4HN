using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemKhachHangDatSan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    MaKhachHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: true),
                    GioiTinh = table.Column<int>(type: "int", nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiemTichLuy = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhachHang", x => x.MaKhachHang);
                    table.CheckConstraint("CK_KhachHang_DiemTichLuy", "[DiemTichLuy] >= 0");
                    table.CheckConstraint("CK_KhachHang_Email", "LEN(LTRIM(RTRIM([Email]))) > 0");
                    table.CheckConstraint("CK_KhachHang_GioiTinh", "[GioiTinh] IS NULL OR [GioiTinh] IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_KhachHang_HoTen", "LEN(LTRIM(RTRIM([HoTen]))) > 0");
                    table.CheckConstraint("CK_KhachHang_SoDienThoai", "LEN(LTRIM(RTRIM([SoDienThoai]))) > 0");
                    table.CheckConstraint("CK_KhachHang_TrangThai", "[TrangThai] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_KhachHang_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatSan",
                columns: table => new
                {
                    MaDatSan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhachHang = table.Column<int>(type: "int", nullable: false),
                    MaSan = table.Column<int>(type: "int", nullable: false),
                    NgayDat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TienCoc = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHuy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoHuy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TienSan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TienDichVu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatSan", x => x.MaDatSan);
                    table.CheckConstraint("CK_DatSan_ThoiGian", "[GioKetThuc] > [GioBatDau] AND CAST([GioBatDau] AS date) = CAST([GioKetThuc] AS date)");
                    table.CheckConstraint("CK_DatSan_Tien", "[DonGia] >= 0 AND [TienCoc] >= 0");
                    table.CheckConstraint("CK_DatSan_TrangThai", "[TrangThai] IN (0, 1, 2, 3, 4)");
                    table.ForeignKey(
                        name: "FK_DatSan_KhachHang_MaKhachHang",
                        column: x => x.MaKhachHang,
                        principalTable: "KhachHang",
                        principalColumn: "MaKhachHang",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatSan_SanTheThao_MaSan",
                        column: x => x.MaSan,
                        principalTable: "SanTheThao",
                        principalColumn: "MaSan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatSan_MaKhachHang_GioBatDau",
                table: "DatSan",
                columns: new[] { "MaKhachHang", "GioBatDau" });

            migrationBuilder.CreateIndex(
                name: "IX_DatSan_MaSan_GioBatDau_GioKetThuc",
                table: "DatSan",
                columns: new[] { "MaSan", "GioBatDau", "GioKetThuc" });

            migrationBuilder.CreateIndex(
                name: "IX_KhachHang_MaTaiKhoan",
                table: "KhachHang",
                column: "MaTaiKhoan",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatSan");

            migrationBuilder.DropTable(
                name: "KhachHang");

        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

// M1: Trần Trọng Tùng - 23103100202. Nền ảnh M1/M2 nối tiếp migration cọc.

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class ThemAnhDaiDienSanVaLoaiSan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuongDanAnh",
                table: "SanTheThao",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDanAnh",
                table: "LoaiSan",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LoaiSan",
                keyColumn: "MaLoaiSan",
                keyValue: 1,
                column: "DuongDanAnh",
                value: null);

            migrationBuilder.UpdateData(
                table: "LoaiSan",
                keyColumn: "MaLoaiSan",
                keyValue: 2,
                column: "DuongDanAnh",
                value: null);

            migrationBuilder.UpdateData(
                table: "LoaiSan",
                keyColumn: "MaLoaiSan",
                keyValue: 3,
                column: "DuongDanAnh",
                value: null);

            migrationBuilder.UpdateData(
                table: "LoaiSan",
                keyColumn: "MaLoaiSan",
                keyValue: 4,
                column: "DuongDanAnh",
                value: null);

            migrationBuilder.UpdateData(
                table: "LoaiSan",
                keyColumn: "MaLoaiSan",
                keyValue: 5,
                column: "DuongDanAnh",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuongDanAnh",
                table: "SanTheThao");

            migrationBuilder.DropColumn(
                name: "DuongDanAnh",
                table: "LoaiSan");
        }
    }
}

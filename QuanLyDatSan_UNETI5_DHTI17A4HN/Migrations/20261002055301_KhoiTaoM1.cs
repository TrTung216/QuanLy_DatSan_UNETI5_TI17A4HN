using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTaoM1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoaiSan",
                columns: table => new
                {
                    MaLoaiSan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Vietnamese_100_CI_AS"),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SoNguoiToiDa = table.Column<int>(type: "int", nullable: false),
                    DonGiaTheoGio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiSan", x => x.MaLoaiSan);
                    table.CheckConstraint("CK_LoaiSan_DonGiaTheoGio", "[DonGiaTheoGio] >= 0");
                    table.CheckConstraint("CK_LoaiSan_SoNguoiToiDa", "[SoNguoiToiDa] > 0");
                    table.CheckConstraint("CK_LoaiSan_TenLoai", "LEN(LTRIM(RTRIM([TenLoai]))) > 0");
                    table.CheckConstraint("CK_LoaiSan_TrangThai", "[TrangThai] IN (0, 1)");
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, collation: "Vietnamese_100_CI_AS"),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    VaiTro = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                    table.CheckConstraint("CK_TaiKhoan_TenDangNhap", "LEN(LTRIM(RTRIM([TenDangNhap]))) > 0");
                    table.CheckConstraint("CK_TaiKhoan_TrangThai", "[TrangThai] IN (0, 1)");
                    table.CheckConstraint("CK_TaiKhoan_VaiTro", "[VaiTro] IN (1, 2, 3)");
                });

            migrationBuilder.InsertData(
                table: "LoaiSan",
                columns: new[] { "MaLoaiSan", "DonGiaTheoGio", "MoTa", "SoNguoiToiDa", "TenLoai", "TrangThai" },
                values: new object[,]
                {
                    { 1, 200000m, null, 10, "Bóng đá 5 người", 1 },
                    { 2, 300000m, null, 14, "Bóng đá 7 người", 1 },
                    { 3, 80000m, null, 4, "Cầu lông", 1 },
                    { 4, 150000m, null, 10, "Bóng rổ", 1 },
                    { 5, 120000m, null, 4, "Quần vợt", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoaiSan_TenLoai",
                table: "LoaiSan",
                column: "TenLoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoaiSan");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}

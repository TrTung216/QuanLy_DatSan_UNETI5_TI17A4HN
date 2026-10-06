// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ soạn mã.
// Nội dung: ánh xạ dữ liệu M1, ràng buộc SQL Server và loại sân mẫu.
// M2: Phan Giang Tâm; MSSV: 23103100196.
// Nội dung: đăng ký sân, cấu hình bảng, quan hệ loại sân và chuẩn hóa dữ liệu.
// M3: Nguyễn Văn Quý; MSSV: 23103100181. ánh xạ, ràng buộc và chuẩn hóa dữ liệu KhachHang, ánh xạ DatSan.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // CI: không phân biệt hoa/thường; AS: phân biệt dấu tiếng Việt.
    public const string CollationTen = "Vietnamese_100_CI_AS";

    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
    public DbSet<LoaiSan> LoaiSans => Set<LoaiSan>();
    public DbSet<SanTheThao> SanTheThaos => Set<SanTheThao>();
    public DbSet<KhachHang> KhachHangs => Set<KhachHang>();
    public DbSet<DatSan> DatSans => Set<DatSan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.ToTable("TaiKhoan", table =>
            {
                table.HasCheckConstraint("CK_TaiKhoan_VaiTro", "[VaiTro] IN (1, 2, 3)");
                table.HasCheckConstraint("CK_TaiKhoan_TrangThai", "[TrangThai] IN (0, 1)");
                table.HasCheckConstraint("CK_TaiKhoan_TenDangNhap", "LEN(LTRIM(RTRIM([TenDangNhap]))) > 0");
            });
            entity.HasKey(x => x.MaTaiKhoan);
            entity.Property(x => x.TenDangNhap).HasMaxLength(50).UseCollation(CollationTen).IsRequired();
            entity.HasIndex(x => x.TenDangNhap).IsUnique();
            entity.Property(x => x.MatKhau).HasMaxLength(255).IsRequired();
            entity.Property(x => x.HoTen).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
        });
        modelBuilder.Entity<SanTheThao>(entity =>
        {
            entity.ToTable("SanTheThao", table =>
            {
                table.HasCheckConstraint(
                    "CK_SanTheThao_TenSan",
                    "LEN(LTRIM(RTRIM([TenSan]))) > 0");

                table.HasCheckConstraint(
                    "CK_SanTheThao_DonGia",
                    "[DonGia] >= 0");

                table.HasCheckConstraint(
                    "CK_SanTheThao_TrangThai",
                    "[TrangThai] IN (1, 2, 3)");

                table.HasCheckConstraint(
                    "CK_SanTheThao_Gio",
                    "[GioDongCua] > [GioMoCua]");
            });

            entity.HasKey(x => x.MaSan);

            entity.Property(x => x.TenSan)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.DiaChi)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(x => x.TienIch).HasMaxLength(1000);
            entity.Property(x => x.GhiChu).HasMaxLength(1000);
            entity.Property(x => x.DonGia).HasPrecision(18, 2);
            entity.Property(x => x.TrangThai).HasConversion<byte>();
            entity.Property(x => x.GioMoCua).HasColumnType("time(0)");
            entity.Property(x => x.GioDongCua).HasColumnType("time(0)");
            entity.Property(x => x.NgayBaoTri).HasColumnType("date");

            entity.HasIndex(x => x.MaLoaiSan);

            entity.HasOne(x => x.LoaiSan)
                .WithMany(x => x.DanhSachSan)
                .HasForeignKey(x => x.MaLoaiSan)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<LoaiSan>(entity =>
        {
            entity.ToTable("LoaiSan", table =>
            {
                table.HasCheckConstraint("CK_LoaiSan_SoNguoiToiDa", "[SoNguoiToiDa] > 0");
                table.HasCheckConstraint("CK_LoaiSan_DonGiaTheoGio", "[DonGiaTheoGio] >= 0");
                table.HasCheckConstraint("CK_LoaiSan_TrangThai", "[TrangThai] IN (0, 1)");
                table.HasCheckConstraint("CK_LoaiSan_TenLoai", "LEN(LTRIM(RTRIM([TenLoai]))) > 0");
            });
            entity.HasKey(x => x.MaLoaiSan);
            entity.Property(x => x.TenLoai).HasMaxLength(100).UseCollation(CollationTen).IsRequired();
            entity.HasIndex(x => x.TenLoai).IsUnique();
            entity.Property(x => x.MoTa).HasMaxLength(1000);
            entity.Property(x => x.DonGiaTheoGio).HasPrecision(18, 2);
            // Dữ liệu giả phục vụ học tập và kiểm thử, không phải bảng giá thực tế.
            entity.HasData(
                new LoaiSan { MaLoaiSan = 1, TenLoai = "Bóng đá 5 người", SoNguoiToiDa = 10, DonGiaTheoGio = 200000m },
                new LoaiSan { MaLoaiSan = 2, TenLoai = "Bóng đá 7 người", SoNguoiToiDa = 14, DonGiaTheoGio = 300000m },
                new LoaiSan { MaLoaiSan = 3, TenLoai = "Cầu lông", SoNguoiToiDa = 4, DonGiaTheoGio = 80000m },
                new LoaiSan { MaLoaiSan = 4, TenLoai = "Bóng rổ", SoNguoiToiDa = 10, DonGiaTheoGio = 150000m },
                new LoaiSan { MaLoaiSan = 5, TenLoai = "Quần vợt", SoNguoiToiDa = 4, DonGiaTheoGio = 120000m });
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.ToTable("KhachHang", table =>
            {
                table.HasCheckConstraint("CK_KhachHang_TrangThai", "[TrangThai] IN (0, 1)");
                table.HasCheckConstraint("CK_KhachHang_GioiTinh", "[GioiTinh] IS NULL OR [GioiTinh] IN (0, 1, 2, 3)");
                table.HasCheckConstraint("CK_KhachHang_DiemTichLuy", "[DiemTichLuy] >= 0");
                table.HasCheckConstraint("CK_KhachHang_HoTen", "LEN(LTRIM(RTRIM([HoTen]))) > 0");
                table.HasCheckConstraint("CK_KhachHang_SoDienThoai", "LEN(LTRIM(RTRIM([SoDienThoai]))) > 0");
                table.HasCheckConstraint("CK_KhachHang_Email", "LEN(LTRIM(RTRIM([Email]))) > 0");
            });
            entity.HasKey(x => x.MaKhachHang);
            entity.Property(x => x.HoTen).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SoDienThoai).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.DiaChi).HasMaxLength(255);
            entity.Property(x => x.GhiChu).HasMaxLength(1000);
            entity.HasIndex(x => x.MaTaiKhoan).IsUnique();
            entity.HasOne(x => x.TaiKhoan)
                .WithOne(x => x.KhachHang)
                .HasForeignKey<KhachHang>(x => x.MaTaiKhoan)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DatSan>(entity =>
        {
            entity.ToTable("DatSan", table =>
            {
                table.HasCheckConstraint("CK_DatSan_ThoiGian",
                    "[GioKetThuc] > [GioBatDau] AND CAST([GioBatDau] AS date) = CAST([GioKetThuc] AS date)");
                table.HasCheckConstraint("CK_DatSan_Tien", "[DonGia] >= 0 AND [TienCoc] >= 0");
                table.HasCheckConstraint("CK_DatSan_TrangThai", "[TrangThai] IN (0, 1, 2, 3, 4)");
            });
            entity.HasKey(x => x.MaDatSan);
            entity.Property(x => x.DonGia).HasPrecision(18, 2);
            entity.Property(x => x.TienCoc).HasPrecision(18, 2);
            entity.Property(x => x.TienSan).HasPrecision(18, 2);
            entity.Property(x => x.TienDichVu).HasPrecision(18, 2);
            entity.Property(x => x.TongTien).HasPrecision(18, 2);
            entity.Property(x => x.LyDoHuy).HasMaxLength(1000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.MaSan, x.GioBatDau, x.GioKetThuc });
            entity.HasIndex(x => new { x.MaKhachHang, x.GioBatDau });
            entity.HasOne(x => x.KhachHang)
                .WithMany(x => x.DanhSachDatSan)
                .HasForeignKey(x => x.MaKhachHang)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SanTheThao)
                .WithMany()
                .HasForeignKey(x => x.MaSan)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ChuanHoaDuLieu()
    {
        foreach (var entry in ChangeTracker.Entries<TaiKhoan>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.TenDangNhap = entry.Entity.TenDangNhap.Trim();
            entry.Entity.HoTen = entry.Entity.HoTen.Trim();
            entry.Entity.Email = entry.Entity.Email.Trim();
            // Không Trim hoặc biến đổi giá trị băm mật khẩu.
        }

        foreach (var entry in ChangeTracker.Entries<LoaiSan>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.TenLoai = entry.Entity.TenLoai.Trim();
            entry.Entity.MoTa = entry.Entity.MoTa?.Trim();
        }
        foreach (var entry in ChangeTracker.Entries<SanTheThao>()
             .Where(x => x.State is EntityState.Added
                 or EntityState.Modified))
        {
            entry.Entity.TenSan = entry.Entity.TenSan.Trim();
            entry.Entity.DiaChi = entry.Entity.DiaChi.Trim();
            entry.Entity.TienIch = entry.Entity.TienIch?.Trim();
            entry.Entity.GhiChu = entry.Entity.GhiChu?.Trim();
        }

        foreach (var entry in ChangeTracker.Entries<KhachHang>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.HoTen = entry.Entity.HoTen.Trim();
            entry.Entity.Email = entry.Entity.Email.Trim();
            entry.Entity.SoDienThoai = entry.Entity.SoDienThoai.Trim();
            entry.Entity.DiaChi = entry.Entity.DiaChi?.Trim();
            entry.Entity.GhiChu = entry.Entity.GhiChu?.Trim();
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ChuanHoaDuLieu();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChuanHoaDuLieu();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

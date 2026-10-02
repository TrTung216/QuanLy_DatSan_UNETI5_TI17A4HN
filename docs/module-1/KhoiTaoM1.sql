IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [LoaiSan] (
    [MaLoaiSan] int NOT NULL IDENTITY,
    [TenLoai] nvarchar(100) COLLATE Vietnamese_100_CI_AS NOT NULL,
    [MoTa] nvarchar(1000) NULL,
    [SoNguoiToiDa] int NOT NULL,
    [DonGiaTheoGio] decimal(18,2) NOT NULL,
    [TrangThai] int NOT NULL,
    CONSTRAINT [PK_LoaiSan] PRIMARY KEY ([MaLoaiSan]),
    CONSTRAINT [CK_LoaiSan_DonGiaTheoGio] CHECK ([DonGiaTheoGio] >= 0),
    CONSTRAINT [CK_LoaiSan_SoNguoiToiDa] CHECK ([SoNguoiToiDa] > 0),
    CONSTRAINT [CK_LoaiSan_TenLoai] CHECK (LEN(LTRIM(RTRIM([TenLoai]))) > 0),
    CONSTRAINT [CK_LoaiSan_TrangThai] CHECK ([TrangThai] IN (0, 1))
);

CREATE TABLE [TaiKhoan] (
    [MaTaiKhoan] int NOT NULL IDENTITY,
    [TenDangNhap] nvarchar(50) COLLATE Vietnamese_100_CI_AS NOT NULL,
    [MatKhau] nvarchar(255) NOT NULL,
    [HoTen] nvarchar(100) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [VaiTro] int NOT NULL,
    [TrangThai] int NOT NULL,
    CONSTRAINT [PK_TaiKhoan] PRIMARY KEY ([MaTaiKhoan]),
    CONSTRAINT [CK_TaiKhoan_TenDangNhap] CHECK (LEN(LTRIM(RTRIM([TenDangNhap]))) > 0),
    CONSTRAINT [CK_TaiKhoan_TrangThai] CHECK ([TrangThai] IN (0, 1)),
    CONSTRAINT [CK_TaiKhoan_VaiTro] CHECK ([VaiTro] IN (1, 2, 3))
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MaLoaiSan', N'DonGiaTheoGio', N'MoTa', N'SoNguoiToiDa', N'TenLoai', N'TrangThai') AND [object_id] = OBJECT_ID(N'[LoaiSan]'))
    SET IDENTITY_INSERT [LoaiSan] ON;
INSERT INTO [LoaiSan] ([MaLoaiSan], [DonGiaTheoGio], [MoTa], [SoNguoiToiDa], [TenLoai], [TrangThai])
VALUES (1, 200000.0, NULL, 10, N'Bóng đá 5 người', 1),
(2, 300000.0, NULL, 14, N'Bóng đá 7 người', 1),
(3, 80000.0, NULL, 4, N'Cầu lông', 1),
(4, 150000.0, NULL, 10, N'Bóng rổ', 1),
(5, 120000.0, NULL, 4, N'Quần vợt', 1);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MaLoaiSan', N'DonGiaTheoGio', N'MoTa', N'SoNguoiToiDa', N'TenLoai', N'TrangThai') AND [object_id] = OBJECT_ID(N'[LoaiSan]'))
    SET IDENTITY_INSERT [LoaiSan] OFF;

CREATE UNIQUE INDEX [IX_LoaiSan_TenLoai] ON [LoaiSan] ([TenLoai]);

CREATE UNIQUE INDEX [IX_TaiKhoan_TenDangNhap] ON [TaiKhoan] ([TenDangNhap]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261002055301_KhoiTaoM1', N'10.0.10');

COMMIT;
GO

# M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ soạn kiểm thử.
# Kiểm thử ràng buộc trên database riêng đã áp dụng migration. Mọi dữ liệu thử được rollback.
param(
    [string]$ConnectionString = 'Server=(localdb)\MSSQLLocalDB;Database=QuanLyDatSan_M1_KiemThu_20261002;Integrated Security=True;Connect Timeout=5'
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Data
$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$connection.Open()
try {
    $countCommand = $connection.CreateCommand()
    $countCommand.CommandText = 'SELECT COUNT(*) FROM LoaiSan WHERE MaLoaiSan BETWEEN 1 AND 5 AND TrangThai = 1'
    if ($countCommand.ExecuteScalar() -ne 5) { throw 'Thieu 5 loai san mau hoat dong.' }
    Write-Output 'PASS: 5 loai san mau hoat dong'

    $cases = @(
        @{ Name='Gia bang 0 hop le'; Sql="INSERT LoaiSan (TenLoai, SoNguoiToiDa, DonGiaTheoGio, TrangThai) VALUES (N'M1_TEST', 1, 0, 1)"; Error=0 },
        @{ Name='Ten loai trung khong phan biet hoa thuong'; Sql="INSERT LoaiSan (TenLoai, SoNguoiToiDa, DonGiaTheoGio, TrangThai) SELECT UPPER(TenLoai), SoNguoiToiDa, DonGiaTheoGio, TrangThai FROM LoaiSan WHERE MaLoaiSan=1"; Error=2601 },
        @{ Name='Chan gia am'; Sql="INSERT LoaiSan (TenLoai, SoNguoiToiDa, DonGiaTheoGio, TrangThai) VALUES (N'M1_TEST', 1, -1, 1)"; Error=547 },
        @{ Name='Chan so nguoi bang 0'; Sql="INSERT LoaiSan (TenLoai, SoNguoiToiDa, DonGiaTheoGio, TrangThai) VALUES (N'M1_TEST', 0, 100, 1)"; Error=547 },
        @{ Name='Chan ten rong'; Sql="INSERT LoaiSan (TenLoai, SoNguoiToiDa, DonGiaTheoGio, TrangThai) VALUES (N'   ', 1, 100, 1)"; Error=547 },
        @{ Name='Chan trang thai loai san sai'; Sql="INSERT LoaiSan (TenLoai, SoNguoiToiDa, DonGiaTheoGio, TrangThai) VALUES (N'M1_TEST', 1, 100, 2)"; Error=547 },
        @{ Name='Chan vai tro sai'; Sql="INSERT TaiKhoan (TenDangNhap, MatKhau, HoTen, Email, VaiTro, TrangThai) VALUES ('M1_TEST', 'schema_test_hash', N'Test', 'test@example.test', 4, 1)"; Error=547 },
        @{ Name='Chan trang thai tai khoan sai'; Sql="INSERT TaiKhoan (TenDangNhap, MatKhau, HoTen, Email, VaiTro, TrangThai) VALUES ('M1_TEST', 'schema_test_hash', N'Test', 'test@example.test', 1, 2)"; Error=547 },
        @{ Name='Ten dang nhap trung khong phan biet hoa thuong'; Sql="INSERT TaiKhoan (TenDangNhap, MatKhau, HoTen, Email, VaiTro, TrangThai) VALUES ('m1_schema_test', 'schema_test_hash', N'Test', 'test@example.test', 1, 1), ('M1_SCHEMA_TEST', 'schema_test_hash', N'Test', 'test@example.test', 1, 1)"; Error=2601 }
    )
    foreach ($case in $cases) {
        $transaction = $connection.BeginTransaction()
        try {
            $command = $connection.CreateCommand()
            $command.Transaction = $transaction
            $command.CommandText = $case.Sql
            $actualError = 0
            try { $null = $command.ExecuteNonQuery() }
            catch [System.Data.SqlClient.SqlException] { $actualError = $_.Exception.Number }
            if ($actualError -ne $case.Error) {
                throw "$($case.Name): expected SQL error $($case.Error), received $actualError"
            }
            Write-Output "PASS: $($case.Name)"
        }
        finally { $transaction.Rollback(); $transaction.Dispose() }
    }
}
finally { $connection.Dispose() }

// M1: Trần Trọng Tùng; MSSV 23103100202. Tích hợp giao diện khách hàng với hợp đồng dịch vụ M5.
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DichVu;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.KhachHang;

public sealed class DichVuDonKhachHangViewModel
{
    public ThemDichVuTrongDonViewModel Form { get; set; } = new();
    public bool ChoPhepThem { get; set; }
    public decimal TongTienDichVu { get; set; }
    public IReadOnlyList<LuaChonDichVuKhachHang> LuaChon { get; set; } = [];
    public IReadOnlyList<DichVuCaNhanViewModel> Dong { get; set; } = [];
}

public sealed record LuaChonDichVuKhachHang(int MaDichVu, string TenDichVu, string DonViTinh, decimal DonGia);

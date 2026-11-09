// M3: Nguyễn Văn Quý; MSSV: 23103100181. dữ liệu hiển thị lịch đặt cá nhân và chi tiết đơn của khách hàng.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class DonDatDongViewModel
{
    public int MaDatSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public TrangThaiDatSan TrangThai { get; set; }
}

public class ChiTietDonViewModel
{
    public int MaDatSan { get; set; }
    public string TenSan { get; set; } = string.Empty;
    public string TenLoai { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public DateTime NgayDat { get; set; }
    public DateTime GioBatDau { get; set; }
    public DateTime GioKetThuc { get; set; }
    public decimal DonGia { get; set; }
    public decimal TienCoc { get; set; }
    public TrangThaiDatSan TrangThai { get; set; }
    public DateTime? NgayXacNhan { get; set; }
    public DateTime? NgayHuy { get; set; }
    public DateTime? NgayHoanThanh { get; set; }
    public string? LyDoHuy { get; set; }
    public decimal? TienSan { get; set; }
    public decimal? TienDichVu { get; set; }
    public decimal? TongTien { get; set; }
    public decimal TienSanDuKien { get; set; }
    public string PhienBanDon { get; set; } = string.Empty;
    public bool CoTheHuy { get; set; }
    public string? LyDoHuyDaNhap { get; set; }
    public CocDonViewModel? Coc { get; set; }
    public string? LyDoKhongTheHuy { get; set; }
    public string? NhanPhanConLai { get; set; }
}

public sealed class HuyDonCaNhanViewModel
{
    [Range(1, int.MaxValue)]
    public int MaDon { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do hủy.")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Lý do tối đa 1000 ký tự.")]
    [Display(Name = "Lý do hủy")]
    public string LyDo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string PhienBan { get; set; } = string.Empty;
}

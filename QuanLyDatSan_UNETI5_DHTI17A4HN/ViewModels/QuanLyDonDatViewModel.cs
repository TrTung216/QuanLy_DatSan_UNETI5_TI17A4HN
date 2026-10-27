// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-02/03/04 - bộ lọc, chi tiết, tiền dự kiến và điều kiện xử lý/ghi cọc/hoàn thành đơn.
// Khai báo lý do từ chối/hủy, RowVersion, validation POST và dữ liệu nhập cần giữ lại khi có lỗi.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public class LocDonQuanLyViewModel : IValidatableObject
{
    [StringLength(200, ErrorMessage = "Từ khóa tối đa 200 ký tự.")]
    [Display(Name = "Khách hàng hoặc sân")]
    public string? TuKhoa { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Loại sân không hợp lệ.")]
    [Display(Name = "Loại sân")]
    public int? MaLoaiSan { get; set; }
    [EnumDataType(typeof(TrangThaiDatSan), ErrorMessage = "Trạng thái không hợp lệ.")]
    [Display(Name = "Trạng thái")]
    public TrangThaiDatSan? TrangThai { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày sử dụng từ")]
    public DateOnly? TuNgay { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày sử dụng đến")]
    public DateOnly? DenNgay { get; set; }
    [EnumDataType(typeof(SapXepDonQuanLy), ErrorMessage = "Cách sắp xếp không hợp lệ.")]
    [Display(Name = "Sắp xếp")]
    public SapXepDonQuanLy SapXep { get; set; } = SapXepDonQuanLy.NgayGiam;
    public int Trang { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TuNgay > DenNgay)
            yield return new ValidationResult("Ngày đến phải bằng hoặc sau ngày bắt đầu.", [nameof(DenNgay)]);
    }

    public Dictionary<string, string> ThamSoTrang(int trang)
    {
        var result = new Dictionary<string, string>
        {
            ["trang"] = trang.ToString(CultureInfo.InvariantCulture),
            ["sapXep"] = SapXep.ToString()
        };
        if (!string.IsNullOrWhiteSpace(TuKhoa)) result["tuKhoa"] = TuKhoa;
        if (MaLoaiSan.HasValue) result["maLoaiSan"] = MaLoaiSan.Value.ToString(CultureInfo.InvariantCulture);
        if (TrangThai.HasValue) result["trangThai"] = TrangThai.Value.ToString();
        if (TuNgay.HasValue) result["tuNgay"] = TuNgay.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (DenNgay.HasValue) result["denNgay"] = DenNgay.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return result;
    }
}

public sealed class DanhSachDonQuanLyViewModel : LocDonQuanLyViewModel
{
    [BindNever] public IReadOnlyList<DonQuanLyDongViewModel> Dong { get; set; } = [];
    [BindNever] public IReadOnlyList<LoaiSanLocViewModel> LoaiSan { get; set; } = [];
    [BindNever] public int TongSo { get; set; }
    [BindNever] public int TongTrang { get; set; } = 1;
}

public sealed record LoaiSanLocViewModel(int MaLoaiSan, string TenLoai);
public sealed record DonQuanLyDongViewModel(int MaDatSan, string TenKhachHang, string TenSan, string TenLoai,
    DateTime NgayDat, DateTime GioBatDau, DateTime GioKetThuc, TrangThaiDatSan TrangThai, decimal TienCoc);
public sealed class ChiTietDonQuanLyViewModel : ChiTietDonViewModel
{
    public int MaKhachHang { get; init; }
    public string TenKhachHang { get; init; } = "";
    public string SoDienThoai { get; init; } = "";
    public string Email { get; init; } = "";
    public string QuayLai { get; init; } = "";
    public string PhienBan { get; init; } = "";
    public bool CoTheXacNhan { get; init; }
    public bool CoTheTuChoi { get; init; }
    public bool CoTheNhanVienHuy { get; init; }
    public bool CoTheGhiCoc { get; init; }
    public bool CoTheHoanThanh { get; init; }
    public decimal TienDichVuDuKien { get; init; }
    public decimal TongTienDuKien { get; init; }
    public decimal ConPhaiThu => TrangThai == TrangThaiDatSan.HoanThanh ? 0m
        : Math.Max(0m, (TongTien ?? TongTienDuKien) - TienCoc);
    [BindNever] public string? TongCocNhap { get; set; }
    [BindNever] public string? LyDoTuChoiNhap { get; set; }
    [BindNever] public string? LyDoHuyNhap { get; set; }
}

public sealed class HuyDonQuanLyViewModel
{
    [Range(1, int.MaxValue)]
    public int MaDon { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do.")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Lý do tối đa 1000 ký tự.")]
    [Display(Name = "Lý do")]
    public string LyDo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Thiếu phiên bản đơn. Vui lòng tải lại trang.")]
    public string PhienBan { get; set; } = string.Empty;

    public string? QuayLai { get; set; }
}

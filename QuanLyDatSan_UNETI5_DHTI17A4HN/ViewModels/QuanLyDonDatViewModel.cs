// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: Dữ liệu và validation bộ lọc; dữ liệu hiển thị danh sách/chi tiết đơn và tham số giữ trang.

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
}

// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Form quản lý đối soát cọc.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Việt hóa nhãn/lỗi form, giữ nguyên giới hạn validation.
using System.ComponentModel.DataAnnotations;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels.DonDat.QuanLy;

public sealed class XacNhanCocViewModel : XuLyDonViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn giao dịch.")]
    public int MaGiaoDich { get; set; }
    public bool DaKiemTraGiaoDich { get; set; }
}

public sealed class TaoGiaoDichMoPhongViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Mã đơn không hợp lệ.")] public int MaDon { get; set; }
    [Display(Name = "Mã giao dịch ngoài")]
    [Required(ErrorMessage = "Vui lòng nhập mã giao dịch ngoài.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Mã giao dịch ngoài phải có từ 3 đến 100 ký tự.")]
    public string MaGiaoDichNgoai { get; set; } = "";
    [Display(Name = "Nội dung chuyển khoản")]
    [Required(ErrorMessage = "Vui lòng nhập nội dung chuyển khoản.")]
    [StringLength(100, ErrorMessage = "Nội dung chuyển khoản không được quá 100 ký tự.")]
    public string NoiDung { get; set; } = "";
    [Display(Name = "Số tiền")]
    [Required(ErrorMessage = "Vui lòng nhập số tiền.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "Số tiền phải lớn hơn 0 và không vượt quá giới hạn cho phép.")]
    public decimal? SoTien { get; set; }
    [Display(Name = "Thời điểm nhận")]
    [Required(ErrorMessage = "Vui lòng nhập thời điểm nhận.")]
    public DateTime? NgayNhan { get; set; }
}

public sealed class DoiSoatCocViewModel
{
    public required DoiSoatCocDto DuLieu { get; init; }
    public required string PhienBan { get; init; }
    public bool CoTheMoPhong { get; init; }
}

public static class ThongBaoDoiSoatCoc
{
    public static string Loi(MaLoiCoc loi) => loi switch
    {
        MaLoiCoc.GiaoDichKhongTonTai => "Không tìm thấy giao dịch. Hãy tải lại trang.",
        MaLoiCoc.GiaoDichKhongKhop => "Giao dịch không khớp đơn, nguồn hoặc tài khoản nhận.",
        MaLoiCoc.SoTienKhongKhop => "Số tiền giao dịch không khớp số cọc yêu cầu.",
        MaLoiCoc.ThoiDiemNhanKhongHopLe => "Thời điểm nhận tiền hoặc đối soát không hợp lệ; cần xử lý ngoại lệ.",
        MaLoiCoc.GiaoDichDaSuDung => "Giao dịch đã được sử dụng cho đơn khác.",
        MaLoiCoc.DonDaNhanCoc => "Đơn đã nhận cọc từ giao dịch khác.",
        MaLoiCoc.ChuaKiemTraGiaoDich => "Vui lòng xác nhận đã kiểm tra giao dịch.",
        MaLoiCoc.PhienBanDaCu => "Đơn đã thay đổi. Hãy kiểm tra lại thông tin trước khi xác nhận.",
        MaLoiCoc.TrungMaGiaoDich => "Mã giao dịch ngoài đã tồn tại. Kiểm tra giao dịch đã ghi trước khi tạo thêm.",
        MaLoiCoc.KhongApDungCoc => "Đơn không áp dụng đối soát cọc.",
        MaLoiCoc.MoPhongKhongDuocPhep or MaLoiCoc.ChuyenKhoanChuaSanSang => "Nguồn giao dịch mô phỏng chưa sẵn sàng.",
        MaLoiCoc.DuLieuTienKhongHopLe => "Dữ liệu tiền không hợp lệ. Cần kiểm tra trước khi đối soát.",
        _ => "Dữ liệu đối soát không hợp lệ. Vui lòng kiểm tra lại."
    };
}

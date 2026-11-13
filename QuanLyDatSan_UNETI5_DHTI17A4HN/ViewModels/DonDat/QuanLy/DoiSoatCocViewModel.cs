// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Form quản lý đối soát cọc.
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
    [Range(1, int.MaxValue)] public int MaDon { get; set; }
    [Required, StringLength(100, MinimumLength = 3)]
    public string MaGiaoDichNgoai { get; set; } = "";
    [Required, StringLength(100)] public string NoiDung { get; set; } = "";
    [Required, Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal? SoTien { get; set; }
    [Required] public DateTime? NgayNhan { get; set; }
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

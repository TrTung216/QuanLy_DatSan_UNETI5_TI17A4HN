// M3: Nguyễn Văn Quý; MSSV: 23103100181. ViewModel và hiển thị thông tin đặt cọc của khách hàng (chỉ đọc, dữ liệu tiền lấy từ service M4).
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public sealed class BienNhanCocViewModel
{
    public int MaBienNhan { get; set; }
    public decimal SoTien { get; set; }
    public string MaGiaoDichNgoai { get; set; } = string.Empty;
    public string Nguon { get; set; } = string.Empty;
    public DateTime NgayNhan { get; set; }
    public DateTime NgayXacNhan { get; set; }
}

public sealed class CocDonViewModel
{
    public int MaDatSan { get; set; }
    public TrangThaiDatSan TrangThaiDon { get; set; }
    public TrangThaiCocHienThi TrangThaiCoc { get; set; }
    public decimal TienSanDuKien { get; set; }
    public decimal TienCocYeuCau { get; set; }
    public decimal TienCocDaNhan { get; set; }
    // Tạm tính từ dữ liệu service: (tổng tiền nếu đã có, ngược lại tiền sân dự kiến) trừ cọc đã ghi nhận, không âm.
    public decimal ConPhaiThanhToanTamTinh { get; set; }
    public bool ConPhaiThanhToanLaTamTinh { get; set; }
    public string NoiDungChuyenKhoan { get; set; } = string.Empty;
    public bool CoTheChuyen { get; set; }
    public string? NoiDungQrMoPhong { get; set; }
    public BienNhanCocViewModel? BienNhan { get; set; }
}

public static class HienThiCoc
{
    public static string TenTrangThai(TrangThaiCocHienThi trangThai) => trangThai switch
    {
        TrangThaiCocHienThi.KhongApDung => "Đơn này không áp dụng cọc",
        TrangThaiCocHienThi.ChuaNhan => "Chưa nhận cọc",
        TrangThaiCocHienThi.ChoDoiSoat => "Chờ nhân viên đối soát",
        TrangThaiCocHienThi.DaNhan => "Đã nhận cọc",
        TrangThaiCocHienThi.CanXuLyNgoaiLe => "Cần xử lý ngoại lệ",
        TrangThaiCocHienThi.KhongSanSang => "Chuyển khoản chưa sẵn sàng",
        _ => string.Empty
    };

    public static string LopMau(TrangThaiCocHienThi trangThai) => trangThai switch
    {
        TrangThaiCocHienThi.ChuaNhan or TrangThaiCocHienThi.ChoDoiSoat => "ql-status ql-status--waiting",
        TrangThaiCocHienThi.DaNhan => "ql-status ql-status--confirmed",
        TrangThaiCocHienThi.CanXuLyNgoaiLe => "ql-status ql-status--rejected",
        _ => "ql-status ql-status--cancelled"
    };

    // Mã lỗi cọc dùng chung cho thao tác của khách; không lộ chi tiết nội bộ.
    public static string ThongBaoLoi(MaLoiCoc loi) => loi switch
    {
        MaLoiCoc.KhongDuQuyen => "Tài khoản hiện tại không đủ quyền thực hiện thao tác này.",
        MaLoiCoc.KhongTonTai => "Không tìm thấy đơn đặt sân.",
        MaLoiCoc.ChuyenKhoanChuaSanSang => "Chuyển khoản chưa sẵn sàng. Vui lòng liên hệ nhân viên.",
        MaLoiCoc.MoPhongKhongDuocPhep => "Chức năng mô phỏng không được phép sử dụng.",
        MaLoiCoc.KhongApDungCoc => "Đơn này không áp dụng cọc.",
        MaLoiCoc.DonDaNhanCoc => "Đơn đã được ghi nhận cọc.",
        MaLoiCoc.GiaoDichChoDoiSoat or MaLoiCoc.YeuCauDoiSoatCoc
            => "Đơn có giao dịch đang chờ nhân viên đối soát.",
        MaLoiCoc.DuLieuTienKhongHopLe => "Dữ liệu tiền của đơn chưa hợp lệ. Vui lòng liên hệ nhân viên.",
        _ => "Không thể xử lý thông tin cọc lúc này. Vui lòng thử lại hoặc liên hệ nhân viên."
    };
}

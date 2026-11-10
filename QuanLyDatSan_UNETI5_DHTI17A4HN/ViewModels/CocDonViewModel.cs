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
    // Chỉ có giá trị khi đơn đã hoàn thành; không có số tiền còn lại vì M3 không tự tính tiền.
    public string? NhanPhanConLai { get; set; }
    public string NoiDungChuyenKhoan { get; set; } = string.Empty;
    public bool CoTheChuyen { get; set; }
    public string? NoiDungQrMoPhong { get; set; }
    public BienNhanCocViewModel? BienNhan { get; set; }
    // Phiên bản đơn dạng Base64 cho nút Hủy ở màn hình QR; rỗng thì không hiện nút Hủy.
    public string PhienBanDon { get; set; } = string.Empty;
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

    // Đơn hoàn thành: khoản còn lại đã được thu khi chốt đơn. Không có số tiền, tránh khách hiểu cần trả thêm.
    public static string? NhanPhanConLai(TrangThaiDatSan trangThai) =>
        trangThai == TrangThaiDatSan.HoanThanh ? "Đã thu phần còn lại" : null;

    // Lý do không cho khách hủy; null nghĩa là giao diện được phép hiện nút (service vẫn kiểm tra lại khi POST).
    public static string? LyDoKhongTheHuy(TrangThaiCocHienThi trangThaiCoc, decimal tienCocDaGhi) => trangThaiCoc switch
    {
        TrangThaiCocHienThi.ChoDoiSoat
            => "Đơn có giao dịch cọc đang chờ nhân viên đối soát nên chưa thể hủy. Vui lòng liên hệ nhân viên.",
        TrangThaiCocHienThi.CanXuLyNgoaiLe
            => "Giao dịch cọc của đơn cần nhân viên xử lý thủ công nên chưa thể hủy. Vui lòng liên hệ nhân viên.",
        TrangThaiCocHienThi.DaNhan
            => "Đơn đã ghi nhận cọc nên cần nhân viên xử lý nếu muốn hủy. Vui lòng liên hệ nhân viên.",
        _ when tienCocDaGhi > 0m
            => "Đơn đã ghi nhận cọc nên cần nhân viên xử lý nếu muốn hủy. Vui lòng liên hệ nhân viên.",
        _ => null
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

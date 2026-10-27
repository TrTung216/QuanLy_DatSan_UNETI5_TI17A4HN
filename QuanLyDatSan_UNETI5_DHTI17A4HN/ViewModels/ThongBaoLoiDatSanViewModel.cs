// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: ánh xạ mã lỗi tạo đơn, xác nhận, từ chối, hủy và hoàn thành đơn sang trường form và thông báo tiếng Việt.
// Phát hiện mã lỗi chưa được ánh xạ hoặc thiếu dữ liệu để tạo thông báo.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

public static class ThongBaoLoiDatSanViewModel
{
    public static (string Truong, string ThongBao) AnhXa(LoiDatSan loi) => loi.Ma switch
    {
        MaLoiDatSan.HoSoKhongHoatDong => ("", "Hồ sơ hoặc tài khoản không đủ điều kiện để đặt sân."),
        MaLoiDatSan.BatDauKhongTuongLai => ("GioBatDau", "Thời gian bắt đầu phải ở tương lai."),
        MaLoiDatSan.KetThucKhongHopLe => ("GioKetThuc", "Giờ kết thúc phải sau giờ bắt đầu."),
        MaLoiDatSan.SanKhongTonTai => ("MaSan", "Sân không tồn tại."),
        MaLoiDatSan.SanKhongNhanDat => ("MaSan", "Sân hiện không nhận đặt."),
        MaLoiDatSan.SanBaoTriKhiTao => ("NgaySuDung", "Sân bảo trì vào ngày này."),
        MaLoiDatSan.TrungLichKhiTao => ("GioBatDau", "Khung giờ này đã có người đặt. Vui lòng chọn khung giờ hoặc sân khác."),
        MaLoiDatSan.ChiXacNhanDonCho => ("", "Chỉ có thể xác nhận đơn đang chờ xử lý."),
        MaLoiDatSan.QuaGioXacNhan => ("", "Không thể xác nhận vì giờ bắt đầu của đơn đã qua."),
        MaLoiDatSan.SanKhongHoatDong => ("", "Sân hoặc loại sân hiện không hoạt động."),
        MaLoiDatSan.NgoaiGioHoatDong => ("", "Thời gian đặt nằm ngoài giờ hoạt động của sân."),
        MaLoiDatSan.SanBaoTri => ("", "Sân được lên lịch bảo trì vào ngày này."),
        MaLoiDatSan.TrungLich => ("", "Khung giờ này đã bị một đơn khác giữ. Không thể xác nhận."),
        MaLoiDatSan.LyDoTuChoiKhongHopLe => ("", "Lý do từ chối là bắt buộc và tối đa 1000 ký tự."),
        MaLoiDatSan.ChiTuChoiDonCho => ("", "Chỉ có thể từ chối đơn đang chờ xử lý."),
        MaLoiDatSan.LyDoHuyKhongHopLe => ("", "Lý do hủy là bắt buộc và tối đa 1000 ký tự."),
        MaLoiDatSan.TaiKhoanKhongHopLe => ("", "Tài khoản không hợp lệ."),
        MaLoiDatSan.KhongTonTai => ("", "Không tìm thấy đơn đặt sân."),
        MaLoiDatSan.KhachChiHuyDonCho => ("", "Bạn chỉ có thể hủy đơn đang chờ xử lý."),
        MaLoiDatSan.QuaGioHuy => ("", "Không thể hủy sau khi giờ bắt đầu đã tới."),
        MaLoiDatSan.TrangThaiKhongChoHuy => ("", "Trạng thái hiện tại không cho phép hủy đơn."),
        MaLoiDatSan.DaNhanCoc => ("", "Đơn đã nhận cọc không thể hủy trong phiên bản hiện tại."),
        MaLoiDatSan.ChuaThuDu => ("", "Cần xác nhận đã thu đủ tiền trước khi hoàn thành đơn."),
        MaLoiDatSan.TienAm => ("", "Các khoản tiền không được âm."),
        MaLoiDatSan.ChiHoanThanhDonXacNhan => ("", "Chỉ có thể hoàn thành đơn đã xác nhận."),
        MaLoiDatSan.ChuaDenGioKetThuc => ("", "Chưa đến giờ kết thúc sử dụng sân."),
        MaLoiDatSan.TongTienThapHonCoc => ("", "Tổng tiền không được thấp hơn tiền cọc đã nhận."),
        MaLoiDatSan.MaDonKhongHopLe => ("", "Mã đơn không hợp lệ."),
        MaLoiDatSan.PhienBanKhongHopLe => ("", "Thiếu phiên bản đơn hợp lệ. Hãy tải lại trang."),
        MaLoiDatSan.SanDaThayDoi => ("", "Thông tin sân của đơn đã thay đổi. Hãy tải lại trang."),
        MaLoiDatSan.PhienBanDaCu => ("", "Đơn đã được cập nhật ở nơi khác. Hãy tải lại trang rồi thử lại."),
        MaLoiDatSan.XungDotDuLieu => ("", "Đơn vừa được người khác cập nhật. Hãy tải lại trang."),
        MaLoiDatSan.ChiGhiCocDonXacNhan => ("", "Chỉ ghi nhận cọc cho đơn đã xác nhận."),
        MaLoiDatSan.TienCocKhongHopLe => ("TongCoc", "Tổng cọc phải là số đồng nguyên dương và không vượt giới hạn lưu trữ."),
        MaLoiDatSan.CocKhongTang => ("TongCoc", "Tổng cọc mới phải lớn hơn cọc đã ghi nhận. Chưa hỗ trợ giảm hoặc hoàn cọc."),
        MaLoiDatSan.CocVuotTongTien => ("TongCoc", "Tổng cọc không được vượt tổng tiền hiện tại của đơn."),
        MaLoiDatSan.TienVuotGioiHan => ("", "Tổng tiền vượt giới hạn lưu trữ. Vui lòng kiểm tra đơn và dịch vụ."),
        MaLoiDatSan.NgoaiGioKhiTao when loi.GioMoCua.HasValue && loi.GioDongCua.HasValue
            => ("GioBatDau", $"Sân chỉ mở cửa từ {loi.GioMoCua:HH:mm} đến {loi.GioDongCua:HH:mm}."),
        _ => throw new InvalidOperationException("Mã lỗi đặt sân chưa được ánh xạ hoặc thiếu dữ liệu thông báo.")
    };
}

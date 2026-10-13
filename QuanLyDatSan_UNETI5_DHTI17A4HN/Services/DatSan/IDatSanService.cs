// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: khai báo hợp đồng service dùng chung M3/M4 cho tạo đơn, kiểm tra trùng lịch, khóa sân và chuyển trạng thái.
// Bao gồm đọc danh sách/chi tiết đơn cá nhân và danh sách sân để chọn khi đặt.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public interface IDatSanService
{
    Task<KetQuaTaoDon> TaoDonAsync(TaoDonYeuCau yeuCau, CancellationToken cancellationToken);

    Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken);

    Task KhoaSanAsync(int maSan, CancellationToken cancellationToken);

    Task<KetQuaXuLyDon> XacNhanDonAsync(XuLyDonYeuCau yeuCau, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> TuChoiDonAsync(HuyDonYeuCau yeuCau, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> KhachHangHuyDonAsync(HuyDonYeuCau yeuCau, int maTaiKhoan, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> NhanVienHuyDonAsync(HuyDonYeuCau yeuCau,
        CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> HoanThanhDonAsync(HoanThanhDonYeuCau yeuCau, CancellationToken cancellationToken);
    Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai, int trang, CancellationToken cancellationToken);
    Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SanChonDto>> LayLuaChonSanAsync(CancellationToken cancellationToken);
}

// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: khai báo hợp đồng service dùng chung M2/M3/M4 cho tra cứu khả dụng theo lô, tạo đơn, kiểm tra trùng lịch, khóa sân và chuyển trạng thái.
// Đọc đơn cá nhân/quản lý kèm RowVersion để hủy/xử lý an toàn; lọc, sắp xếp, phân trang và chọn sân.
// Bên gọi xác thực vai trò; thao tác khách hủy lấy mã tài khoản từ phiên đăng nhập, service kiểm tra chủ đơn.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public interface IDatSanService
{
    // M2 tra cứu tối đa 200 mã sân/lần; kết quả không giữ chỗ. Lọc sân khả dụng trước khi phân trang.
    Task<KetQuaKhaDungSan> KiemTraKhaDungAsync(IReadOnlyCollection<int> maSans,
        KhoangGioTraCuuSan khoangGio, CancellationToken cancellationToken);

    Task<KetQuaTaoDon> TaoDonAsync(TaoDonYeuCau yeuCau, CancellationToken cancellationToken);

    Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken);

    Task KhoaSanAsync(int maSan, CancellationToken cancellationToken);

    Task<KetQuaXuLyDon> XacNhanDonAsync(XuLyDonYeuCau yeuCau, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> TuChoiDonAsync(HuyDonYeuCau yeuCau, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> KhachHangHuyDonAsync(HuyDonYeuCau yeuCau, int maTaiKhoan, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> NhanVienHuyDonAsync(HuyDonYeuCau yeuCau,
        CancellationToken cancellationToken);
    // M4-04: chỉ Admin/NhanVien gọi; ghi tổng cọc tăng dần, chốt tiền bằng dữ liệu server/M5.
    Task<KetQuaXuLyDon> GhiCocAsync(GhiCocYeuCau yeuCau, CancellationToken cancellationToken);
    Task<KetQuaXuLyDon> HoanThanhDonAsync(HoanThanhDonYeuCau yeuCau, CancellationToken cancellationToken);
    Task<DanhSachDonDto> LayDanhSachCaNhanAsync(int maTaiKhoan, TrangThaiDatSan? trangThai, int trang, CancellationToken cancellationToken);
    Task<ChiTietDonDto?> LayChiTietCaNhanAsync(int maTaiKhoan, int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SanChonDto>> LayLuaChonSanAsync(CancellationToken cancellationToken);
    // Bên gọi phải kiểm tra quyền Admin/NhanVien trước khi tra cứu đơn quản lý.
    Task<DanhSachDonQuanLyDto> LayDanhSachQuanLyAsync(LocDonQuanLy loc, CancellationToken cancellationToken);
    Task<ChiTietDonQuanLyDto?> LayChiTietQuanLyAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LoaiSanLocDto>> LayLoaiSanLocDonAsync(CancellationToken cancellationToken);
}

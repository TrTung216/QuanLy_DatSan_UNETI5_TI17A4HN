// Phan Giang Tâm - 23103100196. M2: Hợp đồng đọc, ghi sân và tính giá dự kiến.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

// Người gọi: SanTheThaoController. Quyền đọc lấy qua ITaiKhoanHienTai trong service.
// Các thao tác ghi dành cho Admin/NhanVien, được Authorize tại Controller.
public interface ISanTheThaoService
{
    Task<TrangSanDoc> LayDanhSachAsync(int trang, CancellationToken cancellationToken);
    Task<SanDoc?> LayChiTietAsync(int maSan, CancellationToken cancellationToken);
    Task<IReadOnlyList<LoaiSanLuaChon>> LayLuaChonAsync(int? maLoaiSanHienTai, CancellationToken cancellationToken);
    Task<bool> KiemTraLoaiSanAsync(int? maLoaiSan, int? maLoaiSanHienTai, CancellationToken cancellationToken);
    Task<KetQuaSan<int>> TaoAsync(GhiSan yeuCau, CancellationToken cancellationToken);
    Task<KetQuaSan<int>> SuaAsync(int maSan, GhiSan yeuCau, CancellationToken cancellationToken);
    Task<KetQuaSan<int>> XoaAsync(int maSan, CancellationToken cancellationToken);
    Task<KetQuaSan<decimal>> TinhGiaAsync(int maSan, DateOnly ngay,
        TimeOnly batDau, TimeOnly ketThuc, CancellationToken cancellationToken);
}

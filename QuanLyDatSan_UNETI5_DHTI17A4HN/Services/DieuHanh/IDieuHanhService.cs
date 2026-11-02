// Codex: hợp đồng số liệu trang điều hành.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;

public sealed record SoLieuDieuHanh(int SoDonChoXacNhan, int SoKhachHangHoatDong, int SoSanHoatDong);
public interface IDieuHanhService
{
    Task<SoLieuDieuHanh> LaySoLieuAsync(CancellationToken cancellationToken);
}
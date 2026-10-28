// Codex: tách từ QuanTriController của Nguyễn Văn Quý, MSSV 23103100181.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.DieuHanh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;

public sealed class DieuHanhService(IDieuHanhRepository repository, IDongHo dongHo) : IDieuHanhService
{
    public Task<SoLieuDieuHanh> LaySoLieuAsync(CancellationToken cancellationToken) =>
        repository.LaySoLieuAsync(dongHo.BayGio, cancellationToken);
}
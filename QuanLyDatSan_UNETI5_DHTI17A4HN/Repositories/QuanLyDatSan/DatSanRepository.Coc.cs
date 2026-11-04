// M4: Phạm Thành Nghĩa; MSSV: 23103100308. Đọc và chuẩn bị lưu giao dịch, biên nhận cọc; không tự commit.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;

public partial class DatSanRepository
{
    public Task<TaiKhoan?> LayTaiKhoanAsync(int id, CancellationToken ct) =>
        db.TaiKhoans.AsNoTracking().SingleOrDefaultAsync(x => x.MaTaiKhoan == id, ct);
    public Task<DatSan?> DocDonCocAsync(int id, CancellationToken ct) =>
        db.DatSans.AsNoTracking().Include(x => x.KhachHang).SingleOrDefaultAsync(x => x.MaDatSan == id, ct);
    public async Task<GiaoDichCoc?> LayGiaoDichAsync(int id, CancellationToken ct)
    {
        var cu = db.Set<GiaoDichCoc>().Local.SingleOrDefault(x => x.MaGiaoDich == id);
        if (cu is not null) db.Entry(cu).State = EntityState.Detached;
        return await db.Set<GiaoDichCoc>().SingleOrDefaultAsync(x => x.MaGiaoDich == id, ct);
    }
    public Task<List<GiaoDichCoc>> LayGiaoDichCuaDonAsync(string noiDung, CancellationToken ct) =>
        db.Set<GiaoDichCoc>().AsNoTracking().Where(x => x.NoiDung == noiDung).OrderBy(x => x.MaGiaoDich).ToListAsync(ct);
    public Task<BienNhanCoc?> LayBienNhanAsync(int maDon, CancellationToken ct) =>
        db.Set<BienNhanCoc>().AsNoTracking().SingleOrDefaultAsync(x => x.MaDatSan == maDon, ct);
    public Task<BienNhanCoc?> LayBienNhanGiaoDichAsync(int maGiaoDich, CancellationToken ct) =>
        db.Set<BienNhanCoc>().AsNoTracking().SingleOrDefaultAsync(x => x.MaGiaoDich == maGiaoDich, ct);
    public void ThemGiaoDich(GiaoDichCoc giaoDich) => db.Set<GiaoDichCoc>().Add(giaoDich);
    public void ThemBienNhan(BienNhanCoc bienNhan) => db.Set<BienNhanCoc>().Add(bienNhan);
}

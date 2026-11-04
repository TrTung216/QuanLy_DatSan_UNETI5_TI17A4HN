// M1: Trần Trọng Tùng - 23103100202. Không đọc/ghi file trong repository.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Anh;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Anh;

public sealed class AnhRepository(ApplicationDbContext db) : IAnhRepository
{
    public Task<ThongTinAnh?> LayAsync(NhomAnh nhom, int id, CancellationToken ct) => nhom switch
    {
        NhomAnh.LoaiSan => db.LoaiSans.AsNoTracking().Where(x => x.MaLoaiSan == id)
            .Select(x => new ThongTinAnh(x.DuongDanAnh, x.TrangThai == TrangThaiLoaiSan.HoatDong))
            .SingleOrDefaultAsync(ct),
        NhomAnh.San => db.SanTheThaos.AsNoTracking().Where(x => x.MaSan == id)
            .Select(x => new ThongTinAnh(x.DuongDanAnh, x.TrangThai == TrangThaiSan.HoatDong
                && x.LoaiSan.TrangThai == TrangThaiLoaiSan.HoatDong)).SingleOrDefaultAsync(ct),
        _ => throw new ArgumentOutOfRangeException(nameof(nhom))
    };
    public async Task<bool> CoThamChieuAsync(string khoa, CancellationToken ct) =>
        await db.LoaiSans.AnyAsync(x => x.DuongDanAnh == khoa, ct)
        || await db.SanTheThaos.AnyAsync(x => x.DuongDanAnh == khoa, ct);
}

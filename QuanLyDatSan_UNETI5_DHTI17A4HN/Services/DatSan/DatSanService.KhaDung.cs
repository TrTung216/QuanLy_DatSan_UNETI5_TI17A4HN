// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Kiểm tra giờ, trạng thái sân, bảo trì và trùng lịch; kết quả tra cứu không giữ chỗ.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    public async Task<KetQuaKhaDungSan> KiemTraKhaDungAsync(IReadOnlyCollection<int> maSans,
        KhoangGioTraCuuSan khoangGio, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(maSans);
        ArgumentNullException.ThrowIfNull(khoangGio);
        if (maSans.Count > 200 || maSans.Any(x => x <= 0))
            throw new ArgumentException("Mỗi lần tra cứu nhận tối đa 200 mã sân dương.", nameof(maSans));
        cancellationToken.ThrowIfCancellationRequested();
        var (batDau, ketThuc) = ChuanHoaKhoangGio(khoangGio);
        var loi = KiemTraKhoangGio(batDau, ketThuc, dongHo.BayGio);
        if (loi is not null)
            return new(batDau, ketThuc, loi, Array.Empty<KhaDungSanDto>());

        var ids = maSans.Distinct().ToArray();
        if (ids.Length == 0)
            return new(batDau, ketThuc, null, Array.Empty<KhaDungSanDto>());

        var cacSan = await repository.LayKhaDungTheoLoAsync(ids, batDau, ketThuc, cancellationToken);
        var ketQua = ids.Select(id =>
        {
            cacSan.TryGetValue(id, out var san);
            return new KhaDungSanDto(id, DanhGiaKhaDungSan(san, batDau, ketThuc),
                san?.GioMoCua, san?.GioDongCua);
        }).ToArray();
        return new(batDau, ketThuc, null, ketQua);
    }

    private static (DateTime BatDau, DateTime KetThuc) ChuanHoaKhoangGio(KhoangGioTraCuuSan gio) =>
        (gio.NgaySuDung.ToDateTime(new TimeOnly(gio.GioBatDau.Hour, gio.GioBatDau.Minute)),
         gio.NgaySuDung.ToDateTime(new TimeOnly(gio.GioKetThuc.Hour, gio.GioKetThuc.Minute)));

    private static MaLoiKhaDungSan? KiemTraKhoangGio(DateTime batDau, DateTime ketThuc, DateTime bayGio)
    {
        if (batDau <= bayGio) return MaLoiKhaDungSan.BatDauKhongTuongLai;
        if (ketThuc <= batDau || ketThuc.Date != batDau.Date)
            return MaLoiKhaDungSan.KhoangGioKhongHopLe;
        return null;
    }

    private static MaLoiKhaDungSan? DanhGiaKhaDungSan(
        DuLieuKhaDungSan? san, DateTime batDau, DateTime ketThuc)
    {
        if (san is null) return MaLoiKhaDungSan.SanKhongTonTai;
        if (!san.HoatDong) return MaLoiKhaDungSan.SanKhongNhanDat;
        if (TimeOnly.FromDateTime(batDau) < san.GioMoCua
            || TimeOnly.FromDateTime(ketThuc) > san.GioDongCua)
            return MaLoiKhaDungSan.NgoaiGioHoatDong;
        if (san.NgayBaoTri == DateOnly.FromDateTime(batDau)) return MaLoiKhaDungSan.SanBaoTri;
        if (san.TrungLich) return MaLoiKhaDungSan.TrungLich;
        return null;
    }

    private static MaLoiDatSan AnhXaLoiKhaDung(MaLoiKhaDungSan loi, bool khiTao)
    {
        switch (loi)
        {
            case MaLoiKhaDungSan.BatDauKhongTuongLai:
                if (khiTao) return MaLoiDatSan.BatDauKhongTuongLai;
                return MaLoiDatSan.QuaGioXacNhan;
            case MaLoiKhaDungSan.KhoangGioKhongHopLe:
                return MaLoiDatSan.KetThucKhongHopLe;
            case MaLoiKhaDungSan.SanKhongTonTai:
                return MaLoiDatSan.SanKhongTonTai;
            case MaLoiKhaDungSan.SanKhongNhanDat:
                if (khiTao) return MaLoiDatSan.SanKhongNhanDat;
                return MaLoiDatSan.SanKhongHoatDong;
            case MaLoiKhaDungSan.NgoaiGioHoatDong:
                if (khiTao) return MaLoiDatSan.NgoaiGioKhiTao;
                return MaLoiDatSan.NgoaiGioHoatDong;
            case MaLoiKhaDungSan.SanBaoTri:
                if (khiTao) return MaLoiDatSan.SanBaoTriKhiTao;
                return MaLoiDatSan.SanBaoTri;
            case MaLoiKhaDungSan.TrungLich:
                if (khiTao) return MaLoiDatSan.TrungLichKhiTao;
                return MaLoiDatSan.TrungLich;
            default:
                throw new ArgumentOutOfRangeException(nameof(loi));
        }
    }

}

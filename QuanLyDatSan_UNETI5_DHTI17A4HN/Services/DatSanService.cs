// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Tạo đơn: kiểm tra khách, khóa sân, kiểm tra khả dụng và lưu giá tại thời điểm đặt.

using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDatSan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTinhTien;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService(IDatSanRepository repository, IDonViCongViec donViCongViec, IDongHo dongHo, ITinhTienService tinhTien) : IDatSanService
{
    private Task<bool> KhachDuDieuKienAsync(int maTaiKhoan, CancellationToken cancellationToken) =>
        repository.KhachDuDieuKienAsync(maTaiKhoan, cancellationToken);

    public async Task<KetQuaTaoDon> TaoDonAsync(TaoDonYeuCau yeuCau, CancellationToken cancellationToken)
    {
        var (maTaiKhoan, maSan, ngaySuDung, gioBatDau, gioKetThuc) = yeuCau;
        if (!await KhachDuDieuKienAsync(maTaiKhoan, cancellationToken))
            return Loi(MaLoiDatSan.HoSoKhongHoatDong);

        var (batDau, ketThuc) = ChuanHoaKhoangGio(new(ngaySuDung, gioBatDau, gioKetThuc));
        var loiGio = KiemTraKhoangGio(batDau, ketThuc, dongHo.BayGio);
        if (loiGio is not null) return Loi(AnhXaLoiKhaDung(loiGio.Value, khiTao: true));

        DatSan? donMoi = null;
        var ketQua = await donViCongViec.TrongGiaoDichAsync<KetQuaTaoDon?>(async token =>
        {
            await KhoaSanAsync(maSan, cancellationToken);

            var khachHang = await repository.LayKhachTaoDonAsync(maTaiKhoan, token);
            if (khachHang is null || khachHang.TrangThai != TrangThaiKhachHang.HoatDong
                || khachHang.TaiKhoan.TrangThai != TrangThaiTaiKhoan.HoatDong
                || khachHang.TaiKhoan.VaiTro != VaiTro.KhachHang)
                return Loi(MaLoiDatSan.HoSoKhongHoatDong);

            var bayGio = dongHo.BayGio;
            loiGio = KiemTraKhoangGio(batDau, ketThuc, bayGio);
            if (loiGio is not null) return Loi(AnhXaLoiKhaDung(loiGio.Value, khiTao: true));

            var san = await repository.LayKhaDungAsync(maSan, batDau, ketThuc, null, token);
            var loiSan = DanhGiaKhaDungSan(san, batDau, ketThuc);
            if (loiSan is not null)
                return KetQuaTaoDon.ThatBai(new(AnhXaLoiKhaDung(loiSan.Value, khiTao: true),
                    san?.GioMoCua, san?.GioDongCua));

            var don = new DatSan
            {
                MaKhachHang = khachHang.MaKhachHang,
                MaSan = san!.MaSan,
                NgayDat = bayGio,
                GioBatDau = batDau,
                GioKetThuc = ketThuc,
                DonGia = san.DonGia,
                TienCoc = 0m,
                TrangThai = TrangThaiDatSan.ChoXuLy
            };
            repository.Them(don);
            donMoi = don;
            return null;
        }, x => x is null, System.Data.IsolationLevel.ReadCommitted, cancellationToken);
        return ketQua ?? KetQuaTaoDon.DaTao(donMoi!.MaDatSan);
    }

    public Task<bool> CoTrungLichAsync(int maSan, DateTime batDau, DateTime ketThuc, int? boQuaMaDatSan,
        CancellationToken cancellationToken) => repository.CoTrungLichAsync(maSan, batDau, ketThuc, boQuaMaDatSan, cancellationToken);

    public Task KhoaSanAsync(int maSan, CancellationToken cancellationToken) => repository.KhoaSanAsync(maSan, cancellationToken);

    private static KetQuaTaoDon Loi(MaLoiDatSan ma) => KetQuaTaoDon.ThatBai(new(ma));
}

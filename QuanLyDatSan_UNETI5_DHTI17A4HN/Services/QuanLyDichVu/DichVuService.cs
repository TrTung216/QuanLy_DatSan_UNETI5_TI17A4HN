// Codex: tách truy cập dữ liệu sang Repository, giữ nghiệp vụ và tác giả gốc.
// M5: Nguyễn Hữu Quang; MSSV: 23103100184

using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyDichVu;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyDichVu;

public class DichVuService(IDichVuRepository repository, IDonViCongViec donViCongViec) : IDichVuService
{

    public Task<DanhSachDichVu> LayDanhSachAsync(string? tuKhoa, TrangThaiDichVu? trangThai, string? sapXep,
        int trang, CancellationToken cancellationToken) => repository.LayDanhSachAsync(tuKhoa?.Trim(), trangThai, sapXep, trang, cancellationToken);
    public Task<DichVuDoc?> LayChiTietAsync(int maDichVu, CancellationToken cancellationToken) =>
        repository.LayChiTietAsync(maDichVu, cancellationToken);
    public async Task<KetQuaNghiepVu> TaoAsync(string tenDichVu, string donViTinh, decimal donGia, string? moTa,
        CancellationToken cancellationToken)
    {
        var loiDonGia = KiemTraDuLieu(tenDichVu, donViTinh, donGia, moTa);
        if (loiDonGia is not null) return loiDonGia;

        repository.Them(new DichVu
        {
            TenDichVu = tenDichVu,
            DonViTinh = donViTinh,
            DonGia = donGia,
            MoTa = ChuanHoaMoTa(moTa),
            TrangThai = TrangThaiDichVu.HoatDong // do server quyết định, không nhận từ form
        });
        await donViCongViec.LuuAsync(cancellationToken);

        return KetQuaNghiepVu.ThanhCongVoi("Đã thêm dịch vụ.");
    }

    public async Task<KetQuaNghiepVu> SuaAsync(int maDichVu, string tenDichVu, string donViTinh, decimal donGia,
        string? moTa, CancellationToken cancellationToken)
    {
        var loiDonGia = KiemTraDuLieu(tenDichVu, donViTinh, donGia, moTa);
        if (loiDonGia is not null) return loiDonGia;

        var dichVu = await repository.LayDeCapNhatAsync(maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.KhongThay();

        // Chỉ ảnh hưởng đơn mới: ChiTietDatSan sẽ lưu bản chụp DonGia lúc thêm.
        dichVu.TenDichVu = tenDichVu;
        dichVu.DonViTinh = donViTinh;
        dichVu.DonGia = donGia;
        dichVu.MoTa = ChuanHoaMoTa(moTa);
        await donViCongViec.LuuAsync(cancellationToken);

        return KetQuaNghiepVu.ThanhCongVoi("Đã cập nhật dịch vụ.");
    }

    // Không xóa cứng: chỉ chuyển HoatDong <-> NgungHoatDong.
    public async Task<KetQuaNghiepVu> DoiTrangThaiAsync(int maDichVu, CancellationToken cancellationToken)
    {
        var dichVu = await repository.LayDeCapNhatAsync(maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.KhongThay();

        dichVu.TrangThai = dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? TrangThaiDichVu.NgungHoatDong
            : TrangThaiDichVu.HoatDong;
        await donViCongViec.LuuAsync(cancellationToken);

        return KetQuaNghiepVu.ThanhCongVoi(dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? "Đã kích hoạt dịch vụ." : "Đã ngừng hoạt động dịch vụ.");
    }

    // Quy ước: giá nhập theo đồng nguyên.
    private static KetQuaNghiepVu? KiemTraDuLieu(string tenDichVu, string donViTinh, decimal donGia, string? moTa)
    {
        if (string.IsNullOrWhiteSpace(tenDichVu)) return KetQuaNghiepVu.BaoLoi("Vui lòng nhập tên dịch vụ.", nameof(DichVu.TenDichVu));
        if (tenDichVu.Length > 100) return KetQuaNghiepVu.BaoLoi("Tên dịch vụ tối đa 100 ký tự.", nameof(DichVu.TenDichVu));
        if (string.IsNullOrWhiteSpace(donViTinh)) return KetQuaNghiepVu.BaoLoi("Vui lòng nhập đơn vị tính.", nameof(DichVu.DonViTinh));
        if (donViTinh.Length > 30) return KetQuaNghiepVu.BaoLoi("Đơn vị tính tối đa 30 ký tự.", nameof(DichVu.DonViTinh));
        if (donGia is < 0 or > 1000000000) return KetQuaNghiepVu.BaoLoi("Đơn giá phải từ 0 đến 1.000.000.000.", nameof(DichVu.DonGia));
        if (moTa?.Length > 1000) return KetQuaNghiepVu.BaoLoi("Mô tả tối đa 1000 ký tự.", nameof(DichVu.MoTa));
        return KiemTraDonGia(donGia);
    }

    private static KetQuaNghiepVu? KiemTraDonGia(decimal donGia)
        => donGia != decimal.Truncate(donGia)
            ? KetQuaNghiepVu.BaoLoi("Đơn giá phải là số đồng nguyên.", nameof(DichVu.DonGia))
            : null;

    private static string? ChuanHoaMoTa(string? moTa) => string.IsNullOrWhiteSpace(moTa) ? null : moTa;
}

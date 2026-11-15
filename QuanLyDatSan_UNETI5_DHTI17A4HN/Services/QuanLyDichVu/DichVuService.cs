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
        var loiTen = await KiemTraTenTrungAsync(tenDichVu, null, cancellationToken);
        if (loiTen is not null) return loiTen;

        repository.Them(new DichVu
        {
            TenDichVu = tenDichVu.Trim(),
            DonViTinh = donViTinh,
            DonGia = donGia, 
            MoTa = ChuanHoaMoTa(moTa),
            TrangThai = TrangThaiDichVu.HoatDong // do server quyết định, không nhận từ form
        });
        try
        {
            await donViCongViec.LuuAsync(cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.TrungTenDichVu)
        {
            return await KetQuaTenTrungAsync(tenDichVu, null, cancellationToken);
        }

        return KetQuaNghiepVu.ThanhCongVoi("Đã thêm dịch vụ.");
    }

    public async Task<KetQuaNghiepVu> SuaAsync(int maDichVu, string tenDichVu, string donViTinh, decimal donGia,
        string? moTa, CancellationToken cancellationToken)
    {
        var loiDonGia = KiemTraDuLieu(tenDichVu, donViTinh, donGia, moTa);
        if (loiDonGia is not null) return loiDonGia;

        var dichVu = await repository.LayDeCapNhatAsync(maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.KhongThay();
        var loiTen = await KiemTraTenTrungAsync(tenDichVu, maDichVu, cancellationToken);
        if (loiTen is not null) return loiTen;

        // Chỉ ảnh hưởng đơn mới: ChiTietDatSan sẽ lưu bản chụp DonGia lúc thêm.
        dichVu.TenDichVu = tenDichVu.Trim();
        dichVu.DonViTinh = donViTinh;
        dichVu.DonGia = donGia;
        dichVu.MoTa = ChuanHoaMoTa(moTa);
        try
        {
            await donViCongViec.LuuAsync(cancellationToken);
        }
        catch (LoiLuuTruException ex) when (ex.MaLoi == MaLoiLuuTru.TrungTenDichVu)
        {
            return await KetQuaTenTrungAsync(tenDichVu, maDichVu, cancellationToken);
        }

        return KetQuaNghiepVu.ThanhCongVoi("Đã cập nhật dịch vụ.");
    }

    private async Task<KetQuaNghiepVu?> KiemTraTenTrungAsync(string tenDichVu, int? maDichVuNgoaiTru,
        CancellationToken cancellationToken)
    {
        var trangThai = await repository.LayTrangThaiTenTrungAsync(tenDichVu.Trim(), maDichVuNgoaiTru, cancellationToken);
        return trangThai switch
        {
            null => null,
            TrangThaiDichVu.HoatDong => KetQuaNghiepVu.BaoLoi("Tên dịch vụ đã được sử dụng.", nameof(DichVu.TenDichVu)),
            TrangThaiDichVu.NgungHoatDong => KetQuaNghiepVu.BaoLoi(
                "Tên dịch vụ đã tồn tại nhưng đang ngừng hoạt động.", nameof(DichVu.TenDichVu)),
            _ => throw new InvalidOperationException("Trạng thái dịch vụ trùng tên chưa được hỗ trợ.")
        };
    }

    private async Task<KetQuaNghiepVu> KetQuaTenTrungAsync(string tenDichVu, int? maDichVuNgoaiTru,
        CancellationToken cancellationToken) =>
        await KiemTraTenTrungAsync(tenDichVu, maDichVuNgoaiTru, cancellationToken)
        ?? KetQuaNghiepVu.BaoLoi("Tên dịch vụ vừa được sử dụng. Vui lòng kiểm tra lại.", nameof(DichVu.TenDichVu));

    // Không xóa cứng: chỉ chuyển HoatDong <-> NgungHoatDong.
    public async Task<KetQuaNghiepVu> DoiTrangThaiAsync(int maDichVu, CancellationToken cancellationToken)
    {
        var dichVu = await repository.LayDeCapNhatAsync(maDichVu, cancellationToken);
        if (dichVu is null) return KetQuaNghiepVu.KhongThay();

        dichVu.TrangThai = dichVu.TrangThai == TrangThaiDichVu.HoatDong
            ? TrangThaiDichVu.NgungHoatDong
            : TrangThaiDichVu.HoatDong;
        await donViCongViec.LuuAsync(cancellationToken);

        string thongBao;
        if (dichVu.TrangThai == TrangThaiDichVu.HoatDong)
        {
            thongBao = "Đã kích hoạt dịch vụ.";
        }
        else
        {
            thongBao = "Đã ngừng hoạt động dịch vụ.";
        }

        return KetQuaNghiepVu.ThanhCongVoi(thongBao);
    }

    // Quy ước: giá nhập theo đồng nguyên.
    private static KetQuaNghiepVu? KiemTraDuLieu(string tenDichVu, string donViTinh, decimal donGia, string? moTa)
    {
        if (string.IsNullOrWhiteSpace(tenDichVu)) return KetQuaNghiepVu.BaoLoi("Vui lòng nhập tên dịch vụ.", nameof(DichVu.TenDichVu));
        if (tenDichVu.Length > 100) return KetQuaNghiepVu.BaoLoi("Tên dịch vụ tối đa 100 ký tự.", nameof(DichVu.TenDichVu));
        if (string.IsNullOrWhiteSpace(donViTinh)) return KetQuaNghiepVu.BaoLoi("Vui lòng nhập đơn vị tính.", nameof(DichVu.DonViTinh));
        if (donViTinh.Length > 30) return KetQuaNghiepVu.BaoLoi("Đơn vị tính tối đa 30 ký tự.", nameof(DichVu.DonViTinh));
        if (donGia is <= 0 or > 1000000000) return KetQuaNghiepVu.BaoLoi("Đơn giá phải lớn hơn 0 và tối đa 1.000.000.000.", nameof(DichVu.DonGia));
        if (moTa?.Length > 1000) return KetQuaNghiepVu.BaoLoi("Mô tả tối đa 1000 ký tự.", nameof(DichVu.MoTa));
        return KiemTraDonGia(donGia);
    }

    private static KetQuaNghiepVu? KiemTraDonGia(decimal donGia)
    {
        if (donGia != decimal.Truncate(donGia))
        {
            return KetQuaNghiepVu.BaoLoi("Đơn giá phải là số đồng nguyên.", nameof(DichVu.DonGia));
        }

        return null;
    }

    private static string? ChuanHoaMoTa(string? moTa)
    {
        if (string.IsNullOrWhiteSpace(moTa))
        {
            return null;
        }

        return moTa;
    }
}

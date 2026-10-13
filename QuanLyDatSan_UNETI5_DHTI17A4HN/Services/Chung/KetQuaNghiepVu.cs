namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.Chung;

public record KetQuaNghiepVu(
    bool ThanhCong,
    string? ThongBao = null,
    string? Loi = null,
    string? TruongLoi = null,
    bool KhongTimThay = false)
{
    public static KetQuaNghiepVu ThanhCongVoi(string thongBao) => new(true, ThongBao: thongBao);

    public static KetQuaNghiepVu BaoLoi(string loi, string? truongLoi = null) => new(false, Loi: loi, TruongLoi: truongLoi);

    public static KetQuaNghiepVu KhongThay() => new(false, KhongTimThay: true);
}
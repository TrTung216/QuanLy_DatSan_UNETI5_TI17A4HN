// Phan Giang Tâm - 23103100196. M2: Kết quả thao tác sân và mã lỗi nghiệp vụ.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

public enum MaLoiSan
{
    ThanhCong,
    KhongTonTai,
    LoaiSanKhongHopLe,
    DuLieuKhongHopLe,
    DuLieuLienQuanThayDoi,
    DangDuocThamChieu,
    DaBiXoa,
    GioThueKhongHopLe
}

// Chỉ tạo qua factory: thành công có dữ liệu; thất bại có một mã lỗi hợp lệ.
public sealed class KetQuaSan<T> where T : notnull
{
    private readonly T? duLieu;
    public MaLoiSan MaLoi { get; }
    public bool ThanhCong => MaLoi == MaLoiSan.ThanhCong;
    public T DuLieu => ThanhCong ? duLieu! : throw new InvalidOperationException("Kết quả thất bại không có dữ liệu thành công.");

    private KetQuaSan(MaLoiSan maLoi, T? duLieu)
    {
        MaLoi = maLoi;
        this.duLieu = duLieu;
    }

    public static KetQuaSan<T> Dat(T duLieu)
    {
        ArgumentNullException.ThrowIfNull(duLieu);
        return new(MaLoiSan.ThanhCong, duLieu);
    }

    public static KetQuaSan<T> Loi(MaLoiSan maLoi)
    {
        if (!Enum.IsDefined(maLoi) || maLoi == MaLoiSan.ThanhCong)
            throw new ArgumentOutOfRangeException(nameof(maLoi), "Thất bại phải có mã lỗi hợp lệ.");
        return new(maLoi, default);
    }
}

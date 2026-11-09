// M3: Nguyễn Văn Quý; MSSV: 23103100181. Quy tắc cọc tối thiểu dùng chung cho service và màn hình xác nhận đơn.
// Theo docs/Quy_uoc_dat_coc_chuyen_khoan.md: cọc = 50% tiền sân dự kiến, làm tròn đến đồng, chưa tính dịch vụ.
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public static class ChinhSachCocToiThieu
{
    public const decimal TyLe = 0.5m;

    public static decimal Tinh(decimal tienSan) => Math.Round(tienSan * TyLe, 0, MidpointRounding.AwayFromZero);
}

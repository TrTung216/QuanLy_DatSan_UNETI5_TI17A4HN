// M3: Nguyễn Văn Quý; MSSV: 23103100181. định dạng tiền, tên và màu trạng thái đơn theo mục 7 quy ước nhóm.
using System.Globalization;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public static class HienThiDatSan
{
    private static readonly CultureInfo ViVn = CultureInfo.GetCultureInfo("vi-VN");

    public static string Tien(decimal tien) => string.Format(ViVn, "{0:N0} ₫", tien);

    public static string TenTrangThai(TrangThaiDatSan trangThai) => trangThai switch
    {
        TrangThaiDatSan.ChoXuLy => "Chờ xác nhận",
        TrangThaiDatSan.DangXuLy => "Đã xác nhận",
        TrangThaiDatSan.HoanThanh => "Hoàn thành",
        TrangThaiDatSan.DaHuy => "Đã hủy",
        TrangThaiDatSan.TuChoi => "Từ chối",
        _ => string.Empty
    };

    public static string LopMau(TrangThaiDatSan trangThai) => trangThai switch
    {
        TrangThaiDatSan.ChoXuLy => "ql-status ql-status--waiting",
        TrangThaiDatSan.DangXuLy => "ql-status ql-status--confirmed",
        TrangThaiDatSan.HoanThanh => "ql-status ql-status--completed",
        TrangThaiDatSan.DaHuy => "ql-status ql-status--cancelled",
        TrangThaiDatSan.TuChoi => "ql-status ql-status--rejected",
        _ => "ql-status"
    };
}

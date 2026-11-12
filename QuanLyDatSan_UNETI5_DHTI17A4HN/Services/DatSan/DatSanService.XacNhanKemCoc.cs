// M3: Nguyễn Văn Quý; MSSV: 23103100181. Xác nhận đơn kèm ghi nhận cọc "đã nhận đủ" trong cùng một giao dịch.
// Dùng lại XuLyDonCoKhoaSanAsync của M4 (khóa sân, kiểm RowVersion, kiểm quyền quản lý); không thêm transaction riêng.
// M1: Trần Trọng Tùng; MSSV: 23103100202. Dùng công thức cọc từ ITinhTienService.
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Services;

public partial class DatSanService
{
    public Task<KetQuaXuLyDon> XacNhanDonKemCocAsync(XuLyDonYeuCau yeuCau, CancellationToken cancellationToken) =>
        XuLyDonCoKhoaSanAsync(yeuCau.MaDatSan, yeuCau.PhienBan, async (don, token) =>
        {
            // Đơn theo chính sách cọc mới (đối soát giao dịch) vẫn đi qua luồng M4, không ghi cọc tay ở đây.
            if (don.PhienBanChinhSachCoc != 0) return MaLoiDatSan.YeuCauDoiSoatCoc;
            var loi = await KiemTraXacNhanAsync(don, token);
            if (loi.HasValue) return loi;

            var tien = await TinhTienDonAsync(don, token);
            if (tien.Tong > TienToiDa) return MaLoiDatSan.TienVuotGioiHan;
            var tienCoc = tinhTien.TinhTienCocYeuCau(tien.San);
            if (tienCoc <= 0m) return MaLoiDatSan.TienCocKhongHopLe;
            if (tienCoc > tien.Tong) return MaLoiDatSan.CocVuotTongTien;

            don.TrangThai = TrangThaiDatSan.DangXuLy;
            don.NgayXacNhan = dongHo.BayGio;
            don.TienCoc = Math.Max(don.TienCoc, tienCoc);
            return null;
        }, cancellationToken);
}

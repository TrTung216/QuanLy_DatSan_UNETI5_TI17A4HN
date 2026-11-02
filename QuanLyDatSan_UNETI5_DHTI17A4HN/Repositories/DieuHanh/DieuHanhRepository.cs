// Codex: tách truy vấn từ service DieuHanh, giữ nguyên bộ lọc và tracking.
using Microsoft.EntityFrameworkCore;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.DieuHanh;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.DieuHanh;

public class DieuHanhRepository(ApplicationDbContext db) : IDieuHanhRepository
{
    // Ba truy vấn đếm chạy tuần tự trên cùng DbContext; không tải hay track entity.
    // Đơn chờ được tính khi chưa hết giờ kết thúc, kể cả đã đến giờ bắt đầu.
    // Khách hoạt động xét trạng thái hồ sơ; sân hoạt động xét trạng thái sân,
    // không đồng nghĩa tài khoản khách đang mở hoặc sân còn trống ở một khung giờ cụ thể.
    public async Task<SoLieuDieuHanh> LaySoLieuAsync(DateTime bayGio, CancellationToken cancellationToken) => new(
        await db.DatSans.CountAsync(x => x.TrangThai == TrangThaiDatSan.ChoXuLy && x.GioKetThuc > bayGio, cancellationToken),
        await db.KhachHangs.CountAsync(x => x.TrangThai == TrangThaiKhachHang.HoatDong, cancellationToken),
        await db.SanTheThaos.CountAsync(x => x.TrangThai == TrangThaiSan.HoatDong, cancellationToken));
}

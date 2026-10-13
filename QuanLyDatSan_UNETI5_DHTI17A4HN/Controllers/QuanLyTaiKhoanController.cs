// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ Controller tài khoản gọi service và ánh xạ phản hồi.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyTaiKhoan;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.Admin))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class QuanLyTaiKhoanController(IQuanLyTaiKhoanService service, ITaiKhoanHienTai hienTai) : Controller
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? tuKhoa, VaiTro? vaiTro,
        TrangThaiTaiKhoan? trangThai, int trang = 1, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || (vaiTro.HasValue && !Enum.IsDefined(vaiTro.Value))
            || (trangThai.HasValue && !Enum.IsDefined(trangThai.Value))) return BadRequest();
        tuKhoa = tuKhoa?.Trim();
        if (tuKhoa?.Length > 100) return BadRequest();
        var ketQua = await service.LayDanhSachAsync(tuKhoa, vaiTro, trangThai, trang, cancellationToken);
        return View(new TaiKhoanDanhSachViewModel
        {
            DanhSach = ketQua.DanhSach.Select(x => new TaiKhoanDongViewModel
            {
                MaTaiKhoan = x.MaTaiKhoan, TenDangNhap = x.TenDangNhap, HoTen = x.HoTen,
                Email = x.Email, VaiTro = x.VaiTro, TrangThai = x.TrangThai
            }).ToList(),
            TuKhoa = tuKhoa, VaiTro = vaiTro, TrangThai = trangThai,
            Trang = ketQua.Trang, TongTrang = ketQua.TongTrang, TongSo = ketQua.TongSo
        });
    }

    [HttpGet]
    public IActionResult Tao() => View(new TaiKhoanTaoViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaiKhoanTaoViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var ketQua = await service.TaoAsync(new TaoTaiKhoan
        {
            TenDangNhap = model.TenDangNhap, MatKhau = model.MatKhau,
            HoTen = model.HoTen, Email = model.Email, VaiTro = model.VaiTro!.Value
        }, cancellationToken);
        return PhanHoi(ketQua, model, "Đã tạo tài khoản.");
    }

    [HttpGet]
    public async Task<IActionResult> Sua(int id, CancellationToken cancellationToken)
    {
        var taiKhoan = await service.LayChiTietAsync(id, cancellationToken);
        if (taiKhoan is null) return NotFound();
        NapThongTinForm(taiKhoan);
        return View(new TaiKhoanSuaViewModel
        {
            HoTen = taiKhoan.HoTen, Email = taiKhoan.Email, VaiTro = taiKhoan.VaiTro, TrangThai = taiKhoan.TrangThai
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(int id, TaiKhoanSuaViewModel model, CancellationToken cancellationToken)
    {
        // Luôn nạp tên từ server, kể cả khi form lỗi; không nhận tên/mật khẩu trong thao tác sửa.
        var taiKhoan = await service.LayChiTietAsync(id, cancellationToken);
        if (taiKhoan is null) return NotFound();
        NapThongTinForm(taiKhoan);
        if (!ModelState.IsValid) return View(model);
        var ketQua = await service.SuaAsync(id, new SuaTaiKhoan(
            model.HoTen, model.Email, model.VaiTro!.Value, model.TrangThai!.Value), cancellationToken);
        return PhanHoi(ketQua, model, "Đã cập nhật tài khoản.");
    }

    private void NapThongTinForm(TaiKhoanDoc taiKhoan)
    {
        ViewData["MaTaiKhoan"] = taiKhoan.MaTaiKhoan;
        ViewData["TenDangNhap"] = taiKhoan.TenDangNhap;
        ViewData["LaTaiKhoanHienTai"] = taiKhoan.MaTaiKhoan == hienTai.MaTaiKhoan;
    }

    private IActionResult PhanHoi(KetQuaQuanLyTaiKhoan ketQua, object model, string thongBao)
    {
        if (ketQua.Loi.Contains(MaLoiTaiKhoan.KhongDuQuyen)) return Forbid();
        if (ketQua.Loi.Contains(MaLoiTaiKhoan.KhongTonTai)) return NotFound();
        if (ketQua.ThanhCong)
        {
            TempData["Success"] = thongBao;
            return RedirectToAction(nameof(DanhSach));
        }
        foreach (var loi in ketQua.Loi)
        {
            var (truong, noiDung) = loi switch
            {
                MaLoiTaiKhoan.TrungTen => (nameof(TaiKhoanTaoViewModel.TenDangNhap), "Tên đăng nhập đã được sử dụng."),
                MaLoiTaiKhoan.AdminCuoiCung => (string.Empty, "Không thể khóa hoặc hạ quyền Admin hoạt động cuối cùng."),
                MaLoiTaiKhoan.TuKhoaHoacHaQuyen => (string.Empty, "Bạn không thể tự khóa hoặc hạ quyền tài khoản đang đăng nhập."),
                MaLoiTaiKhoan.XungDotDuLieu => (string.Empty, "Có cập nhật đồng thời. Vui lòng tải lại trang và thử lại."),
                MaLoiTaiKhoan.DuLieuKhongHopLe => (string.Empty, "Thông tin tài khoản không hợp lệ."),
                _ => throw new InvalidOperationException("Mã lỗi tài khoản chưa được ánh xạ.")
            };
            ModelState.AddModelError(truong, noiDung);
        }
        return View(model);
    }
}

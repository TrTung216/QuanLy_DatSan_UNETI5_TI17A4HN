// Phan Giang Tâm - 23103100196. M2: Nhận form, gọi service sân và ánh xạ phản hồi.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLySan;

namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SanTheThaoController(ISanTheThaoService sanService) : Controller
{
    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ThemSanViewModel
        {
            TrangThai = TrangThaiSan.HoatDong,
            GioMoCua = new TimeOnly(6, 0),
            GioDongCua = new TimeOnly(22, 0)
        };
        await NapLoaiSan(model, cancellationToken);
        return View(model);
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThemSanViewModel model, CancellationToken cancellationToken)
    {
        // Giữ thông báo loại sân cả khi các trường khác trong form chưa hợp lệ.
        if (!await sanService.KiemTraLoaiSanAsync(model.MaLoaiSan, null, cancellationToken))
            ThemLoiGhi(MaLoiSan.LoaiSanKhongHopLe, false);
        if (!ModelState.IsValid)
        {
            await NapLoaiSan(model, cancellationToken);
            return View(model);
        }

        var ketQua = await sanService.TaoAsync(ThanhYeuCau(model), cancellationToken);
        if (!ketQua.ThanhCong)
        {
            ThemLoiGhi(ketQua.MaLoi, false);
            await NapLoaiSan(model, cancellationToken);
            return View(model);
        }
        TempData["Success"] = "Đã thêm sân thành công.";
        return RedirectToAction(nameof(Details), new { id = ketQua.DuLieu });
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var san = await sanService.LayChiTietAsync(id, cancellationToken);
        if (san is null) return NotFound();
        var model = new SuaSanViewModel
        {
            MaSan = san.MaSan, TenSan = san.TenSan, MaLoaiSan = san.MaLoaiSan,
            DiaChi = san.DiaChi, TienIch = san.TienIch, DonGia = san.DonGia,
            DonGiaCaoDiem = san.DonGiaCaoDiem ?? san.DonGia,
            DonGiaCuoiTuan = san.DonGiaCuoiTuan ?? san.DonGia,
            TrangThai = san.TrangThai, GioMoCua = san.GioMoCua, GioDongCua = san.GioDongCua,
            NgayBaoTri = san.NgayBaoTri, GhiChu = san.GhiChu
        };
        await NapLoaiSan(model, cancellationToken, san.MaLoaiSan);
        return View(model);
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SuaSanViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.MaSan) return BadRequest();
        // Giữ thứ tự: sân không tồn tại trả 404 kể cả khi form sai.
        var san = await sanService.LayChiTietAsync(id, cancellationToken);
        if (san is null) return NotFound();
        if (!await sanService.KiemTraLoaiSanAsync(model.MaLoaiSan, san.MaLoaiSan, cancellationToken))
            ThemLoiGhi(MaLoiSan.LoaiSanKhongHopLe, true);
        if (!ModelState.IsValid)
        {
            await NapLoaiSan(model, cancellationToken, san.MaLoaiSan);
            return View(model);
        }

        var ketQua = await sanService.SuaAsync(id, ThanhYeuCau(model), cancellationToken);
        if (ketQua.MaLoi == MaLoiSan.KhongTonTai) return NotFound();
        if (!ketQua.ThanhCong)
        {
            ThemLoiGhi(ketQua.MaLoi, true);
            await NapLoaiSan(model, cancellationToken, san.MaLoaiSan);
            return View(model);
        }
        TempData["Success"] = "Đã cập nhật sân thành công.";
        return RedirectToAction(nameof(Details), new { id = ketQua.DuLieu });
    }

    private async Task NapLoaiSan(ThemSanViewModel model, CancellationToken cancellationToken, int? maLoaiSanHienTai = null)
    {
        var cacLoai = await sanService.LayLuaChonAsync(maLoaiSanHienTai, cancellationToken);
        model.CacLoaiSan = cacLoai.Select(x => new LuaChonLoaiSan(x.MaLoaiSan,
            x.DangHoatDong ? x.TenLoai : x.TenLoai + " (ngừng hoạt động)",
            x.DonGiaTheoGio, x.DonGiaCaoDiem, x.DonGiaCuoiTuan)).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] TraCuuSanViewModel dieuKien, CancellationToken cancellationToken = default)
    {
        var cacLoai = await sanService.LayLoaiSanLocAsync(cancellationToken);
        var trangSan = new TrangSanDoc([], 1, 1, 0);
        if (ModelState.IsValid)
        {
            dieuKien.TuKhoa = dieuKien.TuKhoa?.Trim();
            var ketQua = await sanService.LayDanhSachAsync(new(dieuKien.TuKhoa, dieuKien.MaLoaiSan,
                dieuKien.TrangThai, dieuKien.GiaTu, dieuKien.GiaDen, dieuKien.SapXep, dieuKien.Trang), cancellationToken);
            switch (ketQua.MaLoi)
            {
                case MaLoiSan.ThanhCong: trangSan = ketQua.DuLieu; break;
                case MaLoiSan.DuLieuKhongHopLe:
                    ModelState.AddModelError(string.Empty, "Điều kiện tra cứu sân không hợp lệ.");
                    break;
                default: throw new InvalidOperationException("Mã lỗi tra cứu sân chưa được ánh xạ.");
            }
        }
        return View(new DanhSachSanViewModel
        {
            DanhSachSan = trangSan.DanhSach.Select(ThanhViewModel).ToList(),
            TrangHienTai = trangSan.Trang,
            TongSoTrang = trangSan.TongTrang,
            TongSoSan = trangSan.TongSoSan,
            TuKhoa = dieuKien.TuKhoa, MaLoaiSan = dieuKien.MaLoaiSan, TrangThai = dieuKien.TrangThai,
            GiaTu = dieuKien.GiaTu, GiaDen = dieuKien.GiaDen, SapXep = dieuKien.SapXep, Trang = trangSan.Trang,
            CacLoaiSan = cacLoai.Select(x => new LoaiSanLocViewModel(x.MaLoaiSan, x.TenLoai)).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken,
        DateOnly? ngay = null, TimeOnly? batDau = null, TimeOnly? ketThuc = null)
    {
        var san = await sanService.LayChiTietAsync(id, cancellationToken);
        if (san is null) return NotFound();
        ViewData["NgayTinhGia"] = ngay?.ToString("yyyy-MM-dd");
        ViewData["GioBatDau"] = batDau?.ToString("HH:mm");
        ViewData["GioKetThuc"] = ketThuc?.ToString("HH:mm");
        if (ngay.HasValue || batDau.HasValue || ketThuc.HasValue || !ModelState.IsValid)
        {
            if (!ngay.HasValue || !batDau.HasValue || !ketThuc.HasValue)
                ModelState.AddModelError(string.Empty, "Nhập đủ ngày thuê, giờ bắt đầu và giờ kết thúc để tính giá.");
            if (ModelState.IsValid)
            {
                var gia = await sanService.TinhGiaAsync(id, ngay!.Value,
                    batDau!.Value, ketThuc!.Value, cancellationToken);
                switch (gia.MaLoi)
                {
                    case MaLoiSan.ThanhCong: ViewData["GiaDuKien"] = gia.DuLieu; break;
                    case MaLoiSan.KhongTonTai: return NotFound();
                    case MaLoiSan.GioThueKhongHopLe:
                        ModelState.AddModelError(string.Empty, "Giờ thuê phải trong giờ phục vụ và kết thúc sau giờ bắt đầu trong cùng ngày.");
                        break;
                    default: throw new InvalidOperationException("Mã lỗi tính giá sân chưa được ánh xạ.");
                }
            }
        }
        return View(ThanhViewModel(san));
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var san = await sanService.LayChiTietAsync(id, cancellationToken);
        return san is null ? NotFound() : View(ThanhViewModel(san));
    }

    [Authorize(Roles = nameof(VaiTro.Admin) + "," + nameof(VaiTro.NhanVien))]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var ketQua = await sanService.XoaAsync(id, cancellationToken);
        switch (ketQua.MaLoi)
        {
            case MaLoiSan.KhongTonTai: return NotFound();
            case MaLoiSan.DangDuocThamChieu:
                var san = await sanService.LayChiTietAsync(id, cancellationToken);
                if (san is null) return NotFound();
                ModelState.AddModelError(string.Empty, "Không thể xóa sân vì đã có dữ liệu liên quan. Hãy giữ sân và chuyển sang ngừng hoạt động.");
                return View("Delete", ThanhViewModel(san));
            case MaLoiSan.DaBiXoa:
                TempData["Warning"] = "Sân đã được xóa bởi một thao tác khác. Danh sách đã được cập nhật.";
                return RedirectToAction(nameof(Index));
            case MaLoiSan.ThanhCong:
                TempData["Success"] = "Đã xóa sân thành công.";
                return RedirectToAction(nameof(Index));
            default: throw new InvalidOperationException("Mã lỗi xóa sân chưa được ánh xạ.");
        }
    }

    private void ThemLoiGhi(MaLoiSan maLoi, bool laSua)
    {
        var (truong, thongBao) = maLoi switch
        {
            MaLoiSan.LoaiSanKhongHopLe => (nameof(ThemSanViewModel.MaLoaiSan), laSua
                ? "Loại sân không hợp lệ hoặc đã ngừng hoạt động."
                : "Vui lòng chọn loại sân đang hoạt động."),
            MaLoiSan.DuLieuKhongHopLe => (string.Empty, "Thông tin sân không hợp lệ."),
            MaLoiSan.DuLieuLienQuanThayDoi => (string.Empty, laSua
                ? "Không lưu được vì dữ liệu liên quan đã thay đổi. Vui lòng kiểm tra lại thông tin sân."
                : "Dữ liệu không còn hợp lệ. Vui lòng kiểm tra loại sân, giá và giờ hoạt động rồi thử lại."),
            _ => throw new InvalidOperationException("Mã lỗi ghi sân chưa được ánh xạ.")
        };
        ModelState.AddModelError(truong, thongBao);
    }

    private static GhiSan ThanhYeuCau(ThemSanViewModel x)
        => new(x.TenSan, x.MaLoaiSan!.Value, x.DiaChi, x.TienIch, x.DonGia!.Value,
            x.DonGiaCaoDiem!.Value, x.DonGiaCuoiTuan!.Value, x.TrangThai!.Value,
            x.GioMoCua!.Value, x.GioDongCua!.Value, x.NgayBaoTri, x.GhiChu);

    private static SanChiTietViewModel ThanhViewModel(SanDoc x)
        => new(x.MaSan, x.TenSan, x.MaLoaiSan, x.TenLoai, x.DiaChi, x.TienIch,
            x.DonGia, x.DonGiaCaoDiem, x.DonGiaCuoiTuan, x.TrangThai,
            x.GioMoCua, x.GioDongCua, x.NgayBaoTri, x.GhiChu);

}

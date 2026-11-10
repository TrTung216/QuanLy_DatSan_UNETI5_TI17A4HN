// M3: Nguyễn Văn Quý; MSSV: 23103100181. khách hàng tạo đơn đặt sân, xem lịch đặt cá nhân và chi tiết đơn của chính mình.
// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung M4: chuyển truy vấn sang IDatSanService; ánh xạ DTO sang ViewModel và mã lỗi sang thông báo cho luồng đặt sân của khách hàng.
// M2: Phan Giang Tâm - 23103100196. Nhận sân và khung giờ từ trang tra cứu sang form đặt sân.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.ViewModels;
namespace QuanLyDatSan_UNETI5_DHTI17A4HN.Controllers;

[Authorize(Roles = nameof(VaiTro.KhachHang))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DonDatController(ITaiKhoanHienTai taiKhoanHienTai,
    IDatSanService datSanService, IDongHo dongHo) : Controller
{

    [HttpGet]
    public async Task<IActionResult> Index(TrangThaiDatSan? trangThai, int trang = 1, CancellationToken cancellationToken = default)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();
        var duLieu = await datSanService.LayDanhSachCaNhanAsync(maTaiKhoan, trangThai, trang, cancellationToken);
        return View(new DanhSachDonViewModel
        {
            Dong = duLieu.Dong.Select(x => new DonDatDongViewModel
            {
                MaDatSan = x.MaDatSan,
                TenSan = x.TenSan,
                TenLoai = x.TenLoai,
                GioBatDau = x.GioBatDau,
                GioKetThuc = x.GioKetThuc,
                DonGia = x.DonGia,
                TrangThai = x.TrangThai,
            }).ToList(),
            TrangThai = duLieu.TrangThai, Trang = duLieu.Trang,
            TongTrang = duLieu.TongTrang, TongSo = duLieu.TongSo
        });
    }

    [HttpGet]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        var model = await LayChiTietViewModelAsync(maTaiKhoan, id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Coc(int id, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        var ketQua = await datSanService.LayThongTinCocAsync(id, cancellationToken);
        if (!ketQua.ThanhCong)
        {
            switch (ketQua.Loi!.Value)
            {
                case MaLoiCoc.KhongDuQuyen: return Forbid();
                case MaLoiCoc.KhongTonTai: return NotFound();
                default:
                    TempData["Error"] = HienThiCoc.ThongBaoLoi(ketQua.Loi.Value);
                    return RedirectToAction(nameof(ChiTiet), new { id });
            }
        }

        var model = TaoCocViewModel(ketQua.DuLieu!);
        // Phiên bản chỉ để nút Hủy trên trang QR gửi lại cho service kiểm tra; không phải căn cứ cấp quyền.
        var don = await datSanService.LayChiTietCaNhanAsync(maTaiKhoan, id, cancellationToken);
        if (don is not null) model.PhienBanDon = Convert.ToBase64String(don.PhienBan);
        return View(model);
    }

    private static CocDonViewModel TaoCocViewModel(ThongTinCocDto coc)
    {
        return new CocDonViewModel
        {
            MaDatSan = coc.MaDatSan,
            TrangThaiDon = coc.TrangThai,
            TrangThaiCoc = coc.TrangThaiCoc,
            TienSanDuKien = coc.TienSanDuKien,
            TienCocYeuCau = coc.TienCocYeuCau,
            TienCocDaNhan = coc.TienCocDaNhan,
            NhanPhanConLai = HienThiCoc.NhanPhanConLai(coc.TrangThai),
            NoiDungChuyenKhoan = coc.NoiDungChuyenKhoan,
            CoTheChuyen = coc.CoTheChuyen,
            NoiDungQrMoPhong = coc.NoiDungQrMoPhong,
            BienNhan = coc.BienNhan is null ? null : new BienNhanCocViewModel
            {
                MaBienNhan = coc.BienNhan.MaBienNhan,
                SoTien = coc.BienNhan.SoTien,
                MaGiaoDichNgoai = coc.BienNhan.MaGiaoDichNgoai,
                Nguon = coc.BienNhan.Nguon,
                NgayNhan = coc.BienNhan.NgayNhan,
                NgayXacNhan = coc.BienNhan.NgayXacNhan
            }
        };
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(HuyDonCaNhanViewModel model, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();

        if (!TryLayPhienBan(model.PhienBan, out var phienBan))
            ModelState.AddModelError(nameof(HuyDonCaNhanViewModel.PhienBan),
                "Phiên bản đơn không hợp lệ. Hãy tải lại chi tiết đơn.");
        if (!ModelState.IsValid)
            return await HienThiLoiHuyAsync(maTaiKhoan, model, cancellationToken);

        var ketQua = await datSanService.KhachHangHuyDonAsync(
            new HuyDonYeuCau(model.MaDon, model.LyDo, phienBan), maTaiKhoan, cancellationToken);
        if (!ketQua.ThanhCong)
        {
            if (ketQua.Loi == MaLoiDatSan.KhongTonTai) return NotFound();
            ModelState.AddModelError(string.Empty,
                ThongBaoLoiDatSanViewModel.AnhXa(new LoiDatSan(ketQua.Loi!.Value)).ThongBao);
            return await HienThiLoiHuyAsync(maTaiKhoan, model, cancellationToken);
        }

        TempData["Success"] = "Đã hủy đơn đặt sân.";
        return RedirectToAction(nameof(ChiTiet), new { id = model.MaDon });
    }

    private async Task<IActionResult> HienThiLoiHuyAsync(int maTaiKhoan, HuyDonCaNhanViewModel model,
        CancellationToken cancellationToken)
    {
        var chiTiet = await LayChiTietViewModelAsync(maTaiKhoan, model.MaDon, cancellationToken);
        if (chiTiet is null) return NotFound();
        chiTiet.LyDoHuyDaNhap = model.LyDo;
        return View(nameof(ChiTiet), chiTiet);
    }

    private async Task<ChiTietDonViewModel?> LayChiTietViewModelAsync(int maTaiKhoan, int id,
        CancellationToken cancellationToken)
    {
        var don = await datSanService.LayChiTietCaNhanAsync(maTaiKhoan, id, cancellationToken);
        if (don is null) return null;
        // Thông tin cọc chỉ để hiển thị; lỗi ở đây không làm hỏng trang chi tiết đơn.
        var coc = await datSanService.LayThongTinCocAsync(id, cancellationToken);
        var cocHienThi = coc.ThanhCong ? TaoCocViewModel(coc.DuLieu!) : null;
        // Không tự tính tiền: chỉ chuyển các khoản service đã cung cấp.
        var lyDoKhongHuy = HienThiCoc.LyDoKhongTheHuy(
            cocHienThi?.TrangThaiCoc ?? TrangThaiCocHienThi.KhongApDung, don.TienCoc);
        var conTheHuyTheoDon = don.TrangThai == TrangThaiDatSan.ChoXuLy && don.GioBatDau > dongHo.BayGio;
        return new ChiTietDonViewModel
        {
            MaDatSan = don.MaDatSan,
            TenSan = don.TenSan,
            TenLoai = don.TenLoai,
            DiaChi = don.DiaChi,
            NgayDat = don.NgayDat,
            GioBatDau = don.GioBatDau,
            GioKetThuc = don.GioKetThuc,
            DonGia = don.DonGia,
            TienCoc = don.TienCoc,
            TrangThai = don.TrangThai,
            NgayXacNhan = don.NgayXacNhan,
            NgayHuy = don.NgayHuy,
            NgayHoanThanh = don.NgayHoanThanh,
            LyDoHuy = don.LyDoHuy,
            TienSan = don.TienSan,
            TienDichVu = don.TienDichVu,
            TongTien = don.TongTien,
            TienSanDuKien = don.TienSanDuKien,
            PhienBanDon = Convert.ToBase64String(don.PhienBan),
            Coc = cocHienThi,
            NhanPhanConLai = HienThiCoc.NhanPhanConLai(don.TrangThai),
            LyDoKhongTheHuy = conTheHuyTheoDon ? lyDoKhongHuy : null,
            // Chỉ để ẩn/hiện nút; service kiểm tra lại toàn bộ điều kiện khi POST.
            CoTheHuy = conTheHuyTheoDon && lyDoKhongHuy is null,
        };
    }

    private static bool TryLayPhienBan(string? encoded, out byte[] phienBan)
    {
        phienBan = [];
        if (string.IsNullOrWhiteSpace(encoded)) return false;
        try
        {
            phienBan = Convert.FromBase64String(encoded);
            return phienBan.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Tao(int? maSan, CancellationToken cancellationToken,
        DateOnly? ngaySuDung = null, TimeOnly? gioBatDau = null, TimeOnly? gioKetThuc = null)
    {
        var model = new TaoDonViewModel
        {
            MaSan = maSan,
            NgaySuDung = ngaySuDung,
            GioBatDau = gioBatDau,
            GioKetThuc = gioKetThuc
        };
        var coKhungGio = ngaySuDung.HasValue || gioBatDau.HasValue || gioKetThuc.HasValue;
        if (coKhungGio && (!ngaySuDung.HasValue || !gioBatDau.HasValue || !gioKetThuc.HasValue))
            ModelState.AddModelError(string.Empty, "Nhập đủ ngày sử dụng, giờ bắt đầu và giờ kết thúc.");
        await NapDanhSachSanAsync(model, cancellationToken);
        if (maSan.HasValue && !model.DanhSachSan.Any(san => san.MaSan == maSan.Value))
            ModelState.AddModelError(nameof(TaoDonViewModel.MaSan), "Sân đã ngừng nhận đặt hoặc không còn tồn tại. Vui lòng chọn sân khác.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tao(TaoDonViewModel model, CancellationToken cancellationToken)
    {
        if (taiKhoanHienTai.MaTaiKhoan is not { } maTaiKhoan) return Challenge();
        if (!ModelState.IsValid)
        {
            await NapDanhSachSanAsync(model, cancellationToken);
            return View(model);
        }

        var ketQua = await datSanService.TaoDonAsync(new TaoDonYeuCau(maTaiKhoan, model.MaSan!.Value, model.NgaySuDung!.Value,
            model.GioBatDau!.Value, model.GioKetThuc!.Value), cancellationToken);
        if (!ketQua.ThanhCong)
        {
            var loi = ThongBaoLoiDatSanViewModel.AnhXa(ketQua.Loi!);
            ModelState.AddModelError(loi.Truong, loi.ThongBao);
            await NapDanhSachSanAsync(model, cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Đặt sân thành công. Đơn đang chờ nhân viên xác nhận.";
        return RedirectToAction(nameof(ChiTiet), new { id = ketQua.MaDatSan });
    }

    private async Task NapDanhSachSanAsync(TaoDonViewModel model, CancellationToken cancellationToken)
    {
        var luaChon = await datSanService.LayLuaChonSanAsync(cancellationToken);
        model.DanhSachSan = luaChon.Select(x => new SanChonViewModel(x.MaSan, x.TenSan, x.TenLoai,
            x.DonGia, x.GioMoCua, x.GioDongCua)).ToList();
    }
}

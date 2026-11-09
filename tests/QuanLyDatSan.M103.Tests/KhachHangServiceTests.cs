// M3: Nguyễn Văn Quý; MSSV: 23103100181. Kiểm thử validation hồ sơ khách ở tầng service, gọi trực tiếp không qua MVC.
using System.Data;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Enums;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Models;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.Chung;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Repositories.QuanLyKhachHang;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services;
using QuanLyDatSan_UNETI5_DHTI17A4HN.Services.QuanLyKhachHang;
using Xunit;

namespace QuanLyDatSan.M103.Tests;

public class KhachHangServiceTests
{
    private static readonly DateOnly HomNay = new(2026, 11, 3);

    private sealed class Dong : IDongHo
    {
        public DateTime BayGio => HomNay.ToDateTime(new TimeOnly(9, 0));
        public DateOnly HomNay => KhachHangServiceTests.HomNay;
    }

    private sealed class TaiKhoanGia(int? ma) : ITaiKhoanHienTai
    {
        public int? MaTaiKhoan => ma;
        public string? HoTen => null;
        public QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro? VaiTro => QuanLyDatSan_UNETI5_DHTI17A4HN.Enums.VaiTro.KhachHang;
    }

    private sealed class KhoGia(KhachHang? khach) : IKhachHangRepository
    {
        public int SoLanNap { get; private set; }
        public Task<HoSoKhachDto?> LayHoSoAsync(int maTaiKhoan, CancellationToken ct) => Task.FromResult<HoSoKhachDto?>(null);
        public Task<KhachHang?> LayDeCapNhatAsync(int maTaiKhoan, CancellationToken ct)
        {
            SoLanNap++;
            return Task.FromResult(khach);
        }
    }

    private sealed class DonViGia : IDonViCongViec
    {
        public int SoLanLuu { get; private set; }
        public Task LuuAsync(CancellationToken ct) { SoLanLuu++; return Task.CompletedTask; }
        public Task<T> ThucHienTrongGiaoDichAsync<T>(Func<CancellationToken, Task<QuyetDinhLuu<T>>> congViec,
            IsolationLevel mucCoLap, CancellationToken ct) => throw new NotSupportedException();
    }

    private static KhachHang TaoKhach(TrangThaiKhachHang trangThai = TrangThaiKhachHang.HoatDong) => new()
    {
        MaKhachHang = 1, MaTaiKhoan = 7, HoTen = "Cũ", Email = "cu@example.test", SoDienThoai = "0900000000",
        TrangThai = trangThai,
        TaiKhoan = new TaiKhoan { MaTaiKhoan = 7, HoTen = "Cũ", Email = "cu@example.test" }
    };

    private static CapNhatHoSoDto Hop(Func<CapNhatHoSoDto, CapNhatHoSoDto>? sua = null)
    {
        var dto = new CapNhatHoSoDto("Nguyễn Văn A", "a@example.test", "0912345678", new DateOnly(2000, 1, 1),
            GioiTinh.Nam, "Hà Nội");
        return sua is null ? dto : sua(dto);
    }

    private static (KhachHangService Service, KhoGia Kho, DonViGia DonVi) Tao(KhachHang? khach, int? maTaiKhoan = 7)
    {
        var kho = new KhoGia(khach);
        var donVi = new DonViGia();
        return (new KhachHangService(kho, donVi, new TaiKhoanGia(maTaiKhoan), new Dong()), kho, donVi);
    }

    public static TheoryData<string, MaLoiHoSo> DuLieuSai => new()
    {
        { "ho-ten-rong", MaLoiHoSo.HoTenKhongHopLe },
        { "ho-ten-trang", MaLoiHoSo.HoTenKhongHopLe },
        { "ho-ten-null", MaLoiHoSo.HoTenKhongHopLe },
        { "ho-ten-dai", MaLoiHoSo.HoTenKhongHopLe },
        { "email-rong", MaLoiHoSo.EmailKhongHopLe },
        { "email-sai", MaLoiHoSo.EmailKhongHopLe },
        { "email-null", MaLoiHoSo.EmailKhongHopLe },
        { "email-dai", MaLoiHoSo.EmailKhongHopLe },
        { "sdt-rong", MaLoiHoSo.SoDienThoaiKhongHopLe },
        { "sdt-chu", MaLoiHoSo.SoDienThoaiKhongHopLe },
        { "sdt-ngan", MaLoiHoSo.SoDienThoaiKhongHopLe },
        { "sdt-dai", MaLoiHoSo.SoDienThoaiKhongHopLe },
        { "sdt-null", MaLoiHoSo.SoDienThoaiKhongHopLe },
        { "ngay-sinh-tuong-lai", MaLoiHoSo.NgaySinhKhongHopLe },
        { "ngay-sinh-truoc-1900", MaLoiHoSo.NgaySinhKhongHopLe },
        { "gioi-tinh-la", MaLoiHoSo.GioiTinhKhongHopLe },
        { "dia-chi-dai", MaLoiHoSo.DiaChiKhongHopLe },
    };

    private static CapNhatHoSoDto TaoDuLieuSai(string ten) => ten switch
    {
        "ho-ten-rong" => Hop(d => d with { HoTen = "" }),
        "ho-ten-trang" => Hop(d => d with { HoTen = "   " }),
        "ho-ten-null" => Hop(d => d with { HoTen = null! }),
        "ho-ten-dai" => Hop(d => d with { HoTen = new string('a', 101) }),
        "email-rong" => Hop(d => d with { Email = "" }),
        "email-sai" => Hop(d => d with { Email = "khong-phai-email" }),
        "email-null" => Hop(d => d with { Email = null! }),
        "email-dai" => Hop(d => d with { Email = new string('a', 250) + "@x.vn" }),
        "sdt-rong" => Hop(d => d with { SoDienThoai = "" }),
        "sdt-chu" => Hop(d => d with { SoDienThoai = "09abc45678" }),
        "sdt-ngan" => Hop(d => d with { SoDienThoai = "091234" }),
        "sdt-dai" => Hop(d => d with { SoDienThoai = "0912345678901234567890" }),
        "sdt-null" => Hop(d => d with { SoDienThoai = null! }),
        "ngay-sinh-tuong-lai" => Hop(d => d with { NgaySinh = HomNay.AddDays(1) }),
        "ngay-sinh-truoc-1900" => Hop(d => d with { NgaySinh = new DateOnly(1899, 12, 31) }),
        "gioi-tinh-la" => Hop(d => d with { GioiTinh = (GioiTinh)99 }),
        "dia-chi-dai" => Hop(d => d with { DiaChi = new string('d', 256) }),
        _ => throw new ArgumentOutOfRangeException(nameof(ten))
    };

    [Theory]
    [MemberData(nameof(DuLieuSai))]
    public async Task DuLieuSaiBiChanTruocKhiNapVaKhongGhiBanGhiNao(string ten, MaLoiHoSo loiDung)
    {
        var khach = TaoKhach();
        var (service, kho, donVi) = Tao(khach);

        var ketQua = await service.CapNhatHoSoAsync(TaoDuLieuSai(ten), default);

        Assert.False(ketQua.ThanhCong);
        Assert.Equal(loiDung, ketQua.Loi);
        Assert.Equal(0, kho.SoLanNap);
        Assert.Equal(0, donVi.SoLanLuu);
        Assert.Equal("Cũ", khach.HoTen); Assert.Equal("Cũ", khach.TaiKhoan.HoTen);
        Assert.Equal("cu@example.test", khach.Email); Assert.Equal("cu@example.test", khach.TaiKhoan.Email);
    }

    [Fact]
    public async Task DuLieuHopLeCapNhatCaHoSoVaTaiKhoanTrongMotLanLuu()
    {
        var khach = TaoKhach();
        var (service, _, donVi) = Tao(khach);

        var ketQua = await service.CapNhatHoSoAsync(Hop(d => d with
        {
            HoTen = "  Nguyễn Văn A  ", Email = " a@example.test ", SoDienThoai = " +84912345678 ", DiaChi = "   "
        }), default);

        Assert.True(ketQua.ThanhCong);
        Assert.Equal(1, donVi.SoLanLuu);
        Assert.Equal("Nguyễn Văn A", khach.HoTen); Assert.Equal(khach.HoTen, khach.TaiKhoan.HoTen);
        Assert.Equal("a@example.test", khach.Email); Assert.Equal(khach.Email, khach.TaiKhoan.Email);
        Assert.Equal("+84912345678", khach.SoDienThoai);
        Assert.Null(khach.DiaChi);
    }

    [Theory]
    [InlineData(100, 254, 255)]
    public async Task GiaTriDungBienToiDaDuocChapNhan(int hoTen, int email, int diaChi)
    {
        var (service, _, donVi) = Tao(TaoKhach());
        var phanDia = new string('a', email - "@x.vn".Length);

        var ketQua = await service.CapNhatHoSoAsync(Hop(d => d with
        {
            HoTen = new string('a', hoTen), Email = phanDia + "@x.vn", DiaChi = new string('d', diaChi)
        }), default);

        Assert.True(ketQua.ThanhCong, ketQua.Loi?.ToString());
        Assert.Equal(1, donVi.SoLanLuu);
    }

    [Fact]
    public async Task HoSoBiKhoaKhongDuocSuaVaKhongGhi()
    {
        var khach = TaoKhach(TrangThaiKhachHang.BiKhoa);
        var (service, _, donVi) = Tao(khach);

        var ketQua = await service.CapNhatHoSoAsync(Hop(), default);

        Assert.Equal(MaLoiHoSo.HoSoBiKhoa, ketQua.Loi);
        Assert.Equal(0, donVi.SoLanLuu);
        Assert.Equal("Cũ", khach.HoTen);
    }

    [Fact]
    public async Task ChuaDangNhapHoacKhongCoHoSoTraKhongTonTai()
    {
        var (chuaDangNhap, _, donVi1) = Tao(TaoKhach(), maTaiKhoan: null);
        Assert.Equal(MaLoiHoSo.KhongTonTai, (await chuaDangNhap.CapNhatHoSoAsync(Hop(), default)).Loi);
        Assert.Equal(0, donVi1.SoLanLuu);

        var (khongHoSo, _, donVi2) = Tao(null);
        Assert.Equal(MaLoiHoSo.KhongTonTai, (await khongHoSo.CapNhatHoSoAsync(Hop(), default)).Loi);
        Assert.Equal(0, donVi2.SoLanLuu);
    }
}

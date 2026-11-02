# Hợp đồng M3/M4/M5 — đặt sân, tiền cọc và chốt tiền

**Phiên bản 1.0 — 23/10/2026. Điều phối: Trần Trọng Tùng — MSSV 23103100202 (M1).**

Nội dung thực hiện: chốt hợp đồng phương thức, dữ liệu, quyền, trạng thái, mã lỗi và nguồn tính tiền giữa M3/M4/M5.

Căn cứ: `main` tại `1a97883` sau PR #52, #53, #54; quy ước nhóm v1.4; yêu cầu khách chuyển cọc 50% trước, nhân viên/Admin đối soát rồi xác nhận đặt sân. Đây là hợp đồng triển khai được chốt theo yêu cầu của Tùng, không phải xác nhận các thành viên đã review hoặc code đã đáp ứng. Lần này chỉ cập nhật tài liệu, không triển khai lại QR, sửa schema hay chạy Update-Database.

Trong phạm vi cọc/tính tiền, tài liệu này là nguồn thống nhất cho phương thức, DTO, trạng thái và lỗi; thay thế các chỗ “cần chốt” tương ứng trong [phân công](Phan_cong_chuyen_khoan_QR.md) và [quy ước cọc](Quy_uoc_dat_coc_chuyen_khoan.md). Mọi thay đổi tiếp theo cần tăng phiên bản và cập nhật nơi gọi trong cùng PR.

## 1. Code đã có và phần phải chuyển đổi

| Thành phần | Hiện trạng đã đọc trên main | Hợp đồng đích |
| --- | --- | --- |
| `IDatSanService` | Tạo/đọc đơn, xử lý trạng thái, khóa sân, kiểm tra khả dụng theo lô | Giữ một service điều phối M3/M4; bổ sung cọc và chốt tiền tại đây, có thể dùng file partial. |
| `ITinhTienService` | Đã có `TinhTienSan`, `TinhTienDichVu`, `TinhTongTien` | Là nguồn công thức chính thức; M5 bổ sung cọc/còn phải thu. Không tạo service tính tiền thứ hai. |
| `DatSanService.TinhTienSanDuKien` | Vẫn lặp công thức M5 | M4 chuyển các caller đọc/tạo/chốt sang `ITinhTienService`; bỏ hàm cũ khi hết caller, cập nhật test/constructor cùng PR. |
| `IDichVuTrongDonService` | Đã có đọc/thêm/sửa/hủy; đang cho sửa cả `ChoXuLy` và `DangXuLy`, hủy bằng xóa dòng | Chỉ sửa ở `DangXuLy` theo quy ước v1.4; hủy có trạng thái để giữ lịch sử; đồng bộ khóa/phiên bản với M4. |
| DTO dịch vụ hiện tại | `ChiTietDichVuTrongDon` chứa `List<DichVu>` | M5 thay Entity bằng DTO lựa chọn. Khách chỉ nhận các dòng của đơn mình, không nhận danh sách dùng để quản trị. |
| `HoanThanhDonAsync` | Nhận `TienSan`, `TienDichVu` nội bộ nhưng chưa tự tải/tính dưới khóa | Thêm entry point chốt đơn không nhận tiền từ HTTP; tính lại từ dữ liệu DB trong transaction. |
| Cọc/biên nhận | Chưa có sau khi gỡ thử nghiệm | Các kiểu và phương thức mới dưới đây là thiết kế cần triển khai. |

Các file dưới project `QuanLyDatSan_UNETI5_DHTI17A4HN/`; tên mới trong tài liệu chưa phải file đã tồn tại.

## 2. Quyết định nghiệp vụ áp dụng v1

1. **Giữ chỗ:** `ChoXuLy`, `DangXuLy`, `HoanThanh` chiếm lịch; không tự hết hạn trong v1. Không thêm job hoặc đếm ngược 15 phút. Hai khoảng giờ liền kề không trùng.
2. **Đơn cũ:** migration sắp tới đặt `PhienBanChinhSachCoc = 0`, `TienCocYeuCau = 0` cho dữ liệu cũ; không sửa `TienCoc`, trạng thái hoặc lịch sử đã có. Đơn cũ tiếp tục quy tắc hiện tại, không tự sinh biên nhận cho tiền cọc lịch sử.
3. **Đơn mới:** sau khi triển khai trọn luồng, `TaoDonAsync` tự ghi phiên bản chính sách `1`, tính và lưu `TienCocYeuCau` từ tiền sân tại lúc tạo. Client không chọn phiên bản/tỷ lệ/số cọc. Khi chỉ merge nền dữ liệu thì chưa kích hoạt tạo đơn chính sách 1. Sau khi kích hoạt, tắt mô phỏng/QR không được làm đơn mới quay về chính sách 0; phải hiển thị chưa sẵn sàng, không mở đường bỏ qua cọc.
4. **Số cọc bằng 0:** đơn miễn phí hoặc làm tròn ra 0 không chuyển khoản, không tạo biên nhận 0 đồng; xác nhận qua `XacNhanDonAsync` với đủ điều kiện sân/giờ/quyền. Không dựa vào checkbox client để miễn cọc.
5. **Đơn chính sách 1, cọc > 0:** chỉ `XacNhanCocVaDatSanAsync` được chuyển sang `DangXuLy`. `XacNhanDonAsync` cũ trả lỗi `YeuCauDoiSoatCoc`, không được là đường đi vòng.
6. **Thời gian:** nhận tiền từ `NgayDat` đến trước `GioBatDau`, không ở tương lai. Xác nhận cọc/đặt sân cũng phải trước `GioBatDau`. Đối soát muộn sau đó chuyển ngoại lệ thủ công; không ép xác nhận đơn đã quá giờ.
7. **Hủy/từ chối:** giữ điều kiện trạng thái/giờ/lý do hiện có; bổ sung chặn nếu đã có biên nhận hoặc có giao dịch đã gắn đúng nội dung đơn nhưng chưa xử lý xong, kể cả sai số tiền. Khi chưa có giao dịch được ghi nhận, khách có thể hủy đơn chờ trước giờ; tiền đến sau khi hủy được lưu ngoại lệ, không mở lại đơn.
8. **Nguồn tiền:** v1 chỉ `MoPhong`, phải đồng thời Development và cấu hình bật. Không có adapter ngân hàng thật, không tự tin ảnh chụp/lời báo của khách. Không có chức năng tự xác nhận thanh toán phía khách.
9. **Dịch vụ:** chỉ Admin/Nhân viên sửa khi `DangXuLy`; không thay đổi cọc yêu cầu khi thêm/hủy dịch vụ. Đơn cuối cùng không cho sửa tiền/dịch vụ hoặc mở lại.

Đây là lựa chọn tích hợp v1, không phải mô tả rằng các điều kiện đã được thêm vào code. Bật nhận tiền thật cần hợp đồng riêng về nguồn giao dịch và ngoại lệ/hoàn tiền; không đổi cờ để coi dữ liệu mô phỏng là tiền thật.

## 3. Nguồn tính tiền — M5 chịu trách nhiệm

Namespace giữ nguyên: `Services.QuanLyTinhTien`. Service chỉ tính, không đọc DB, không mở transaction, không thay trạng thái.

```csharp
public interface ITinhTienService
{
    // Đã có — giữ chữ ký.
    decimal TinhTienSan(decimal donGia, DateTime batDau, DateTime ketThuc);
    decimal TinhTienDichVu(IEnumerable<DongDichVu> cacDong);
    decimal TinhTongTien(decimal tienSan, decimal tienDichVu);
    // Bổ sung.
    decimal TinhTienCocYeuCau(decimal tienSan);
    decimal TinhConPhaiThu(decimal tongTien, decimal tienCoc);
}
```

| Giá trị | Nguồn và công thức |
| --- | --- |
| Đơn giá sân | `DatSan.DonGia` chụp từ server khi tạo. Không lấy lại giá sân hiện tại khi đọc/chốt đơn. |
| Tiền sân | Thời gian chuẩn hóa đến phút; `Round(soPhut / 60m * donGia, 0, AwayFromZero)`. |
| Cọc yêu cầu | `Round(tienSan * 0.5m, 0, AwayFromZero)`, tiền sân đã làm tròn; lưu khi tạo chính sách 1. |
| Tiền dịch vụ | Tổng `SoLuong * DonGia` của dòng `HieuLuc`; giá chụp khi thêm, không đọc giá danh mục hiện tại. |
| Tổng | Tiền sân + tiền dịch vụ; đã bao gồm phần cọc. |
| Còn phải thu | Tổng − cọc đã nhận; không tự `Max(0, ...)` che dữ liệu sai. Cọc > tổng là lỗi. |
| Sau hoàn thành | Đọc các khoản đã chốt trên đơn; không tính lại từ danh mục. Doanh thu cộng `TongTien` của đơn hoàn thành, không cộng cọc lần nữa. |

- Dùng `decimal`, tiền không âm và nằm trong giới hạn `decimal(18,2)`; kiểm tra overflow trước ghi. Thời gian kết thúc > bắt đầu; số lượng dịch vụ 1–100 theo service hiện có. Hàm thuần báo `ArgumentException`/`OverflowException` cho đầu vào sai; service điều phối chỉ ánh xạ các lỗi tiền dự kiến, không nuốt exception hệ thống.
- M5 bổ sung validation cho `TinhTongTien` hiện chỉ cộng trực tiếp. `TinhConPhaiThu` từ chối cọc âm hoặc vượt tổng.
- V1 giữ giá đơn hiện tại (đơn giá thường đã chụp), **không tự đổi sang tính từng khung cao điểm/cuối tuần**. `GiaThueSan` M2 chỉ là ước tính tham khảo hiện có; M2/M3 phải ghi rõ trên giao diện, thông tin cọc lấy từ số tiền chính thức của đơn. Áp dụng giá nhiều khung cần hợp đồng snapshot giá riêng sau này, không tính lại đơn cũ bằng giá danh mục.
- Ví dụ nghiệm thu: 90 phút × 200.000 = 300.000; cọc 150.000; dịch vụ 60.000 → tổng 360.000, còn 210.000. Tiền sân 101 → cọc 51; không có dịch vụ → còn 50.

## 4. Phương thức M3/M4 và dữ liệu vào

Giữ namespace `Services` và `IDatSanService`; M4 chủ trì interface và partial triển khai. Các phương thức **mới** dưới đây lấy actor qua `ITaiKhoanHienTai` ở service, kiểm tra tài khoản/role hiện tại từ DB; không nhận role hoặc mã người xác nhận từ HTTP.

```csharp
Task<KetQuaCoc<ThongTinCocDto>> LayThongTinCocAsync(int maDatSan, CancellationToken ct);
Task<KetQuaCoc<DoiSoatCocDto>> LayDoiSoatCocAsync(int maDatSan, CancellationToken ct);
Task<KetQuaCoc<GiaoDichCocDto>> TaoGiaoDichMoPhongAsync(TaoGiaoDichMoPhongYeuCau yc, CancellationToken ct);
Task<KetQuaCoc<XacNhanCocDto>> XacNhanCocVaDatSanAsync(XacNhanCocYeuCau yc, CancellationToken ct);
Task<KetQuaCoc<TienDonDto>> HoanThanhVaTinhTienAsync(ChotDonYeuCau yc, CancellationToken ct);

public sealed record XacNhanCocYeuCau(int MaDatSan, int MaGiaoDich,
    byte[] PhienBan, bool DaKiemTraGiaoDich);
public sealed record ChotDonYeuCau(int MaDatSan, byte[] PhienBan, bool DaThuDu);
public sealed record TaoGiaoDichMoPhongYeuCau(string MaGiaoDichNgoai,
    string NoiDung, decimal SoTien, DateTime NgayNhan);
```

- `MaDatSan`, `MaGiaoDich` dương. `PhienBan` là 8 byte RowVersion; form dùng Base64, controller kiểm tra giải mã/độ dài, service kiểm tra lại.
- `DaKiemTraGiaoDich` chỉ xác nhận nhân viên đã kiểm tra; số tiền/nội dung/tài khoản nhận lấy từ bản ghi giao dịch server. Không nhận tiền hoặc nội dung để ghi đè tại POST xác nhận.
- Tạo giao dịch mô phỏng là công cụ Admin/Nhân viên Development, không phải endpoint khách. Số tiền dương, tối đa giới hạn tiền; thời điểm không ở tương lai. Cho phép lệch tiền/nội dung/ngày so với đơn để test nhánh lỗi; không coi tạo giao dịch là xác nhận đơn.
- Nội dung chính thức do server sinh `COC {MaDatSan}`. So sánh sau trim, gom khoảng trắng và uppercase invariant; phải khớp toàn chuỗi, không dùng `Contains` (tránh đơn 12 khớp 123).
- `MaGiaoDichNgoai`: trim, uppercase invariant, 3–100 ký tự chữ/số ASCII hoặc `- _ . /`. Giới hạn nội dung 100 ký tự. Tài khoản nhận/nguồn lấy từ cấu hình mô phỏng server cố định, không bind từ form.
- `TaoDonAsync(TaoDonYeuCau, ct)`, đọc đơn và các phương thức hủy giữ chữ ký hiện có; mã tài khoản lấy từ phiên, service kiểm tra quyền sở hữu. Bổ sung chính sách cọc trong xử lý nội bộ.
- `HoanThanhDonYeuCau` hiện có chứa tiền: chỉ dùng nội bộ trong giai đoạn chuyển đổi, không được model-bind. Route hoàn thành mới phải gọi `HoanThanhVaTinhTienAsync`; M4 tách helper áp dụng trạng thái, không gọi lại method tự mở transaction.

## 5. DTO đầu ra và kết quả

Các DTO mới là immutable record, danh sách `IReadOnlyList<T>`; không trả Entity EF, IQueryable, SelectListItem hoặc HttpContext. Thời gian dùng quy ước `IDongHo`, tiền VND `decimal`.

| DTO | Trường bắt buộc |
| --- | --- |
| `ThongTinCocDto` | `int MaDatSan`, `TrangThaiDatSan TrangThai`, `int PhienBanChinhSachCoc`, `decimal TienSanDuKien, TienCocYeuCau, TienCocDaNhan`, `string NoiDungChuyenKhoan`, `TrangThaiCocHienThi TrangThaiCoc`, `bool MoPhong, CoTheChuyen`, `string? NoiDungQrMoPhong`, `BienNhanCocDto? BienNhan` |
| `DoiSoatCocDto` | `ThongTinCocDto Don`, `byte[] PhienBan`, `IReadOnlyList<GiaoDichCocDto> GiaoDich` (chỉ giao dịch khớp nội dung đơn), `bool CoTheXacNhan` |
| `GiaoDichCocDto` | `int MaGiaoDich`, `string MaGiaoDichNgoai, NoiDung, Nguon, TaiKhoanNhan`, `decimal SoTien`, `DateTime NgayNhan`, `TrangThaiGiaoDichCoc TrangThai` |
| `BienNhanCocDto` | `int MaBienNhan, MaDatSan`, `decimal SoTien`, `string MaGiaoDichNgoai, Nguon`, `DateTime NgayNhan, NgayXacNhan`; không lộ danh sách giao dịch/PII người khác |
| `XacNhanCocDto` | `BienNhanCocDto BienNhan`, `TrangThaiDatSan TrangThai`, `byte[] PhienBanMoi`, `bool DaXuLyTruocDo` |
| `TienDonDto` | `int MaDatSan`, `decimal TienSan, TienDichVu, TongTien, TienCoc, ConPhaiThu`, `TrangThaiDatSan TrangThai`, `DateTime? NgayHoanThanh`, `byte[] PhienBan` |

`KetQuaCoc<T>`: `bool ThanhCong`, `T? DuLieu`, `MaLoiCoc? Loi`, `MaLoiDatSan? LoiDatSan`, `string? TruongLoi`. Factory/constructor bảo đảm thành công có dữ liệu và không có lỗi; thất bại không có dữ liệu và có `Loi`; `LoiDatSan` chỉ có khi `Loi = DieuKienDonKhongHopLe`. Không quyết định theo chuỗi thông báo tiếng Việt.

`TrangThaiCocHienThi` là giá trị **suy ra khi đọc**, không thay enum đơn: `KhongApDung=0`, `ChuaNhan=1`, `ChoDoiSoat=2`, `DaNhan=3`, `CanXuLyNgoaiLe=4`, `KhongSanSang=5`. Ưu tiên: biên nhận đã nhận → ngoại lệ → giao dịch chờ → không áp dụng → cấu hình tắt → chưa nhận. Đơn hủy/quá giờ có `CoTheChuyen=false` kể cả nhãn chưa nhận; UI giải thích theo trạng thái/thời gian. Đơn cũ có `TienCoc>0` hiển thị đã ghi nhận, `BienNhan=null`, không dựng biên nhận lịch sử giả.

`CoTheChuyen/CoTheXacNhan` chỉ phục vụ UI; service luôn kiểm tra lại khi POST. Sau khi có giao dịch khớp nội dung chờ đối soát, ẩn lời mời chuyển thêm. QR mô phỏng chỉ `DEMO:ORDER:{id}`, không URL thanh toán thật.

M3 đọc dòng dịch vụ của chính đơn qua DTO chi tiết cá nhân bổ sung `IReadOnlyList<DongDichVuDaDatDto>` với mã dòng, tên dịch vụ, số lượng, giá chụp, thành tiền, trạng thái. M4/M5 cập nhật query chung. Không gọi endpoint quản lý M5 bằng quyền khách.

## 6. Quyền và route đích

| Route MVC dự kiến | Method | Quyền / service |
| --- | --- | --- |
| `DonDat/Coc/{id}` | GET | Khách hoạt động, hồ sơ hoạt động, đúng chủ đơn → `LayThongTinCocAsync`. |
| `QuanLyDonDat/DoiSoatCoc/{id}` | GET | Admin/Nhân viên hoạt động → `LayDoiSoatCocAsync`. |
| `QuanLyDonDat/XacNhanCoc` | POST + token | Admin/Nhân viên → `XacNhanCocVaDatSanAsync`. |
| `QuanLyDonDat/HoanThanh` | POST + token | Admin/Nhân viên → `HoanThanhVaTinhTienAsync`. |
| `GiaoDichMoPhong/Tao` | POST + token | Admin/Nhân viên **và** Development **và** bật cấu hình → `TaoGiaoDichMoPhongAsync`. |

Các route mới chưa tồn tại: không thêm menu/link trước khi có action. Trang nhạy cảm no-store. Không dùng GET tạo giao dịch, ghi cọc hoặc chuyển trạng thái. Cookie/Session tiếp tục cơ chế M1; không tạo hệ thống xác thực mới.

## 7. Mã lỗi ổn định và phản hồi

Enum **mới** `MaLoiCoc`, không sửa giá trị số `MaLoiDatSan` đang có:

| Giá trị / tên | Điều kiện | Controller/UI |
| --- | --- | --- |
| 1 `DuLieuKhongHopLe` | ID, Base64, số tiền hoặc chuỗi sai định dạng | Lỗi form theo trường; không gọi ghi DB. |
| 2 `KhongDuQuyen` | Chưa đủ vai trò/tài khoản hoặc hồ sơ bị khóa | Challenge nếu chưa đăng nhập; còn lại Forbid. |
| 3 `KhongTonTai` | Đơn không có hoặc không thuộc khách | 404, không phân biệt hai trường hợp. |
| 4 `ChuyenKhoanChuaSanSang` | Chế độ tắt hoặc nguồn thật chưa hỗ trợ | Trang thông báo, không hiện QR. |
| 5 `MoPhongKhongDuocPhep` | Không Development hoặc cấu hình mô phỏng tắt | 404 cho endpoint mô phỏng, không ghi dữ liệu. |
| 6 `KhongApDungCoc` | Chính sách 0 hoặc cọc yêu cầu bằng 0 | Không tạo biên nhận; hướng tới xác nhận thông thường theo quyền. |
| 7 `YeuCauDoiSoatCoc` | Cố xác nhận thường cho đơn phải cọc | Dẫn nhân viên tới đối soát. |
| 8 `GiaoDichKhongTonTai` | Mã giao dịch server không có | Báo kiểm tra lại, không lưu. |
| 9 `GiaoDichKhongKhop` | Sai nội dung/nguồn/tài khoản nhận | Không xác nhận; giữ dấu vết để xử lý. |
| 10 `SoTienKhongKhop` | Tiền thực nhận khác cọc yêu cầu | Không chấp nhận thiếu/thừa, không tự cộng dồn. |
| 11 `ThoiDiemNhanKhongHopLe` | Trước tạo đơn, tương lai, hoặc từ giờ bắt đầu trở đi | Ngoại lệ; không xác nhận đặt sân. |
| 12 `GiaoDichDaSuDung` | Giao dịch đã gắn đơn khác | Không ghi lần hai. |
| 13 `DonDaNhanCoc` | Đơn đã có cọc nhưng dùng giao dịch khác để xác nhận lại | Không cộng tiền; hiển thị biên nhận cũ. |
| 14 `ChuaKiemTraGiaoDich` | Checkbox đối soát chưa xác nhận | Lỗi form; checkbox không thay kiểm tra dữ liệu server. |
| 15 `PhienBanDaCu` | Phiên bản không khớp hoặc DbUpdateConcurrencyException | Nạp lại dữ liệu, yêu cầu đối chiếu lại; POST lỗi trả View, API nếu có trả 409. |
| 16 `DieuKienDonKhongHopLe` | Trạng thái/giờ/sân/lịch/thu đủ không hợp lệ | Kèm mã `MaLoiDatSan` hiện có, ánh xạ đầy đủ bằng bảng lỗi MVC hiện hành. |
| 17 `DuLieuTienKhongHopLe` | Tiền âm, overflow, cọc > tổng hoặc dữ liệu chốt mâu thuẫn | Không lưu, báo cần kiểm tra dữ liệu; không tự sửa tiền. |
| 18 `GiaoDichChoDoiSoat` | Hủy/từ chối khi còn tiền chờ xử lý | Chặn và yêu cầu nhân viên đối soát. |
| 19 `TrungMaGiaoDich` | Tạo giao dịch mô phỏng trùng khóa ngoài | Không tạo thêm; kiểm tra bản ghi đã có. |

Các thao tác cũ trả `KetQuaXuLyDon`: thêm cuối enum `MaLoiDatSan` các mã `YeuCauDoiSoatCoc=101`, `GiaoDichChoDoiSoat=102`, `KhongDuQuyen=103` để chặn đường cũ. M4 cập nhật `ThongBaoLoiDatSanViewModel` và test ánh xạ cùng PR. Giữ nguyên các mã 1–32, 100.

Lỗi hệ thống ngoài dự kiến ghi log nội bộ và qua handler chung; không đổi thành thành công/lỗi “trùng” chung chung, không trả SQL/connection string. `CancellationToken` phải truyền xuyên suốt và hủy transaction nếu chưa commit.

## 8. Giao dịch, biên nhận và tính nguyên tử

**Dữ liệu đích cần migration riêng do M1 tích hợp:**

- `DatSan`: thêm `PhienBanChinhSachCoc` và `TienCocYeuCau`; giữ nguyên `TienCoc` là tiền đã nhận, không dùng làm tiền yêu cầu.
- `GiaoDichCoc`: mã nội bộ, nguồn/tài khoản nhận/mã giao dịch ngoài chuẩn hóa, nội dung, tiền, ngày nhận, ngày ghi/người ghi, trạng thái `ChuaDoiSoat=0 / DaGanBienNhan=1 / CanXuLyNgoaiLe=2`, mã lỗi ngoại lệ nếu có, RowVersion. Giao dịch là bất biến về số tiền/nội dung sau ghi. Unique `(Nguon, TaiKhoanNhan, MaGiaoDichNgoai)`; không cascade xóa.
- `BienNhanCoc`: mã, đơn, giao dịch, số tiền, ngày/người xác nhận và snapshot thông tin nhận tiền cần thiết. Unique `MaDatSan` và unique `MaGiaoDich`; FK Restrict, số tiền > 0. Các giới hạn tiền/chuỗi của DTO áp dụng cả mapping.
- Hai bảng tách biệt: có tiền chờ/ngoại lệ không có nghĩa đã nhận đơn. Khách không xem danh sách giao dịch của người khác. Chưa có thao tác sửa/xóa biên nhận hoặc hoàn tiền tự động.

**Xác nhận cọc (M4 là chủ transaction):**

1. Xác thực, kiểm tra hình dạng input → mở transaction → xác định sân của đơn → khóa sân bằng cơ chế hiện có.
2. Nạp mới đơn, tài khoản/hồ sơ, giao dịch và biên nhận dưới khóa; kiểm tra lại quyền. Không dùng entity đã theo dõi cũ làm nguồn quyết định.
3. Nếu đúng cặp đơn/giao dịch đã có biên nhận hợp lệ: trả kết quả cũ với `DaXuLyTruocDo=true`, không ghi lại (cho phép request retry mang RowVersion cũ). Nếu giao dịch/đơn gắn cặp khác, trả mã 12/13. Không bỏ kiểm tra quyền khi retry.
4. Kiểm tra RowVersion đơn, chính sách, checkbox, giao dịch, số tiền và thời gian. Tái sử dụng quy tắc khả dụng của M4 với **bỏ qua chính đơn này**; không gọi API tra cứu công khai mà tự coi đơn hiện tại là trùng.
5. Ghi biên nhận, gán `TienCoc`, chuyển `ChoXuLy → DangXuLy`, ghi `NgayXacNhan` và đánh dấu giao dịch đã gắn → SaveChanges/commit một lần.
6. Lỗi: rollback, giải phóng/detach entity thất bại. Unique index bảo vệ trường hợp hai sân khác nhau cùng dùng một giao dịch; bắt lỗi ràng buộc xác định, tải lại để phân biệt replay hợp lệ với dùng trùng.

Giao dịch mô phỏng được tạo trong thao tác riêng **trước** đối soát nên vẫn tồn tại khi xác nhận thất bại. Đánh dấu ngoại lệ bằng transaction riêng sau rollback, kiểm tra RowVersion/trạng thái chưa được dùng; không ghi đè giao dịch đã được người khác xác nhận. Không đánh dấu ngoại lệ chỉ vì form sai, phiên bản cũ hay thiếu quyền.

Mọi thao tác ghi cùng đơn (cọc, hủy/từ chối, dịch vụ, chốt tiền) dùng thứ tự khóa **sân → đơn/giao dịch**, cùng DbContext scoped; không gọi song song. Nguồn tạo giao dịch mô phỏng khớp đơn cũng lấy khóa sân trước khi lưu để phối hợp với hủy. Không giữ khóa DB trong lúc người dùng quét QR/nhập form.

## 9. Dịch vụ trong đơn và chốt tiền — điểm nối M5/M4

Chữ ký đích M5 (thay đồng thời mọi caller):

```csharp
Task<ChiTietDichVuTrongDon?> LayTheoDonAsync(int maDatSan, CancellationToken ct);
Task<KetQuaNghiepVu> ThemAsync(int maDatSan, int maDichVu, int soLuong,
    byte[] phienBan, CancellationToken ct);
Task<KetQuaNghiepVu> SuaSoLuongAsync(int maDatSan, int maChiTiet, int soLuong,
    byte[] phienBan, CancellationToken ct);
Task<KetQuaNghiepVu> HuyAsync(int maDatSan, int maChiTiet,
    byte[] phienBan, CancellationToken ct);
```

`ChiTietDichVuTrongDon` giữ dữ liệu hiển thị hiện có, thêm `byte[] PhienBan`; thay `List<DichVu>` bằng `IReadOnlyList<DichVuLuaChonDto>` (`int MaDichVu`, `string TenDichVu, DonViTinh`, `decimal DonGia`). `DongDichVuTrongDon` bổ sung trạng thái dòng; tổng chỉ cộng dòng HieuLuc. Đầu đọc quản lý giữ chữ ký nullable: null chỉ khi đơn không tồn tại; thiếu quyền/tài khoản khóa thì service ném `UnauthorizedAccessException`, controller bắt đúng exception này và trả `Forbid()`. Authorize vẫn chặn trước; không để handler mặc định đổi lỗi quyền thành 500 hoặc trả danh sách rỗng.

- Phần mở rộng kết quả M5: thêm cuối record `KetQuaNghiepVu` một tham số tùy chọn `MaLoiDichVuTrongDon? MaLoi = null` để không phá caller M1/danh mục dịch vụ đang dùng. Enum mới cố định `KhongDuQuyen=1`, `KhongTonTai=2`, `SaiTrangThai=3`, `SoLuongKhongHopLe=4`, `DichVuKhongHoatDong=5`, `PhienBanDaCu=6`, `DuLieuTienKhongHopLe=7`. Nhánh lỗi dịch vụ trong đơn luôn gán mã; thành công không có mã. M5 cập nhật factory/caller của phần mình trong cùng PR, không buộc các module khác đoán lỗi từ chuỗi.

- M5 giữ tên `ThemAsync`, `SuaSoLuongAsync`, `HuyAsync`, nhưng bổ sung `byte[] phienBan` trước `CancellationToken` cho ba phương thức ghi; cập nhật controller/form/test cùng PR. Kết quả đọc bổ sung RowVersion đơn để form mang lại. Quyền actor lấy từ phiên; service kiểm tra lại role/trạng thái, mã chi tiết phải thuộc đúng đơn.
- Chỉ sửa `DangXuLy`; giữ số lượng 1–100, giá snapshot. Cùng dịch vụ còn hiệu lực thì cộng số lượng và giữ giá chụp. Hủy đặt trạng thái `DaHuy`, không xóa cứng. Thêm lại sau hủy tạo dòng mới với giá hiện tại; không làm sống lại giá cũ. M1/M5 thống nhất migration thêm `TrangThai` (0 HieuLuc, 1 DaHuy), thời điểm hủy và unique có điều kiện cho dòng còn hiệu lực; dòng hiện có migrate HieuLuc.
- Khi sửa dịch vụ: khóa sân, kiểm tra RowVersion mới nhất, nạp dòng → dùng M5 tính lại → kiểm tra tổng >= cọc → cập nhật `TienDichVu` và RowVersion đơn cùng transaction. Không thay `TienSan/TongTien` đã chốt. `KetQuaNghiepVu` hiện tại giữ tương thích; bổ sung thuộc tính mã lỗi tùy chọn (không phân tích chuỗi `Loi`) với các mã `KhongDuQuyen`, `KhongTonTai`, `SaiTrangThai`, `SoLuongKhongHopLe`, `DichVuKhongHoatDong`, `PhienBanDaCu`, `DuLieuTienKhongHopLe`. M5 ánh xạ UI trong cùng PR.
- `HoanThanhVaTinhTienAsync`: M4 khóa sân/đơn, kiểm tra quyền, phiên bản, `DangXuLy`, đã đến giờ kết thúc và `DaThuDu`; tải dòng HieuLuc từ DB → gọi M5 tính tiền → ghi `TienSan`, `TienDichVu`, `TongTien`, `NgayHoanThanh`, trạng thái `HoanThanh` cùng transaction. `ConPhaiThu` trong DTO là số phải thu của lần chốt (tổng trừ cọc), **không phải công nợ sau khi đã xác nhận thu đủ**; UI ghi “Đã thu phần còn lại” khi hoàn thành.
- Không gọi public `HoanThanhDonAsync` mở thêm transaction bên trong. Helper áp dụng trạng thái được dùng lại, service tính tiền không gọi ngược `IDatSanService`. Dependency: controller → service điều phối → công thức M5/DB; không có vòng DI.

## 10. Bàn giao và nghiệm thu theo hợp đồng

| Chủ trì | File/phần sửa chính | Người review |
| --- | --- | --- |
| M4 — Nghĩa | `IDatSanService`, `HopDongDatSan`, DTO cọc mới, partial cọc/chốt tiền, controller quản lý; nguồn mô phỏng và bảng giao dịch/biên nhận | M3 dữ liệu khách; M5 tiền; M1 quyền/schema |
| M5 — Quang | `ITinhTienService`/triển khai/test; dịch vụ trong đơn, DTO lựa chọn, trạng thái dòng và phối hợp khóa | M4 transaction; M1 schema |
| M3 — Quý | `DonDatController`, ViewModel/View cọc/lịch sử/hủy; không tự sửa quy tắc tiền | M4 ownership/trạng thái |
| M1 — Tùng | DbContext/migration/snapshot, cấu hình/DI chung, tài liệu và điều phối PR | M4/M5 |

Thứ tự PR: (1) DTO/interface và công thức M5, bảo đảm mọi implementation/test build; (2) dữ liệu/migration và nghiệp vụ M4/M5; (3) UI M3/M4 + kích hoạt tạo đơn chính sách 1; (4) kiểm thử toàn luồng. Không bật luồng mới nếu thiếu bất kỳ chặng ghi/đọc/đối soát. Không tạo stub controller hoặc báo thành công giả chỉ để build.

Các ca bắt buộc: 50% và làm tròn; giá danh mục thay đổi không đổi đơn cũ; khách đổi ID; tài khoản khóa; POST thiếu token; nguồn mô phỏng ngoài Development; xác nhận đúng/thiếu/thừa/sai nội dung/sai thời gian; cùng cặp retry; cùng giao dịch cho hai đơn khác sân; hai nhân viên cùng xác nhận; xác nhận cạnh tranh hủy; dịch vụ cạnh tranh chốt tiền; lỗi sau SaveChanges trước commit; đơn miễn phí/chính sách 0; đường xác nhận cũ chặn đơn chính sách 1; không tính doanh thu/cọc hai lần; migration không mất lịch sử.

Kết quả test trên main hiện tại chỉ chứng minh code hiện có; không nghiệm thu các hợp đồng mới chưa triển khai. Không cập nhật/đóng Issue hoặc gửi thông báo cho thành viên thay mặt người dùng trong lần chốt tài liệu này.

## 11. Kiểm tra sau cập nhật main ngày 23/10/2026

Các kết quả build/test dưới đây được chạy trên `main` tại `1a97883` kèm các thay đổi M1 chưa commit đang có ở local. Chúng là thông tin kiểm tra môi trường tại thời điểm cập nhật, không phải kết quả chạy trên một checkout sạch của PR tài liệu hoặc bằng chứng nghiệm thu luồng cọc mới.

- `main` fast-forward từ `ce5de11` đến `1a97883`; xác minh SHA-256 toàn bộ 30 file local trước pull không đổi sau pull, không stash/reset/commit.
- Restore package cho project test M5 mới; không sửa phiên bản package. Build Release toàn solution: **0 lỗi, 0 cảnh báo**.
- Test hiện hành: **360/360 đạt**, không bỏ qua: M103 75, M104 24, M201 115, M4 141, M5 5. SQL Server tests dùng database test tạm riêng, không database ứng dụng.
- `dotnet ef migrations has-pending-model-changes`: không có chênh lệch model. Không chạy Update-Database hoặc tạo migration trong đầu việc tài liệu này.
- Bằng chứng local: `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/m345-test.log` và `obj/m345-tests-20261023/*.trx`. Bộ test này chưa kiểm tra các DTO/phương thức cọc dự kiến; phải bổ sung test khi triển khai.

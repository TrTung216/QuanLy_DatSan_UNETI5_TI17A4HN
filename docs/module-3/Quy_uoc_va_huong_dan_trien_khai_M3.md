# M3 — Quy ước và hướng dẫn hoàn thiện chức năng khách hàng

**Phiên bản:** 1.0 — 02/11/2026.

**Người thực hiện M3:** Nguyễn Văn Quý — MSSV **23103100181**.

**Điều phối và bàn giao:** Trần Trọng Tùng — M1, MSSV **23103100202**. Codex hỗ trợ đối chiếu code và biên soạn.

Tài liệu hướng dẫn phần việc tiếp theo của M3, không phải biên bản nghiệm thu. Căn cứ code `main@e6a17e7` sau PR #63 và các issue #17–#20. Không ghi nhận phần service dùng chung của M4/M5 hoặc phần tài khoản của M1 thành đóng góp mới của M3.

Đọc cùng các nguồn sau; tài liệu này không thay hợp đồng tiền/cọc hoặc tự cấp thêm quyền:

- [Quy ước Controller–Service–Repository](../Quy_uoc_Controller_Service_Repository.md).
- [Hợp đồng M3/M4/M5](../Hop_dong_M3_M4_M5.md), [quy ước cọc](../Quy_uoc_dat_coc_chuyen_khoan.md), [phân công chuyển khoản/QR](../Phan_cong_chuyen_khoan_QR.md).
- [Hướng dẫn layout](../Huong_dan_su_dung_layout.md) và [mẫu PR](<../../.github/PULL_REQUEST_TEMPLATE (1).md>).

## 1. Hiện trạng và việc cần bàn giao theo issue

| Issue GitHub | Đã có trong code | Phần M3 cần hoàn thiện |
| --- | --- | --- |
| [#17 — M3-01: Quản lý khách hàng và hồ sơ](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/17) | Entity/quan hệ tài khoản–khách; đăng ký kèm hồ sơ; đọc/sửa hồ sơ theo tài khoản hiện tại; đồng bộ họ tên/email | Validation đầy đủ trong service; kiểm thử quyền sở hữu và tính nguyên tử; màn hình quản lý khách hàng theo ma trận quyền thống nhất. Quản lý tài khoản M1 không thay thế toàn bộ yêu cầu quản lý khách hàng M3. |
| [#18 — M3-02: Tạo đơn](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/18) | Form/validation; `TaoDonAsync`; giá và khách lấy từ server; transaction và khóa sân của service chung | Kiểm thử HTTP và tích hợp, bổ sung bằng chứng nghiệm thu. Không tạo lại service đặt sân hoặc cơ chế khóa lịch. |
| [#19 — M3-03: Lịch sử và chi tiết](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/19) | Danh sách, lọc trạng thái, phân trang; chi tiết có kiểm chủ sở hữu; thông tin sân/giờ/trạng thái và một số khoản tiền | Danh sách dịch vụ của đơn; trình bày đầy đủ tiền/kết quả thanh toán qua dữ liệu chung; kiểm thử quyền đọc và sự nhất quán tiền. |
| [#20 — M3-04: Khách hủy đơn chờ](https://github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN/issues/20) | M4 có `KhachHangHuyDonAsync`, kiểm chủ sở hữu, trạng thái, giờ, cọc và phiên bản | Action POST, form/lý do hủy, đưa phiên bản xuống ViewModel, ánh xạ lỗi, trạng thái menu và kiểm thử HTTP. |

Các issue trên vẫn mở ở lần kiểm tra 02/11/2026. Có code không đồng nghĩa đã đạt mọi điều kiện nghiệm thu. #18 gần hoàn thiện về phần triển khai; cần xác minh trên phiên bản build được trước khi đề nghị đóng.

### Điều kiện nền trước khi bắt đầu

- `main@e6a17e7` có lỗi Razor trong `_HeaderKhachHang.cshtml` và đường dẫn project test M5 sai trong solution.
- Header đã được sửa **local** trên `feature/m1-sua-header-khach-hang`: build riêng ứng dụng đạt, 17/17 test menu đạt; còn hai cảnh báo biến không dùng ở `Views/ThongKe/Index.cshtml`. Chưa coi bản sửa này đã có trên GitHub/main.
- Lỗi đường dẫn test M5 vẫn cần người phụ trách phối hợp sửa. Không xóa project/test khỏi solution chỉ để báo build đạt.
- M3 lấy bản sửa tích hợp sau khi được merge, hoặc ghi rõ phụ thuộc trong PR. Không chép cả phần ảnh M1 đang làm vào nhánh M3.

## 2. Phạm vi sửa và phối hợp

Đường dẫn code dưới đây tính từ `QuanLyDatSan_UNETI5_DHTI17A4HN/`.

| Phạm vi | M3 thực hiện | Khi cần phối hợp |
| --- | --- | --- |
| Controller khách | `KhachHangController`, `HoSoController`, `DonDatController` | Giữ phần tích hợp service và ghi nhận tác giả M4 trong file |
| Hồ sơ/đăng ký | `Services/QuanLyKhachHang/*`, `Repositories/QuanLyKhachHang/*`, ViewModel tương ứng | Trường họ tên/email và tài khoản liên kết trao đổi M1 |
| Giao diện riêng | `Views/KhachHang/*`, `Views/HoSo/*`, `Views/DonDat/*`, CSS/JS riêng có phạm vi rõ | Không chèn header/menu/footer riêng vào từng View |
| Đơn đặt và trạng thái | M3 gọi `IDatSanService` | M4 chủ trì interface, DTO, `DatSanService`, repository đặt sân và khóa/transaction |
| Dịch vụ, tiền, cọc | M3 trình bày DTO đã kiểm quyền | M5 công thức/dữ liệu dịch vụ; M4 điều phối đơn, quyền sở hữu và cọc |
| File dùng chung | Nêu chính xác thay đổi cần thiết trong PR | Tùng review layout/menu, `Program.cs`, DI, Entity, DbContext, migration/snapshot; không sửa hàng loạt ngoài task |

M3 có thể bổ sung test vào project phù hợp hiện có hoặc tạo project test M3 nếu cần. Nếu tạo mới, thêm đúng đường dẫn vào solution và kiểm tra build từ checkout sạch.

## 3. Quy ước kỹ thuật bắt buộc

### 3.1. Controller, Service, Repository và Unit of Work

```text
Form / URL → Controller → Service → Repository → DbContext
                              └→ IDonViCongViec → lưu / transaction
View       ← ViewModel ← DTO / kết quả nghiệp vụ
```

1. Controller nhận ViewModel, kiểm `ModelState`, gọi interface service, ánh xạ lỗi và redirect/View. Không inject repository/DbContext, không tính tiền hoặc đổi `DatSan.TrangThai` trực tiếp.
2. Service kiểm đầu vào nghiệp vụ, quyền sở hữu và trạng thái bản ghi; không phụ thuộc MVC, `TempData`, `ModelState` hoặc `IFormFile`.
3. Repository dùng LINQ/projection/tracking phù hợp; không trả `IQueryable` ra service, không gọi `SaveChanges`, không quyết định khách được hủy đơn hay không.
4. `IDonViCongViec` **đã có trong code hiện tại**. Hồ sơ và tài khoản được cập nhật trong cùng lần lưu. Không tự tạo Unit of Work thứ hai.
5. Với đặt/hủy đơn, gọi service điều phối của M4; không mở thêm transaction ở Controller hoặc gọi `KhoaSanAsync` rồi lưu riêng.
6. Truyền `CancellationToken` xuyên suốt. Chỉ ánh xạ lỗi dự kiến; không `catch (Exception)` rồi báo thành công hoặc lỗi validation chung.
7. Dùng tên hiện có, PascalCase cho kiểu/phương thức, camelCase cho biến; đầu file ghi đúng họ tên, MSSV và phần thực hiện. Giữ ghi nhận người đã viết trước.

### 3.2. Quyền và đầu vào không đáng tin

| Chức năng đang có | Quyền hiện tại |
| --- | --- |
| Đăng ký khách | Chưa đăng nhập; server tự gán vai trò Khách hàng |
| Hồ sơ/trang chủ cá nhân, tạo đơn, lịch sử/chi tiết cá nhân | `KhachHang`; chỉ dữ liệu gắn tài khoản hiện tại |
| Hủy đơn của khách sắp bổ sung | `KhachHang`; service kiểm đúng chủ đơn |
| Màn hình quản lý khách hàng sắp bổ sung | **Chưa có endpoint/ma trận quyền đầy đủ trong code M3 hiện tại**; lập bảng quyền đọc/sửa/khóa để Tùng xác nhận trước khi mở route/menu. Không suy ra Nhân viên được quản lý khách chỉ vì có layout quản trị. |

- Danh tính lấy từ `ITaiKhoanHienTai`, không lấy `MaTaiKhoan`, `MaKhachHang` hoặc role trong hidden input làm quyền truy cập.
- Route có `id` chỉ là yêu cầu tìm bản ghi, không phải bằng chứng sở hữu. Lọc theo chủ sở hữu ở truy vấn/service; đơn không thuộc khách trả 404 theo hành vi hiện tại.
- Mọi thao tác ghi dùng POST + `[ValidateAntiForgeryToken]`; không dùng GET để hủy/khóa/cập nhật.
- Không bind Entity. Điểm tích lũy, ngày đăng ký, trạng thái, quyền, cọc và giá không thuộc form hồ sơ khách.
- Giữ cơ chế đăng nhập, cookie/Session và thu hồi phiên của M1. Ẩn nút không thay kiểm tra phía server.

### 3.3. Giao diện và lỗi

- View dùng layout hiện có, đặt `ViewData["Title"]`; tài nguyên riêng qua `Styles`/`Scripts`.
- Dùng `HienThiDatSan.TenTrangThai`, `LopMau`, `Tien` và thành phần chung. Luôn có chữ trạng thái; không dựng một bảng màu/enum trạng thái khác.
- TempData thực tế: `Success`, `Error`, `Warning`, `Info`; không thêm `ThanhCong`/`ThatBai` mà layout không đọc.
- Khi form sai, nạp lại dữ liệu chỉ đọc/lựa chọn bằng service. Không đưa mật khẩu hoặc dữ liệu đơn người khác vào View lỗi.
- Menu khách lấy từ `MenuKhachHang.Lay(User)`; menu quản trị từ `MenuQuanTri`. Khi thêm action POST trả lại trang chi tiết, phối hợp M1 thêm action đó vào nhóm active. Không tạo liên kết GET tới action chỉ nhận POST.
- Form tiền chỉ hiển thị dữ liệu server. JavaScript được cải thiện trải nghiệm, không trở thành nguồn quyết định giá/cọc.

## 4. Hướng dẫn hoàn thiện M3-01 — hồ sơ và quản lý khách

### 4.1. Sửa validation ở service trước

`ThongTinKhachViewModel` đã kiểm nhiều trường ở biên HTTP, nhưng `KhachHangService.CapNhatHoSoAsync` hiện mới tự kiểm ngày sinh. Bổ sung kiểm tra trước khi gán entity hoặc gọi `.Trim()` trên dữ liệu chưa xác minh:

| Trường | Quy tắc tương thích code hiện có |
| --- | --- |
| Họ tên | Bắt buộc, không chỉ khoảng trắng, tối đa 100 ký tự |
| Email | Bắt buộc, tối đa 254 ký tự, đúng định dạng email |
| Điện thoại | Bắt buộc, tối đa 20 ký tự; cùng quy tắc `^(0|\+84)\d{9,10}$` của ViewModel/đăng ký |
| Ngày sinh | Có thể trống; nếu có thì từ năm 1900 đến `IDongHo.HomNay` |
| Giới tính | Giá trị thuộc enum `GioiTinh` |
| Địa chỉ | Có thể trống, tối đa 255 ký tự; chuỗi trắng lưu null |

Không tự thêm điều kiện email/điện thoại duy nhất khi schema và quy ước hiện có chưa yêu cầu. Thống nhất xử lý khoảng trắng giữa ViewModel và service, không để mỗi nơi chấp nhận một tập dữ liệu khác nhau.

Các bước:

1. Giữ nguyên `CapNhatHoSoDto` chỉ chứa trường khách được sửa.
2. Bổ sung mã lỗi cần thiết **ở cuối** `MaLoiHoSo`, không đổi giá trị các mã đang có; đây là thay đổi dự kiến, chưa có trong code.
3. Cập nhật `HoSoController` ánh xạ đầy đủ mã mới vào trường form và thông báo tiếng Việt trong cùng PR.
4. Nạp hồ sơ theo tài khoản hiện tại, kiểm trạng thái hồ sơ; đồng bộ `KhachHang.HoTen/Email` và `TaiKhoan.HoTen/Email` trong cùng `IDonViCongViec.LuuAsync`.
5. Test gọi trực tiếp service với dữ liệu sai, không chỉ gửi qua MVC. Kiểm tra khi lưu thất bại cả hai bản ghi không bị cập nhật một phần.

### 4.2. Quản lý khách hàng theo quyền

Tách khỏi màn hình tự sửa hồ sơ. Trước khi code, ghi rõ quyền xem danh sách/chi tiết, sửa trường nào và thay trạng thái nào; phân biệt trạng thái hồ sơ khách với trạng thái tài khoản đăng nhập.

- Dùng Controller/ViewModel riêng cho quản lý nếu cần; không biến `/HoSo/Sua?id=...` thành đường sửa khách bất kỳ.
- Tận dụng repository/service M3; tìm/lọc/sort/phân trang tại DB, thứ tự có khóa phụ ổn định.
- M3 không tự đổi vai trò, mật khẩu hoặc quyền đăng nhập; những việc này thuộc M1.
- Không xóa khách có lịch sử đặt sân hoặc cascade xóa đơn. Trường hợp đổi trạng thái phải giữ lịch sử và có quyền server phù hợp.
- Tránh tạo trùng hồ sơ đã được M1 tạo khi Admin cấp tài khoản khách. Không phát sinh thêm migration cho quan hệ tài khoản–khách vốn đã có chỉ để làm UI.

## 5. Hướng dẫn hoàn thiện M3-04 — khách hủy đơn

Đây là phần có thể triển khai sớm sau validation vì backend chung đã có.

**Chữ ký hiện tại**, namespace `QuanLyDatSan_UNETI5_DHTI17A4HN.Services`:

```csharp
Task<KetQuaXuLyDon> KhachHangHuyDonAsync(
    HuyDonYeuCau yeuCau, int maTaiKhoan, CancellationToken cancellationToken);

public sealed record HuyDonYeuCau(int MaDatSan, string? LyDo, byte[] PhienBan);
```

`ChiTietDonDto.PhienBan` đã có, repository đã lấy từ `DatSan.RowVersion`. Không cần migration mới để truyền phiên bản xuống form.

### Các bước triển khai

1. Thêm ViewModel hủy riêng: mã đơn, lý do và phiên bản dạng Base64. Tên file mới do M3 đặt theo quy ước, không coi ví dụ này là file đã tồn tại.
2. Khi đọc chi tiết qua `LayChiTietCaNhanAsync`, ánh xạ `don.PhienBan` sang `Convert.ToBase64String(...)`; không lấy phiên bản mới thay cho phiên bản người dùng đã xem khi POST.
3. Thêm form POST ở chi tiết đơn hoặc trang xác nhận riêng. Lý do bắt buộc, sau chuẩn hóa dài tối đa 1000; hidden phiên bản không phải thông tin cấp quyền.
4. Controller lấy `MaTaiKhoan` từ `ITaiKhoanHienTai`; kiểm định dạng Base64 và độ dài 8 byte. Base64 sai phải trả lỗi form/400, không gây 500.
5. Gọi `KhachHangHuyDonAsync(new HuyDonYeuCau(id, lyDo, phienBan), maTaiKhoan, ct)`; không gán trạng thái hoặc ngày hủy trong Controller.
6. Thành công: `TempData["Success"]`, redirect về chi tiết hoặc lịch sử. Lỗi: ánh xạ theo bảng dưới, nạp lại thông tin qua service có kiểm chủ sở hữu.
7. Với phiên bản cũ: yêu cầu tải lại/đối chiếu rồi người dùng chủ động gửi lại. Không tự retry thao tác ghi với phiên bản mới.
8. Phối hợp M1 giữ menu “Lịch của tôi” active khi action hủy trả lại View lỗi.

```text
Khách xem đơn của mình → nhập lý do → POST + antiforgery + phiên bản
→ Controller lấy danh tính phiên → service M4 kiểm chủ đơn/phiên bản/điều kiện
→ transaction + khóa sân → đổi trạng thái và ghi lý do/thời điểm → commit
→ redirect + thông báo → khách xem lại lịch sử
```

### Điều kiện và lỗi hiện tại

Service chỉ cho khách hủy đơn `ChoXuLy`, trước giờ bắt đầu và chưa có tiền cọc đã nhận. Đây là quy tắc code hiện hành; khi tích hợp cọc mới phải bổ sung chặn giao dịch chờ đối soát qua M4, không tự suy ra từ chữ trên giao diện.

| Mã lỗi hiện có | Xử lý ở giao diện khách |
| --- | --- |
| `KhongTonTai` | 404, không tiết lộ đơn thuộc ai |
| `MaDonKhongHopLe`, `PhienBanKhongHopLe` | Yêu cầu không hợp lệ; không ghi dữ liệu |
| `LyDoHuyKhongHopLe` | Lỗi tại ô lý do |
| `KhachChiHuyDonCho` | Đơn không còn chờ xác nhận; không cho gửi tiếp như đơn cũ |
| `QuaGioHuy` | Đã đến/quá giờ bắt đầu |
| `DaNhanCoc` | Không hủy tự động; hướng dẫn liên hệ nhân viên xử lý |
| `PhienBanDaCu`, `XungDotDuLieu`, `SanDaThayDoi` | Thông tin đã thay đổi; tải lại và đối chiếu |
| `TaiKhoanKhongHopLe` | Yêu cầu đăng nhập lại theo cơ chế hiện có; không lấy tài khoản thay thế từ form |

Kiểm thử cả POST trực tiếp: ẩn nút theo điều kiện chỉ hỗ trợ trải nghiệm. Giờ dùng `IDongHo` hiện có, không dùng giờ máy khách làm căn cứ hủy.

## 6. Hướng dẫn hoàn thiện M3-02 — tạo đơn

Giữ luồng hiện tại:

```text
GET Tao → LayLuaChonSanAsync → form
POST Tao → ModelState → TaoDonAsync → service đọc khách/giá, kiểm lịch dưới khóa
→ thành công về ChiTiet; thất bại nạp lại lựa chọn và hiển thị lỗi
```

- Form chỉ gửi sân, ngày, giờ bắt đầu/kết thúc; mã tài khoản do Controller lấy từ phiên. Giá, tiền cọc, mã khách và trạng thái thêm lén vào request phải bị bỏ qua.
- `TaoDonAsync` là đầu mối kiểm tra sân và loại sân hoạt động, bảo trì, giờ mở cửa, thời gian tương lai và trùng lịch. Không thay bằng kiểm tra AJAX rồi lưu trực tiếp.
- Tra cứu sân trống chỉ là thông tin tại lúc xem, không giữ chỗ. Khi gửi đơn vẫn kiểm tra lại dưới khóa.
- Giá đơn là snapshot từ server. Phần giá cao điểm/cuối tuần chưa trở thành hợp đồng tính đơn chính thức thì phải ghi đúng là tham khảo; không hiển thị dự kiến như số tiền đã chốt.
- Dùng các test đã có của M4 làm bằng chứng tích hợp khi chạy lại thành công; ghi nhận đúng tác giả và chỉ bổ sung ca còn thiếu, không sao chép lại cả bộ test.

## 7. Hướng dẫn hoàn thiện M3-03 — lịch sử, dịch vụ và tiền

1. Giữ `LayDanhSachCaNhanAsync`/`LayChiTietCaNhanAsync` và bộ lọc theo tài khoản hiện tại. Phân trang phải giữ bộ lọc và không tải toàn bảng vào bộ nhớ.
2. Phối hợp M4/M5 bổ sung DTO đọc cho khách: các dòng dịch vụ, số lượng, đơn giá snapshot, thành tiền, trạng thái hiệu lực/hủy theo backend đã triển khai. Đây là phần bổ sung, không giả định `ChiTietDonDto` hiện đã chứa danh sách này.
3. Chỉ đọc dịch vụ của đơn đã xác minh quyền sở hữu; không gọi endpoint quản trị rồi bỏ kiểm tra role cho khách dùng.
4. Nhận tiền sân/dịch vụ/tổng/cọc/còn phải thu từ nguồn chung. View định dạng và hiển thị; không tự tạo công thức tổng/cọc hoặc dùng giá danh mục hiện tại thay giá đã chụp.
5. Đơn hoàn thành hiển thị các khoản đã chốt. Đơn chưa chốt có nhãn “dự kiến”. Không đổi null thành 0 để che việc chưa có dữ liệu thanh toán.
6. Không cộng cọc vào tổng lần nữa; không suy ra “đã thanh toán đủ” chỉ vì `TienCoc > 0` hoặc có `TongTien`.
7. Kiểm thử thay đổi giá sân/dịch vụ sau khi đặt không làm đổi số tiền lịch sử đã chốt; số tiền trên màn hình khách và quản lý phải cùng nguồn.

## 8. Cọc 50% và QR mô phỏng — giai đoạn phụ thuộc M4/M5

Phần này có hợp đồng thiết kế nhưng **chưa có đủ backend trên main đã đối chiếu**. `LayThongTinCocAsync`, `TienCocYeuCau`, chính sách cọc và biên nhận không được coi là đã triển khai. Không thêm link/action giả hoặc tự gọi phương thức chưa tồn tại để hoàn thiện giao diện.

M3 làm khi hợp đồng và backend tương ứng đã tích hợp:

- Sau tạo đơn, dẫn đến thông tin cọc của chính đơn đó; có đường quay lại từ lịch sử/chi tiết.
- Hiển thị tiền cọc **50% tiền sân theo kết quả server**, nội dung chuyển khoản, hướng dẫn và trạng thái cọc tách biệt trạng thái đặt sân.
- QR minh họa/local có nhãn mô phỏng; không dùng tài khoản ngân hàng giả như đích chuyển tiền thật.
- Giao dịch mô phỏng chỉ hoạt động khi đồng thời Development và cấu hình bật, theo backend M4. M3 không tự cấp quyền tạo giao dịch hoặc xác nhận cọc cho khách.
- Khách không có nút tự xác nhận đã thanh toán. Nhân viên/Admin đối soát rồi xác nhận cọc và đặt sân qua service M4.
- Kiểm tra quyền sở hữu khi đọc đơn/biên nhận; có trạng thái chưa sẵn sàng, chưa nhận cọc, chờ đối soát và kết quả thực tế từ server.
- Khi hủy gặp giao dịch chờ đối soát, dùng mã lỗi M4 theo hợp đồng mới; không thêm ngoại lệ bỏ qua cọc ở Controller.

Không tự bật luồng cọc mới nếu thiếu dữ liệu/transaction/đối soát. M1 điều phối migration, M4 trạng thái/ownership, M5 tiền; M3 chịu trách nhiệm UI và tích hợp phía khách.

## 9. Thứ tự PR và kế hoạch làm việc

Ước lượng theo buổi làm việc, điều chỉnh theo tiến độ phụ thuộc; không phải cam kết ngày hoàn tất.

| Thứ tự | Nhánh đề xuất | Đầu ra | Ước lượng |
| --- | --- | --- | --- |
| 0 | Nhánh sửa tích hợp của người phụ trách | Main build được, có bản sửa header và solution | Trước khi nghiệm thu M3 |
| 1 | `feature/m3-validation-ho-so` | Validation service, ánh xạ lỗi, test hồ sơ | 1 buổi |
| 2 | `feature/m3-huy-don-cho` | Form/action hủy, phiên bản, test HTTP và cạnh tranh | 1–2 buổi |
| 3 | `feature/m3-chi-tiet-don-dich-vu` | Dịch vụ/tiền/kết quả thanh toán của đúng khách | 1–2 buổi, cần M4/M5 |
| 4 | `feature/m3-quan-ly-khach-hang` | Ma trận quyền được thống nhất, màn hình quản lý và test | 2–3 buổi tùy phạm vi |
| 5 | Nhánh nghiệm thu phù hợp | Bằng chứng #18, rà lại #17–#20, cập nhật docs | 1 buổi |
| 6 | `feature/m3-giao-dien-coc` | UI cọc/QR mô phỏng và kiểm thử toàn luồng | Sau backend M4/M5 |

Phần test tạo đơn có thể chuẩn bị song song lúc chờ DTO dịch vụ/cọc; không cần viết lại chức năng đã có để tạo PR mới. Nếu chỉ bổ sung test/docs thì mô tả đúng phạm vi.

## 10. Checklist kiểm thử trước review

Các ô dưới đây là **việc phải thực hiện**, chưa đánh dấu đã đạt bởi tài liệu này.

- [ ] Đăng ký tạo đủ tài khoản và hồ sơ; role/trạng thái/điểm gửi lén không được áp dụng.
- [ ] Hồ sơ: rỗng/khoảng trắng, email/điện thoại sai, quá dài, enum lạ, ngày sinh sai bị chặn ở cả HTTP và service.
- [ ] Sửa hồ sơ đồng bộ họ tên/email hai bảng; lỗi lưu không gây cập nhật một phần; hồ sơ bị khóa không sửa được.
- [ ] Hai khách A/B: A không đọc/sửa hồ sơ hoặc đọc/hủy đơn của B bằng URL hay POST tự tạo.
- [ ] Khách chưa đăng nhập, Admin/Nhân viên và tài khoản bị khóa không dùng được endpoint dành riêng khách.
- [ ] Tạo đơn hợp lệ; chặn sân/loại ngừng hoạt động, bảo trì, giờ sai, quá khứ, trùng một phần/toàn phần; hai khung liền kề được phép.
- [ ] Hai yêu cầu đồng thời đặt cùng sân/giờ chỉ tạo một đơn; tra cứu trước không thay kiểm tra lúc ghi.
- [ ] Hủy đơn chờ trước giờ thành công; lưu lý do/thời điểm; chặn trạng thái khác, đã đến giờ, đã nhận cọc, sai chủ đơn.
- [ ] Hủy với Base64 hỏng/phiên bản sai, thiếu CSRF, gửi lặp hoặc cạnh tranh với nhân viên xác nhận không ghi đè im lặng.
- [ ] Dịch vụ và tiền khớp dữ liệu chung; giá danh mục mới không thay lịch sử; chưa chốt và đã chốt có nhãn đúng.
- [ ] Bộ lọc/phân trang giữ tham số; trạng thái rỗng rõ; View lỗi giữ dữ liệu form cần thiết và menu active.
- [ ] Kiểm tra desktop 1440 px, mobile 390/320 px: không tràn ngang, dùng bàn phím được, nhãn và thông báo đọc rõ.
- [ ] Test DB dùng database riêng có guard; không `EnsureDeleted`/reset database của thành viên. Không đưa ảnh/tài khoản/số liệu kiểm thử thành dữ liệu thật để làm đẹp trang.

Các lệnh tham khảo chạy tại thư mục repo, sau khi main build được:

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release

# Bộ test hiện có liên quan luồng khách/đặt sân; xem fixture trước khi chạy.
dotnet test tests/QuanLyDatSan.M4.Tests/QuanLyDatSan.M4.Tests.csproj -c Release --filter "FullyQualifiedName~DonDatHttpTests|FullyQualifiedName~DatSanTests|FullyQualifiedName~RepositoryHttpTests"

# Tích hợp menu, không thay thế test nghiệp vụ hồ sơ/đặt/hủy đơn.
dotnet test tests/QuanLyDatSan.M103.Tests/QuanLyDatSan.M103.Tests.csproj -c Release --filter "FullyQualifiedName~MenuVaiTroTests"

git diff --check
git status --short
```

Các test SQL Server cần LocalDB phù hợp theo fixture. Không dùng `--no-build` để lấy kết quả DLL cũ khi source mới chưa build được. Báo rõ test nào chưa chạy do môi trường/database; không ghi “đã test toàn bộ” chỉ dựa vào build hoặc 17 test menu.

## 11. Git, migration, PR và lưu ý tránh lặp lỗi

1. Kiểm tra `git status`, bảo toàn thay đổi đang có rồi cập nhật `main` bằng fast-forward; tạo nhánh `feature/m3-ten-dau-viec`. Không sửa/push trực tiếp main hoặc dùng nhánh của thành viên khác.
2. Commit đúng MSSV, ví dụ: `[23103100181] [M3] Bo sung huy don cho cua khach hang`. Chỉ stage file thuộc task; không `git add .` khi có thay đổi không liên quan.
3. PR theo template repo: vấn đề/hành vi sau sửa, file chung bị tác động, migration, lệnh test/kết quả, giới hạn và cách chạy thử. Không đưa mật khẩu/token/connection string thật vào bằng chứng.
4. Chỉ `Closes #20` khi hoàn thành đủ issue hủy; PR validation hồ sơ mới là một phần #17 thì ghi `Liên quan #17`, không đóng sớm. Mã `M3-04` không phải số issue GitHub.
5. Validation, ViewModel và nối form hủy với RowVersion sẵn có thường không cần migration. Khi thực sự đổi schema, phối hợp M1 và gửi đủ migration `.cs`, `.Designer.cs`, `ApplicationDbContextModelSnapshot.cs`; không sửa migration cũ đã dùng chung.
6. Không commit hai file migration `CapNhatModel` đã bị loại trước đây. Kiểm tra `.gitignore`: main đối chiếu còn bỏ qua `Migrations/`, nên không dựa riêng vào `git status` để kết luận đã đưa đủ migration mới vào PR.
7. Sửa conflict theo cấu trúc code và kiểm lại Razor/DI/DTO; không xóa nhánh `switch`, biến dùng phía dưới hoặc nguồn menu chung. Build ngay sau giải quyết conflict.
8. Khi thay chữ ký hoặc mã lỗi, cập nhật implementation, Controller, ViewModel và test trong cùng PR; không chỉ sửa interface để hết lỗi ở một project.
9. Giữ kiểm tra quyền server, transaction và lịch sử. Không tạo hai bộ quy tắc hủy cho khách và nhân viên; không tính tiền trong View; không thêm thư viện giao diện hoặc nâng package ngoài task.
10. Một thành viên khác review; Tùng merge bằng **Create a merge commit**. Không tự đánh dấu PR/issue đạt khi chưa có bằng chứng.

### Mẫu bàn giao ngắn cho mỗi PR

```text
Issue: #...
Đã làm: ...
Luồng sử dụng và route thực tế: ...
File chung / người phối hợp: ...
Migration: không có / tên và phạm vi thay đổi
Kiểm thử: lệnh, số ca đạt, ca lỗi và môi trường
Chưa kiểm thử / còn phụ thuộc: ...
Đủ đóng issue: có / chưa, vì ...
```

## 12. Cách đọc tài liệu M3 cũ

[Phần 2 — Tạo đơn](Phan-2-Tao-don.md) và [Phần 3 — Giao diện](Phan-3-Giao-dien-khach-hang-quan-tri.md) ghi lại giai đoạn trước; không sửa mất lịch sử đóng góp. Một số mô tả đã cũ: hàm tính tiền tạm đã được thay bằng nguồn M5 trong service đọc, menu đã có thêm chức năng, và hủy đơn có backend M4 nhưng UI khách vẫn là phần M3 cần làm. Dùng code tại commit được review và hợp đồng mới để quyết định, không sao chép nguyên quy tắc cũ trái hợp đồng hiện tại.

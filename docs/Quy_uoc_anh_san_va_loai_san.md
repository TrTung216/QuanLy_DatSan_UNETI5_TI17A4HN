# Quy ước ảnh sân và ảnh loại sân

**Nhóm 5 — Đề 18: Quản lý đặt sân thể thao — DHTI17A4HN**

**Phiên bản:** 1.1 — 04/11/2026.

**Điều phối:** Trần Trọng Tùng — MSSV 23103100202 (M1).

**Nội dung:** thống nhất phạm vi, phân công, dữ liệu, luồng lưu ảnh, phân quyền, giao diện và kiểm thử. Codex hỗ trợ biên soạn theo yêu cầu của Tùng.

**Trạng thái:** hợp đồng nền đã triển khai trên nhánh `feature/m1-kho-anh-dung-chung` từ main `6fa6c3b` (đã có nền đặt cọc PR #67). M2 đã thống nhất trường ảnh sân nullable, dài tối đa 255 ký tự và concurrency token. Xem [bàn giao phần ảnh M1 và hướng dẫn M2](module-1/Phan-9-Anh-loai-san.md) để phân biệt phần đã làm và phần M2 tiếp tục. M2 tích hợp sau khi PR nền được review và merge; tài liệu không xác nhận toàn bộ chức năng ảnh đã nghiệm thu.

Áp dụng cùng [quy ước Controller–Service–Repository](Quy_uoc_Controller_Service_Repository.md) và [hướng dẫn layout](Huong_dan_su_dung_layout.md). Không thay đổi giá, cọc, lịch đặt hoặc quyền nghiệp vụ đang có.

## 1. Phạm vi bản đầu

- Mỗi `LoaiSan` có tối đa **một ảnh minh họa đại diện**.
- Mỗi `SanTheThao` có tối đa **một ảnh thực tế đại diện**.
- Ảnh không bắt buộc. Sân/loại sân chưa có ảnh vẫn hoạt động bình thường.
- Có thao tác tải ảnh lên, thay ảnh và gỡ ảnh. Người dùng không nhập URL ảnh bên ngoài hoặc đường dẫn file trên máy chủ.
- Ảnh loại sân dùng cho nhóm sân; ảnh sân dùng cho thẻ sân và trang chi tiết. Không coi ảnh loại sân là ảnh thực tế của một sân cụ thể.
- Chưa làm album nhiều ảnh, kéo thả sắp xếp, trình cắt ảnh, upload cho khách hàng hoặc lưu ảnh riêng trên đơn đặt sân.
- Đây là chức năng nâng cao; không thay thế các yêu cầu bắt buộc còn thiếu của đề.

## 2. Phân công và thứ tự tích hợp

| Phần việc | Người phụ trách | Kết quả cần bàn giao |
| --- | --- | --- |
| Quy ước, hợp đồng lưu ảnh dùng chung, cấu hình, DI | M1 — Trần Trọng Tùng | Một đầu mối lưu/đọc/gỡ file và bộ kiểm tra ảnh dùng chung; M2 review khả năng sử dụng |
| Thay đổi schema hai Entity, mapping và migration | M1 điều phối; M2 thống nhất phần `SanTheThao` trước | Một migration nền, snapshot và tài liệu triển khai; tránh hai thành viên sinh migration trùng |
| Quản lý ảnh loại sân | M1 | Service/repository, form, quyền Admin, test |
| Quản lý ảnh sân | M2 — Phan Giang Tâm | Service/repository, form, quyền Admin/Nhân viên, test |
| Hiển thị trang chủ theo loại, quy cách thẻ ảnh chung | M1, phối hợp M2 | DTO trang chủ và thành phần hiển thị nhất quán |
| Hiển thị danh sách/chi tiết sân | M2 | Ảnh thật hoặc trạng thái chưa có ảnh, không phá lọc/phân trang |
| Dùng ảnh ở màn hình khách/đơn nếu bổ sung sau | M3/M4 | Dùng hợp đồng đọc và quyền hiện có; không tự tạo cơ chế upload |
| M5 | Không có phần bắt buộc trong bản đầu | Không thay tiền hoặc báo cáo chỉ vì thêm ảnh |

Trình tự tích hợp: **nền dữ liệu/hạ tầng và ảnh loại sân M1 → ảnh sân M2 → nghiệm thu chung**. PR M1 kèm caller ảnh loại sân để kiểm chứng hợp đồng thực tế. M2 làm form/service và hiển thị ảnh sân sau khi nền được merge.

Mỗi người giữ header Họ tên/MSSV/phần việc thực tế; trao đổi trước khi sửa file chung hoặc Entity do người khác phụ trách.

## 3. Dữ liệu và migration

| Entity | Trường mới | Quy định |
| --- | --- | --- |
| `LoaiSan` | `string? DuongDanAnh` | Nullable, tối đa 255 ký tự |
| `SanTheThao` | `string? DuongDanAnh` | Nullable, tối đa 255 ký tự |

- Database chỉ lưu **khóa tương đối do server sinh**, ví dụ `loai-san/<guid>.jpg` hoặc `san/<guid>.jpg`. Dùng dấu `/`, không chứa ổ đĩa, hostname, `..`, query string hoặc tên file gốc của người tải lên.
- `null` nghĩa là chưa có ảnh; không dùng chuỗi rỗng, đường dẫn placeholder hoặc ảnh của loại sân để điền vào trường ảnh sân.
- Không lưu `IFormFile`, byte ảnh hoặc Base64 trong Entity/database của bản đầu.
- Mỗi file gắn với một bản ghi; không dùng chung khóa file giữa nhiều sân/loại sân. Nhờ đó gỡ ảnh của một bản ghi không làm hỏng ảnh bản ghi khác.
- Mapping `DuongDanAnh` là **concurrency token** trên cả hai Entity. Cơ chế này bảo vệ cập nhật ảnh bằng giá trị cũ trong điều kiện UPDATE; không cần thêm cột RowVersion trong bản đầu.
- DTO/ViewModel đọc được bổ sung thông tin ảnh theo nhu cầu. Input ghi không được bind trực tiếp Entity hoặc tin `DuongDanAnh` do client gửi như vị trí file cần thao tác.
- Migration nền `20261104074333_ThemAnhDaiDienSanVaLoaiSan`, nối tiếp `20261103132533_ThemGiaoDichVaBienNhanCoc`. Hai cột mới nullable, không có giá trị mặc định giả, không sửa dữ liệu nghiệp vụ cũ; EF sinh thêm UpdateData đặt cột ảnh mới của các loại sân seed về null.
- Commit đủ migration, file Designer và `ApplicationDbContextModelSnapshot.cs`. Không sửa/xóa migration đã chia sẻ, không reset database.
- Việc cấu hình concurrency token ảnh cũng ảnh hưởng UPDATE Entity hiện có; PR nền phải chạy lại test CRUD M1/M2 và ánh xạ lỗi xung đột hiện hành.

## 4. Quyền truy cập

| Thao tác | Guest | Khách hàng | Nhân viên | Admin |
| --- | --- | --- | --- | --- |
| Xem ảnh sân/loại sân được công khai | Có | Có | Có | Có |
| Xem ảnh bản ghi bị ẩn/ngừng hoạt động | Không | Không | Theo quyền xem dữ liệu quản trị hiện có | Có |
| Tải lên/thay/gỡ ảnh loại sân | Không | Không | Không | Có |
| Tải lên/thay/gỡ ảnh sân | Không | Không | Có | Có |

- Giữ `[Authorize]` phía Controller và kiểm tra quyền/bản ghi phía service; việc ẩn nút không thay thế kiểm tra ở server.
- Mọi thao tác ghi dùng POST + antiforgery. GET chỉ đọc, không tự tạo hoặc xóa file.
- Quyền xem ảnh đi theo quyền xem đối tượng. Với khách/Guest, ảnh sân chỉ công khai khi sân và loại sân thuộc phạm vi công khai như luồng đọc sân hiện hành.
- Không nhận ID/đường dẫn rồi đọc file trực tiếp mà bỏ qua việc tải bản ghi và kiểm tra quyền.

## 5. Hợp đồng giữa các tầng

| Thành phần | Trách nhiệm |
| --- | --- |
| Controller/ViewModel | Nhận `IFormFile`, ID, hành động và phiên bản ảnh cũ; kiểm tra ModelState/antiforgery; chuyển dữ liệu sang kiểu độc lập MVC; ánh xạ kết quả thành View/redirect |
| Service loại sân/sân | Kiểm tra quyền và đối tượng, quyết định thay/gỡ ảnh, điều phối kho file và UoW, xử lý xung đột |
| Repository | Nạp bản ghi tracking, đọc khóa ảnh hiện hành, chuẩn bị cập nhật đúng trường ảnh; không đọc/ghi file hoặc tự SaveChanges |
| `IDonViCongViec` | Lưu/commit/rollback thay đổi database theo hợp đồng hiện có |
| `IKhoAnh` | Kiểm tra/chuẩn hóa ảnh, lưu file mới bằng khóa an toàn, đọc/xóa file trong vùng lưu trữ; không đọc database hoặc quyết định quyền người dùng |

`IKhoAnh`, `AnhTaiLen`, `YeuCauAnh`, `KetQuaAnh`, `MaLoiAnh` và `IDonDepAnh` nằm tại `Services/Anh/HopDongAnh.cs`, đăng ký tại `Extensions/DependencyInjection.cs`; M2 dùng lại. Chữ ký chính xác và mẫu tích hợp nằm trong tài liệu bàn giao liên kết ở đầu trang. Không đưa xử lý file trực tiếp vào từng Controller hoặc repository.

Hợp đồng nghiệp vụ hiện có và điểm tích hợp M2:

| Phương thức | Input | Output |
| --- | --- | --- |
| `IAnhService.CapNhatLoaiSanAsync(int id, YeuCauAnh yeuCau, CancellationToken ct)` | Loại sân, hành động, khóa ảnh cũ, dữ liệu upload nếu thay | `Task<KetQuaAnh>`; M1 đã triển khai |
| `IAnhService.DocAsync(NhomAnh nhom, int id, CancellationToken ct)` | Loại đối tượng + ID, danh tính từ cơ chế M1 | `Task<Stream?>`; null nếu không tồn tại/không được xem/không đọc được ảnh |
| Service ảnh sân của M2 (chưa triển khai) | Dùng lại `YeuCauAnh`, ID sân và CancellationToken | Dùng lại `KetQuaAnh`, `IKhoAnh`, `IDonDepAnh`; tên phương thức ghi sân do M2 bổ sung rõ ràng trong service sân |

Dữ liệu ảnh truyền vào service dùng Stream và metadata cần kiểm tra, không chứa `IFormFile`/HttpContext. Controller sở hữu vòng đời stream đầu vào, giữ mở đến khi service hoàn tất và dispose sau đó; kho ảnh không tự đóng stream do caller sở hữu. Không đọc cả ảnh sang Base64 trong ViewModel.

Khóa ảnh cũ từ form chỉ là điều kiện chống ghi đè. Service so với khóa hiện hành ở DB, sau đó EF concurrency token bảo vệ từ lúc đọc đến lúc ghi. Tuyệt đối không dùng khóa từ form để mở/xóa file.

## 6. Luồng giao diện và lưu ảnh

### 6.1. Tách form ảnh khỏi form nghiệp vụ

- Bản đầu tạo sân/loại sân bằng form hiện tại. Sau khi tạo thành công, cung cấp lối vào phần **Ảnh đại diện** ở trang sửa/chi tiết để thêm ảnh.
- Form ảnh độc lập, chỉ hoạt động khi bản ghi đã tồn tại; `multipart/form-data` khi upload. Lưu tên/giá/trạng thái không tự thay ảnh.
- Có hai hành động rõ ràng: **Thay ảnh** yêu cầu đúng một file; **Gỡ ảnh** không gửi file. Nếu gửi đồng thời lệnh gỡ và file, trả lỗi đầu vào thay vì tự chọn một ý định.
- Gỡ khi đang không có ảnh là thao tác không đổi dữ liệu; nếu phiên bản ảnh đã thay đổi thì trả xung đột, không gỡ ảnh mới của người khác.
- Khi lỗi upload, giữ thông báo và ảnh cũ; trình duyệt yêu cầu người dùng chọn lại file nếu cần. Không cố gán lại input file từ server.

### 6.2. Thay ảnh

1. Kiểm tra quyền, ID và phiên bản ảnh cũ; xác nhận dữ liệu upload hợp lệ.
2. Kho ảnh kiểm tra/chuẩn hóa rồi lưu file mới bằng tên duy nhất. Chỉ coi lưu thành công khi file đã ghi đầy đủ và đóng handle.
3. Service cập nhật khóa ảnh mới qua repository và UoW. Cập nhật chỉ trường ảnh, tránh ghi đè tên/giá/trạng thái từ form cũ.
4. Chỉ trả thành công sau khi database commit. Sau đó mới dọn ảnh cũ lấy từ DB.
5. Nếu DB chắc chắn không commit, dọn file mới do thao tác này tạo; ảnh cũ và khóa DB vẫn giữ nguyên.

Database transaction **không rollback được file trên ổ đĩa**. Không giữ transaction database mở trong suốt thời gian upload/giải mã ảnh. Nếu kết quả commit không xác định, kiểm tra lại tham chiếu trước khi dọn; không xóa file có thể vừa được DB ghi nhận. Chưa kiểm chứng được thì giữ file và ghi nhận để dọn sau.

### 6.3. Gỡ ảnh và xóa đối tượng

- Gỡ ảnh: lưu `DuongDanAnh = null` thành công rồi mới xóa file cũ.
- Xóa sân/loại sân: giữ kiểm tra lịch sử/FK hiện có. Chỉ dọn ảnh khi xóa bản ghi thành công; thao tác xóa bị chặn không được làm mất ảnh.
- Ngừng hoạt động không đồng nghĩa xóa ảnh. Khi mở lại, ảnh vẫn có thể sử dụng.
- Xóa file sau commit thất bại: không báo rằng cập nhật database đã rollback. Ghi log tác vụ dọn, trả kết quả lưu ảnh đúng thực tế; có thể kèm `Warning` thân thiện nếu cần.
- File mồ côi do crash/lỗi cleanup được dọn bằng công cụ bảo trì có kiểm tra tham chiếu **cả hai bảng**, bỏ qua file mới dưới 24 giờ và file đang xử lý. Không triển khai thao tác xóa cả thư mục upload để “dọn nhanh”.

### 6.4. Cập nhật đồng thời

Hai người mở cùng ảnh A rồi thay bằng B/C: chỉ một cập nhật với phiên bản A được chấp nhận. Yêu cầu còn lại trả `XungDotAnh`, dọn file mới của chính yêu cầu thất bại khi an toàn và yêu cầu tải lại trang. Không xóa ảnh đang được yêu cầu thành công sử dụng; không tự retry ghi đè.

## 7. Kiểm tra file và cấu hình lưu trữ

Các giới hạn sau là quyết định của nhóm cho bản đầu:

| Thuộc tính | Quy định |
| --- | --- |
| Định dạng đầu vào | JPEG/JPG, PNG, WebP ảnh tĩnh |
| Dung lượng mỗi file | Tối đa **5 MiB = 5.242.880 byte**; từ chối file rỗng |
| Kích thước giải mã | Tối đa 4096 px mỗi chiều và 16.000.000 pixel tổng cộng; giới hạn trước khi cấp phát ảnh lớn |
| File lưu ra | JPEG chuẩn hóa, cạnh dài tối đa 1600 px, giữ tỷ lệ; không phóng ảnh nhỏ lên |
| Metadata | Áp dụng hướng ảnh rồi bỏ EXIF/GPS và metadata không cần thiết; vùng trong suốt chuyển sang nền trắng |
| Tên file | Server sinh GUID ngẫu nhiên; không ghi đè file cũ cùng tên, không dùng tên gốc làm khóa lưu |

- Kiểm tra phần mở rộng, nội dung và khả năng giải mã thực tế; không chỉ tin `ContentType`, tên file hoặc JavaScript. Từ chối SVG, GIF, ảnh nhiều frame/động, file hỏng hoặc giả đuôi ảnh.
- Giới hạn request multipart tổng thể 6 MiB cho một file và metadata; endpoint gắn `[GioiHanTaiAnh]` và các thuộc tính giới hạn MVC theo hướng dẫn bàn giao. Đồng bộ web server khi triển khai. Vượt giới hạn trả 413 dễ hiểu.
- Nền dùng `Magick.NET-Q8-AnyCPU` phiên bản 15.0.0, giấy phép Apache-2.0; policy đi kèm chỉ cho phép JPEG/PNG/WebP, chặn delegate và giới hạn tài nguyên. M2 không thêm thư viện giải mã khác.
- Cấu hình thư mục bằng `LuuTruAnh:ThuMucGoc` qua cấu hình môi trường/User Secrets. Dùng thư mục riêng ngoài cây source và thư mục publish; ứng dụng chỉ cần quyền đọc/ghi, không cấp quyền thực thi file upload.
- Không commit đường dẫn ổ đĩa cá nhân. Thiếu cấu hình lưu trữ: chức năng quản lý khác vẫn dùng được; thao tác ảnh trả `LuuTruAnhChuaSanSang`, không tự ghi vào thư mục hệ thống.
- Mọi khóa file do kho ảnh xử lý phải đúng mẫu `san/<guid>.jpg` hoặc `loai-san/<guid>.jpg`; xác minh đường dẫn resolve nằm trong thư mục gốc. Không nối trực tiếp tên file/ID tùy ý thành đường dẫn.
- Giai đoạn local không cần cloud/CDN. Có thể thay implementation kho ảnh về sau, giữ hợp đồng service và khóa tương đối ổn định hoặc có kế hoạch chuyển dữ liệu rõ ràng.

Tham khảo nguyên tắc kiểm tra server, tên file an toàn, giới hạn upload và vùng lưu riêng: [Microsoft — Upload files in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0). Các con số giới hạn và định dạng đầu ra ở trên là lựa chọn của nhóm, không phải mặc định của ASP.NET Core.

## 8. Phục vụ ảnh và hiển thị

- Không mở toàn thư mục upload bằng static-file middleware công khai, vì có ảnh thuộc sân/loại sân không được công khai.
- Endpoint đọc ảnh `GET /Anh/San/{id}` và `GET /Anh/LoaiSan/{id}` đã có trong `AnhController`: service nạp bản ghi, kiểm tra quyền xem, lấy khóa từ DB rồi đọc kho ảnh.
- Với Guest/Khách hàng, đối tượng không công khai, không có ảnh hoặc file mất trả 404 theo luồng đọc; View xử lý ảnh thiếu. Không lộ tên thư mục vật lý hoặc phân biệt thông tin quản trị qua thông báo.
- Bản đầu trả ảnh với `Content-Type: image/jpeg`, `X-Content-Type-Options: nosniff` và `Cache-Control: private, no-store` để tránh cache công khai vượt quyền khi đổi trạng thái. Chỉ tối ưu cache sau khi thiết kế rõ invalidation/quyền.
- Thẻ sân/nhóm sân dùng tỷ lệ **16:9**, `object-fit: cover`; trang chi tiết cho xem đầy đủ tỷ lệ ảnh. Không cắt ảnh gốc vĩnh viễn chỉ để vừa thẻ.
- Có `width`/`height` hoặc vùng tỷ lệ cố định để tránh nhảy bố cục; ảnh ngoài màn hình đầu có thể dùng lazy loading.
- Ảnh sân có alt theo tên sân; ảnh loại sân có alt “Ảnh minh họa loại sân …”. Tránh lặp alt dài nếu hình chỉ trang trí và tên đã nằm trong cùng liên kết.
- Sân chưa có ảnh hiện biểu tượng trung tính và chữ **Chưa có ảnh sân**. Loại sân chưa có ảnh hiện **Chưa có ảnh minh họa**. Bản đầu không tự lấy ảnh loại sân làm ảnh sân.
- Dùng ảnh thực tế/ảnh được phép sử dụng; không đưa số điện thoại, biển số hoặc thông tin riêng tư không cần thiết vào ảnh demo.

## 9. Lỗi, thông báo và log

M1 định nghĩa bộ `MaLoiAnh` dùng chung cho hạ tầng ảnh; service M1/M2 ánh xạ vào kết quả nghiệp vụ hiện có. Không quyết định nhánh bằng cách so sánh chuỗi thông báo.

| Mã lỗi | Ý nghĩa / phản hồi |
| --- | --- |
| `DuLieuAnhKhongHopLe` | Không có file khi thay, file rỗng, số file sai hoặc hành động mâu thuẫn |
| `DinhDangAnhKhongHoTro` | Định dạng bị cấm hoặc nội dung không khớp định dạng |
| `AnhQuaDungLuong` | File vượt 5 MiB |
| `AnhQuaKichThuoc` | Vượt chiều rộng/cao hoặc tổng pixel |
| `AnhKhongDocDuoc` | File lỗi, không giải mã thành ảnh hợp lệ |
| `XungDotAnh` | Ảnh/bản ghi đã đổi từ khi mở form; tải lại trước khi tiếp tục |
| `LuuTruAnhChuaSanSang` | Thiếu cấu hình hoặc kho ảnh không khả dụng |
| `ThanhCong` | Đã thực hiện; `CanDonTep = true` nghĩa là lưu DB thành công nhưng chưa dọn được tệp cũ |
| `KhongTonTai` | Bản ghi không tồn tại; caller ghi trả 404 |
| `KhongDuQuyen` | Không được thao tác; caller ghi trả Forbid |

`IKhoAnh` báo lỗi dự kiến bằng `LoiAnhException.MaLoi`; service bắt và trả `KetQuaAnh`. Đọc ảnh thiếu/không được xem đều trả 404. Lỗi ngoài dự kiến đi tới handler/log chung, không che thành lỗi định dạng ảnh. UI không hiển thị exception, stack trace hoặc đường dẫn vật lý. Dùng TempData `Success`, `Error`, `Warning`, `Info` theo quy ước; lỗi file gắn vào trường ảnh khi phù hợp.

## 10. Git, dữ liệu demo và triển khai

- Nhánh gợi ý: `feature/m1-ha-tang-anh`, `feature/m1-anh-loai-san`, `feature/m2-anh-san`.
- Commit theo `[MSSV] [Module] Noi dung cong viec`; PR theo template repo, nêu file chung, migration và test. Một thành viên khác review, Tùng merge bằng **Create a merge commit**.
- Không commit file upload phát sinh khi chạy app, thư mục cache/tạm hoặc log. Ảnh minh họa demo được chọn riêng, có quyền sử dụng, có thể lưu trong `docs/du-lieu-mau/anh/` kèm hướng dẫn import; thư mục này chỉ tạo khi có dữ liệu thực tế cần bàn giao.
- `git pull` chỉ lấy code, không đồng bộ file upload và database giữa máy. Người chạy local phải cấu hình kho ảnh và import dữ liệu demo theo cùng bộ hướng dẫn.
- Backup/restore phải bao gồm database **và** kho ảnh tương ứng. Không chỉ sao chép đường dẫn trong database rồi báo đã bàn giao ảnh.
- Khi deploy lại ứng dụng, giữ kho ảnh ngoài thư mục publish để không bị xóa cùng bản build. Không ghi đường dẫn tuyệt đối của máy phát triển vào migration/seed.

## 11. Điều kiện nghiệm thu

- [ ] Migration từ database hiện có thành công, cột mới nullable; bản ghi cũ không bị đổi/mất dữ liệu.
- [ ] Admin quản lý được ảnh loại sân; Admin/Nhân viên quản lý được ảnh sân; Guest/Khách hàng gửi POST trực tiếp bị chặn, thiếu antiforgery bị chặn.
- [ ] Upload/thay/gỡ đúng file; lưu form tên/giá không làm mất ảnh. File/ảnh hiện đúng sau restart ứng dụng.
- [ ] Chặn file rỗng, quá dung lượng, quá pixel, giả đuôi, SVG/GIF, ảnh nhiều frame, file hỏng và tên có path traversal.
- [ ] Khi giải mã/lưu file/ghi DB lỗi, ảnh cũ vẫn dùng được; không để DB trỏ tới file ghi dở. Kiểm thử cả lỗi sau commit và cleanup thất bại.
- [ ] Hai yêu cầu thay/gỡ đồng thời không ghi đè im lặng hoặc xóa ảnh của yêu cầu thắng; không tự retry.
- [ ] Xóa sân/loại sân bị chặn do dữ liệu tham chiếu không xóa ảnh; ngừng hoạt động chỉ đổi quyền hiển thị.
- [ ] Endpoint ảnh không lộ dữ liệu bị ẩn khi đổi ID, đổi vai trò hoặc đổi trạng thái đối tượng.
- [ ] File bị mất ngoài ý muốn hiển thị fallback; không làm trang danh sách/trang chủ lỗi toàn bộ.
- [ ] Trang chủ, danh sách và chi tiết hiển thị desktop/mobile đúng tỷ lệ, alt/nhãn rõ ràng; tìm/lọc/sort/phân trang vẫn hoạt động.
- [ ] Test dùng thư mục tạm và database riêng có guard; không dọn kho ảnh/database người dùng để kiểm thử.
- [ ] Build/test phù hợp đạt; hướng dẫn cấu hình, dữ liệu demo, backup và giới hạn đã ghi trong PR. Chỉ tick mục thực sự đã kiểm tra.

Khi muốn thêm album nhiều ảnh, M1/M2 thống nhất Entity ảnh riêng, thứ tự hiển thị và ảnh đại diện trong phiên bản quy ước tiếp theo. Không lưu nhiều đường dẫn ghép bằng dấu phẩy vào `DuongDanAnh`.

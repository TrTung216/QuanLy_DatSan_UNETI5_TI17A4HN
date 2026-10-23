# Quy ước bổ sung: đặt cọc và chuyển khoản

Điều phối: **Trần Trọng Tùng — MSSV 23103100202 (M1)**. Áp dụng phối hợp M1–M5 theo [bản phân công](Phan_cong_chuyen_khoan_QR.md).

Nội dung thực hiện: thống nhất quy ước tiền cọc, phân quyền, transaction, mô phỏng và nghiệm thu.

Tài liệu bổ sung quy ước nhóm v1.4, theo yêu cầu mới của Tùng: **khách chuyển cọc trước, nhân viên/Admin xác nhận cọc và đặt sân sau**. Thay thế quy tắc cũ chỉ nhận cọc sau khi đơn đã xác nhận; các quy tắc không liên quan vẫn giữ nguyên.

**Cập nhật 23/10/2026:** [hợp đồng M3/M4/M5 v1.0](Hop_dong_M3_M4_M5.md) đã chốt phương thức, dữ liệu, lỗi, chính sách đơn cũ/đơn mới và nguồn tính tiền dựa trên main `1a97883`. Đây là tài liệu triển khai chi tiết có ưu tiên trong phạm vi này.

Đây là hợp đồng triển khai và review, không phải xác nhận chức năng đã có. Code QR thử nghiệm đã gỡ; chưa triển khai lại.

## 1. Phạm vi và thuật ngữ

- Cọc bằng **50% tiền sân dự kiến**, chưa tính dịch vụ phát sinh.
- Đợt đầu dùng mô phỏng trên local; chưa kết nối ngân hàng, webhook hoặc hoàn tiền tự động.
- “Khách đã chuyển” là thông tin cần đối chiếu. “Đã nhận cọc” chỉ được hiển thị sau khi nhân viên/Admin xác nhận thành công trên server.
- Trạng thái tiền và trạng thái đơn hiển thị riêng; không tạo enum đơn mới chỉ để biểu diễn khách báo đã chuyển.
- Không đổi giá trị enum đang lưu: `ChoXuLy = 0`, `DangXuLy = 1`, `HoanThanh = 2`, `DaHuy = 3`, `TuChoi = 4`.

## 2. Quy tắc trạng thái

| Tình huống | Xử lý |
| --- | --- |
| Khách tạo đơn hợp lệ | `ChoXuLy`, tiền cọc đã ghi nhận bằng 0; hiển thị số tiền cần cọc riêng. |
| Khách mở trang cọc | Chỉ đơn của mình, đang `ChoXuLy`, chưa ghi nhận cọc, chưa bắt đầu; đọc dữ liệu không thay đổi trạng thái. |
| Khách báo đã chuyển hoặc đưa ảnh chụp | Không tự tăng `TienCoc`, không xác nhận đặt sân. |
| Nhân viên/Admin đối soát hợp lệ | Ghi biên nhận, `TienCoc` và chuyển `ChoXuLy → DangXuLy` trong một transaction. |
| Đã xác nhận cọc | Không mời chuyển lại; hiển thị kết quả và biên nhận. |
| Đã nhận cọc và yêu cầu hủy | Không hủy theo luồng thông thường khi chưa có chức năng hoàn cọc; chuyển xử lý ngoại lệ thủ công. |
| Đơn hoàn thành/hủy/từ chối | Không mở lại hoặc dùng thao tác cọc để ép chuyển trạng thái. |

Mọi endpoint xác nhận đơn phải tuân thủ yêu cầu cọc đối với đơn thuộc luồng mới. Không chỉ sửa nút giao diện rồi để action cũ bỏ qua điều kiện.

**Giữ chỗ:** v1.3 quy định đơn chờ giữ chỗ đến khi xử lý, chưa tự hết hạn. Tiếp tục dùng quy tắc nền này cho đến khi nhóm chốt thay đổi; không tự thêm bộ đếm, job hết hạn hoặc coi QR hết hạn là đơn tự hủy.

**Đã chốt trong hợp đồng:** chính sách 0 cho đơn cũ, 1 cho đơn mới; không tự hết hạn; cọc yêu cầu bằng 0 xác nhận thường; có giao dịch khớp chờ xử lý thì chặn hủy/từ chối. Không sửa lịch sử hoặc tạo biên nhận giả. Schema và guard mới chưa được triển khai trong lần cập nhật tài liệu.

## 3. Quy tắc tiền và thời gian

- Dùng `decimal`; không dùng `float`/`double` cho tiền nghiệp vụ.
- Tiền sân dùng `ITinhTienService` đã có của M5; M4 chuyển caller khỏi công thức lặp `DatSanService.TinhTienSanDuKien` theo hợp đồng. Không sao chép công thức vào View/controller.
- Cọc yêu cầu: `Math.Round(tienSanDuKien * 0.5m, 0, MidpointRounding.AwayFromZero)`. Tính từ tiền sân đã được hàm chung làm tròn; test rõ thứ tự làm tròn.
- Ví dụ: tiền sân 300.000 đồng → cọc 150.000 đồng. Tổng sau dịch vụ 350.000 đồng → còn thu 200.000 đồng.
- Số tiền đối soát phải đúng cọc yêu cầu. Thiếu/thừa được báo lỗi và xử lý ngoại lệ; chưa hỗ trợ cộng dồn nhiều lần chuyển.
- Tổng tiền = tiền sân + dịch vụ hợp lệ. Còn phải thu = tổng tiền − cọc, không được âm.
- Doanh thu theo quy ước nhóm: đơn `HoanThanh`, theo `NgayHoanThanh`, cộng `TongTien`; không cộng cọc lần nữa.
- Thay giá sân không tính lại đơn giá đã lưu của đơn cũ. Giá, tỷ lệ cọc, mã khách và tổng tiền không lấy từ dữ liệu khách gửi.
- Dùng cùng quy ước thời gian/đồng hồ của dự án. Thời điểm tiền nhận không trước khi tạo đơn, không ở tương lai và phải trước giờ bắt đầu; áp dụng thêm hạn cọc nếu nhóm chốt.
- Không yêu cầu tiền nhận sau `NgayXacNhan`, vì luồng mới nhận tiền trước. Ngày/người xác nhận do server ghi.
- V1 không xác nhận đặt sân sau giờ bắt đầu bằng luồng cọc; tiền nhận đúng giờ nhưng đối soát muộn được chuyển ngoại lệ thủ công theo hợp đồng.

## 4. Phân tầng và phân quyền

- **Controller:** nhận ViewModel, kiểm tra ModelState, phân quyền, anti-forgery, gọi interface service và trả View/redirect/HTTP. Không truy vấn DbContext hoặc điều phối transaction.
- **Service:** kiểm tra dữ liệu thực, quyền đối với bản ghi, tính hợp lệ, transaction và lưu dữ liệu. Không dùng `ModelState`, `TempData`, `IActionResult` hoặc thao tác cookie.
- **View/ViewModel:** chỉ dữ liệu form/hiển thị và validation form; không quyết định tiền chính thức hoặc quyền nghiệp vụ.
- Khách chỉ đọc thông tin cọc/biên nhận của đơn mình. Giới hạn truy vấn theo tài khoản hiện tại; xử lý không có/không được phép nhất quán để không lộ đơn người khác.
- Chỉ Admin/Nhân viên hợp lệ được đối soát; kiểm tra lại tài khoản đang hoạt động và quyền trong thao tác nhạy cảm ở service.
- GET không ghi dữ liệu. Thao tác thay đổi dùng POST và anti-forgery. Không bind trực tiếp Entity hoặc nhận người xác nhận, trạng thái, thông tin nhận tiền từ khách.
- M3 gọi service cọc do M4 chủ trì; M5 cung cấp công thức tiền. Service con không tự commit hoặc mở transaction độc lập trong thao tác xác nhận chung.
- DI tập trung tại `Extensions/DependencyInjection.cs`, lifetime phù hợp DbContext; dùng async/CancellationToken, không truy vấn song song cùng DbContext.

## 5. Transaction, chống trùng và lưu lịch sử

1. Service điều phối mở transaction, lấy khóa sân bằng cơ chế dùng chung.
2. Nạp/kiểm tra lại quyền, trạng thái đơn, phiên bản, thời gian, khả dụng và giao dịch.
3. Kiểm tra không trùng lịch, loại chính đơn đang xử lý khỏi truy vấn trùng.
4. Lưu biên nhận, gán tiền cọc và xác nhận đơn rồi commit. Không cộng thêm cọc khi gửi lại.
5. Nếu lỗi, rollback cả ba; không để thao tác SaveChanges sau đó ghi lại dữ liệu từ lần thất bại.

- `AnyAsync` rồi `SaveChangesAsync` ngoài phạm vi khóa/transaction không đủ chống cạnh tranh.
- Kết hợp RowVersion/cơ chế concurrency hiện có và unique index ở database; không chỉ khóa nút hoặc khóa trong bộ nhớ.
- Một biên nhận cọc thành công cho một đơn trong đợt này. Khóa chống dùng lại giao dịch phải gồm nguồn/ngân hàng, tài khoản nhận và mã giao dịch đã chuẩn hóa; không giả định mã giao dịch duy nhất toàn hệ thống ngân hàng.
- Biên nhận lưu đủ số tiền, mã đơn/giao dịch, thời điểm nhận, người/thời điểm đối soát, thông tin nhận tiền cần bảo toàn lịch sử. Đánh dấu nguồn mô phỏng/thật để phân biệt.
- Không cascade xóa lịch sử biên nhận; không cung cấp sửa/xóa biên nhận đã xác nhận trong đợt này.
- Transaction database không hoàn tác tiền ngân hàng. Nếu đã có tiền nhưng đơn không thể xác nhận, không ép lịch trùng; lưu bằng chứng ngoại lệ theo cơ chế riêng đã chốt, phục vụ đối chiếu/hoàn tiền thủ công.

## 6. Mô phỏng trên local

- Chỉ bật khi đồng thời `Development` và cấu hình mô phỏng được bật; ngoài Development phải từ chối sử dụng nguồn mô phỏng.
- Dùng database kiểm thử riêng; không đưa giao dịch giả vào dữ liệu vận hành, không reset database của thành viên.
- Thông tin minh họa dùng `NGAN HANG TEST`, `TEST-ACCOUNT-001`, `TEST-TXN-0001`. Không tạo QR chuyển khoản thật bằng số tài khoản tự nghĩ ra.
- QR minh họa chỉ chứa dữ liệu như `DEMO:ORDER:123`; luôn ghi **“MÔ PHỎNG — không chuyển tiền thật”**.
- M4 cung cấp giao dịch thử phía server. Nếu có màn hình tạo giao dịch thử, chỉ Admin/Nhân viên hợp lệ ở Development được dùng; endpoint phải có cùng giới hạn, không chỉ ẩn nút.
- Khách không thể tự tạo giao dịch và xác nhận tiền. Nhân viên/Admin vẫn phải đối soát qua service nghiệp vụ thật; mô phỏng không bỏ quyền, validation, khóa hoặc transaction.
- Khi tắt mô phỏng và chưa cấu hình ngân hàng, trang báo chưa sẵn sàng. Không tự chuyển sang thông tin nhận tiền giả.

## 7. Giao diện và thông báo

- Tiếng Việt, dùng layout chung; không tạo lại header/menu/footer trong từng View.
- Phân biệt “Cọc cần chuyển”, “Cọc đã ghi nhận”, “Còn phải thanh toán”. Chưa đối soát không hiển thị “Đã nhận cọc”.
- Trạng thái luôn có chữ: chờ vàng, xác nhận xanh dương, hoàn thành xanh lá, hủy xám, từ chối đỏ.
- Dùng các key đã có: `TempData["Success"]`, `["Error"]`, `["Warning"]`, `["Info"]`; controller ánh xạ kết quả service sang thông báo.
- Giữ dữ liệu form khi lỗi, nạp lại dữ liệu chính thức/phiên bản đơn và yêu cầu kiểm tra lại trước khi gửi. Thông báo không lộ SQL, connection string hoặc chi tiết nội bộ.
- Hỗ trợ bàn phím/mobile; vô hiệu hóa nút lúc gửi chỉ giúp trải nghiệm, không thay chống trùng ở server.

## 8. Migration, Git và nghiệm thu

- M4 thiết kế model biên nhận cùng M1; M1 điều phối DbContext/migration/snapshot. Một người sinh migration cho cùng thay đổi schema.
- PR có đủ migration `.cs`, `.Designer.cs` và snapshot tương ứng nếu thay model. Không đưa migration ngoài phạm vi, không sửa/xóa migration đã dùng chung, không tắt cảnh báo pending model changes.
- Giữ các thay đổi đang có của thành viên; không dùng `git add .` khi workspace lẫn nhiều đầu việc.
- Nhánh `feature/mN-ten-dau-viec`; commit `[MSSV] [Module] Noi dung cong viec`. PR theo template repo, ghi phụ thuộc, thay đổi contract/schema, cách test và kết quả.
- Chỉ `Closes #...` khi hoàn thành toàn bộ Issue thực tế. Một thành viên khác review; Tùng merge bằng **Create a merge commit**.
- Bắt buộc kiểm tra: đúng/sai tiền; sai quyền/chủ đơn; tài khoản khóa; POST thiếu token; gửi lặp/đồng thời; hai đơn trùng giờ; hủy cạnh tranh; rollback; sửa giá sân; tổng/còn lại/doanh thu; tắt mô phỏng ngoài Development.
- Chạy build và test phù hợp. Migration phải được kiểm tra trên database thử riêng khi có thay đổi schema. Ghi rõ ca không chạy và lý do; kiểm thử mô phỏng không chứng minh ngân hàng/QR thật hoạt động.

Các quyết định ở mục 2–3 đã được cụ thể hóa trong hợp đồng ngày 23/10/2026. Tên model/interface mới là hợp đồng đích, không phải bằng chứng đã có code; riêng `ITinhTienService` và dịch vụ trong đơn đã có trên main mới.

# Phân công chức năng đặt cọc và chuyển khoản

Điều phối: **Trần Trọng Tùng — MSSV 23103100202 — M1**.

Nội dung thực hiện: phân công luồng đặt cọc, đối soát và xác nhận đặt sân cho các thành viên.

Quy tắc triển khai và review: [Quy ước đặt cọc và chuyển khoản](Quy_uoc_dat_coc_chuyen_khoan.md).

**Cập nhật 23/10/2026:** đã chốt [hợp đồng M3/M4/M5 v1.0](Hop_dong_M3_M4_M5.md) trên main `1a97883`. Dùng tài liệu này cho chữ ký phương thức, DTO, nguồn tiền, quyền, trạng thái và mã lỗi; không tự tạo hợp đồng khác theo các gợi ý cũ.

**Trạng thái:** code chuyển khoản/QR thử nghiệm đã được gỡ. Hợp đồng đã có để bàn giao triển khai; chưa xác nhận thành viên đã thực hiện, review hoặc nghiệm thu chức năng cọc. Code main mới đã có tính tiền M5, dịch vụ trong đơn và khả dụng M4 để tái sử dụng.

## 1. Luồng nghiệp vụ

1. Khách chọn sân, thời gian và tạo đơn. Server kiểm tra khả dụng, lấy giá từ nguồn tin cậy; đơn ở trạng thái **Chờ xác nhận**.
2. Website hiển thị tiền cọc **50% tiền sân dự kiến**, nội dung chuyển khoản gắn với mã đơn và hướng dẫn.
3. Khách chuyển cọc trước và chờ đối soát. Khách không tự đánh dấu đã nhận cọc hoặc xác nhận đặt sân.
4. Nhân viên/Admin kiểm tra giao dịch, số tiền, mã đơn và khả dụng của sân.
5. Chọn **“Xác nhận cọc và đặt sân”**: ghi biên nhận, tiền cọc và xác nhận đơn trong **cùng transaction**; lỗi thì rollback cả ba.
6. Khách xem **Đã nhận cọc — Đã xác nhận đặt sân**. Khi hoàn thành đơn, tiền cọc được trừ khỏi số còn phải thanh toán.

Tiền cọc tính từ đơn giá và thời gian lưu trên đơn, chưa gồm dịch vụ phát sinh. Quy tắc chốt: làm tròn tiền sân trước, rồi cọc 50% đến đồng bằng `AwayFromZero`; M5 thực hiện trong `ITinhTienService` và test.

**Đợt đầu kiểm thử mô phỏng trên local**, không dùng ngân hàng/QR thật, không chuyển tiền thật. Webhook, kết nối ngân hàng và hoàn tiền tự động chưa thuộc phạm vi.

## 2. Phần việc từng thành viên

### M1 — Trần Trọng Tùng: điều phối và tích hợp

- Chốt hợp đồng giữa M3/M4/M5: phương thức, dữ liệu vào/ra, quyền, trạng thái, mã lỗi và nguồn tính tiền.
- Cập nhật quy ước theo thứ tự **cọc trước, xác nhận đơn sau**.
- Cùng M4 thiết kế dữ liệu biên nhận; tích hợp DbContext, tạo migration và cập nhật snapshot nếu cần.
- Tích hợp DI/cấu hình, giới hạn chế độ mô phỏng ở Development; phụ trách layout/menu/CSS/JS chung khi cần.
- Kiểm tra toàn luồng, tổ chức review chéo và merge PR đủ điều kiện.

**Bàn giao:** hợp đồng chung, cấu hình mẫu không chứa bí mật, migration đồng bộ nếu có, hướng dẫn chạy thử và kết quả tích hợp. M1 không triển khai thay toàn bộ nghiệp vụ các module.

### M2 — Phan Giang Tâm: sân và khả dụng

- Rà soát dữ liệu sân, giá và khả dụng khi tạo đơn, mở thông tin cọc và xác nhận đặt sân.
- Cùng M3/M4 chốt cách giữ khung giờ của đơn chờ, tái sử dụng kiểm tra trùng lịch/khóa sân hiện có.
- Kiểm tra sửa giá sân không làm thay đổi đơn giá đã lưu trên đơn cũ.
- Kiểm tra hồi quy danh sách, tìm kiếm, chi tiết sân và khả dụng sau tích hợp.

**Bàn giao:** kết quả rà soát và các sửa đổi thuộc module sân nếu cần. Không bắt buộc tạo service thanh toán hoặc PR code riêng khi không có thay đổi cần thiết.

### M3 — Nguyễn Văn Quý: giao diện và luồng khách

- Sau khi tạo đơn, dẫn khách đến trang thông tin cọc của chính đơn đó.
- Hiển thị sân, thời gian, tiền cọc, nội dung chuyển khoản, hướng dẫn và trạng thái đơn/cọc riêng biệt.
- Làm QR minh họa và nhãn mô phỏng cho local; không tự tính tiền trong View/JavaScript.
- Bổ sung đường dẫn từ lịch sử/chi tiết đơn; hiển thị kết quả đối soát và biên nhận.
- Kiểm tra chủ sở hữu ở server, kể cả khi khách thay ID đơn hoặc biên nhận trên URL.
- Không cho khách tự ghi nhận đã thanh toán. Đợt này chưa cần nút “Đã chuyển khoản”; nếu thêm sau thì chỉ là thông báo chờ kiểm tra.

**Bàn giao:** controller/action khách, View/ViewModel riêng và test quyền sở hữu; giao diện dùng được trên desktop/mobile. Dùng service cọc chung của M4 và kết quả tiền từ M5.

### M4 — Phạm Thành Nghĩa: đối soát cọc và xác nhận đặt sân

- Phụ trách service nghiệp vụ cọc dùng chung và kiểm tra quyền phía server.
- Thiết kế biên nhận cùng M1: đơn, số tiền, mã giao dịch, thời điểm nhận, người/thời điểm xác nhận và dữ liệu đối chiếu cần thiết.
- Làm màn hình nhân viên/Admin xem đơn chờ, giao dịch và thao tác **“Xác nhận cọc và đặt sân”**.
- Kiểm tra số tiền, giao dịch trùng, quyền, trạng thái, phiên bản đơn, thời gian và trùng lịch trước khi ghi.
- Dùng transaction và khóa dùng chung; chống bấm lặp, gửi đồng thời và cùng giao dịch dùng cho nhiều đơn.
- Rà lại endpoint xác nhận cũ để không bỏ qua cọc đối với đơn áp dụng luồng mới.
- Cung cấp nguồn giao dịch mô phỏng ở Development; việc ghi cọc/xác nhận vẫn qua service nghiệp vụ thật.
- Tiếp nhận ngoại lệ đã có tiền nhưng không thể xác nhận sân; giữ dấu vết để đối chiếu thủ công.

**Bàn giao:** service, controller/View nghiệp vụ, model biên nhận và test transaction/concurrency/phân quyền. **M4 là đầu mối chính của chức năng nhận cọc.**

### M5 — Nguyễn Hữu Quang: tính tiền và thống kê

- Cung cấp một nguồn tính tiền sân, cọc 50%, tổng tiền và số còn phải trả.
- Khảo sát cách tái sử dụng/tách `DatSanService.TinhTienSanDuKien` hiện có; phối hợp M4 cập nhật các nơi gọi, tránh phụ thuộc vòng.
- Tính từ dữ liệu đơn trên server; không nhận giá hoặc tỷ lệ cọc từ khách.
- Kiểm thử thời lượng, làm tròn, dịch vụ phát sinh và khấu trừ cọc.
- Tích hợp tiền cọc/tiền còn lại vào hoàn thành đơn và thống kê; không cộng cọc thêm lần nữa vào tổng giá trị đơn hoặc doanh thu.

**Bàn giao:** công thức tiền chung, test, phần tích hợp thống kê liên quan. `ITinhTienService` đã có trên main từ PR #52; mở rộng đúng interface đó theo hợp đồng, không tạo một service tính tiền song song.

## 3. Quyết định đã chốt để triển khai

| Nội dung | Người phối hợp | Quyết định v1.0 — chi tiết tại hợp đồng |
| --- | --- | --- |
| Giữ khung giờ và hạn cọc | M1/M2/M3/M4 | Đơn chờ giữ chỗ, chưa tự hết hạn. Chuyển tiền và xác nhận trước giờ bắt đầu; muộn chuyển ngoại lệ. |
| Đơn cũ và đơn miễn phí | M1/M4/M5 | Chính sách 0 giữ luồng cũ; chính sách 1 áp dụng cọc cho đơn mới. Cọc yêu cầu bằng 0 không sinh biên nhận giả. |
| Hủy khi chờ đối soát | M3/M4 | Chặn hủy/từ chối khi có cọc hoặc giao dịch khớp nội dung chờ xử lý. Tiền đến sau hủy lưu ngoại lệ, không mở lại đơn. |
| Đã chuyển nhưng không thể xác nhận | M1/M4 | Lưu giao dịch trước, biên nhận chỉ sau xác nhận thành công; giữ bằng chứng ngoại lệ ngoài transaction thất bại. |
| Interface/DTO và quyền sửa file | M1/M3/M4/M5 | Áp dụng các mục 3–10 của hợp đồng; liên kết vào Issue/PR của người thực hiện. |

Transaction database không hoàn tác tiền đã chuyển trong ngân hàng. Bằng chứng giao dịch ngoại lệ cần cơ chế lưu riêng đã thống nhất, không mất theo transaction xác nhận thất bại.

## 4. Kiểm thử mô phỏng local

- Chỉ hoạt động khi **Development** và cấu hình mô phỏng cùng được bật; ngoài Development phải từ chối chế độ này.
- Dùng database kiểm thử riêng theo hướng dẫn M1; không reset database đang dùng của thành viên.
- Dữ liệu minh họa: ngân hàng `NGAN HANG TEST`, tài khoản `TEST-ACCOUNT-001`, giao dịch `TEST-TXN-0001`. Không đưa chúng vào bộ tạo QR thanh toán thật.
- QR minh họa chứa chuỗi như `DEMO:ORDER:123`; trang ghi **“MÔ PHỎNG — không chuyển tiền thật”**. Tắt mô phỏng và chưa cấu hình ngân hàng thì báo chức năng chưa sẵn sàng.
- M4 cung cấp giao dịch thử ở server: đúng tiền, sai tiền, sai nội dung, trùng mã, sai thời điểm. Khách không được gọi chức năng tạo giao dịch để tự làm đơn thành đã thanh toán.
- Nhân viên/Admin đối soát dữ liệu thử rồi chạy cùng service xác nhận; không bỏ validation, quyền, transaction hoặc kiểm tra trùng lịch.
- M3 kiểm tra hiển thị, M4 kiểm tra nghiệp vụ, M5 kiểm tra tiền, M2 kiểm tra sân, M1 kiểm tra tích hợp.

Đạt kiểm thử mô phỏng không đồng nghĩa đã kiểm thử ngân hàng hoặc QR chuyển khoản thật.

## 5. Chủ sở hữu file chung

| Nhóm file | Người sửa chính | Phối hợp |
| --- | --- | --- |
| Service cọc, controller/View đối soát | M4 | M3 gọi service chung, không viết lại nghiệp vụ cọc. |
| Controller/View/ViewModel khách | M3 | Dùng hợp đồng service và layout chung. |
| Công thức tiền và thống kê | M5 | Sửa file dùng chung phải thống nhất với M4 trước. |
| Model biên nhận | M4 | M1 review schema trước khi sinh migration. |
| DbContext, migration/snapshot, DI/cấu hình | M1 | Một người tạo migration, không sinh song song cho cùng model. |
| `IDatSanService`/`DatSanService` | M4 chủ trì thay đổi luồng xác nhận | M3/M5 review nơi gọi; M2 review kiểm tra sân. Không tạo hai bộ quy tắc khách/nhân viên. |
| Layout/menu/CSS/JS chung | M1 | Thành viên khác trao đổi trước khi sửa; tài nguyên riêng trang đặt riêng. |

Tên file/interface mới chốt trong hợp đồng; bảng không khẳng định các file cọc đã tồn tại. Nếu dùng chung controller khách/nhân viên thì M4 sửa chính, M3 phối hợp.

Migration cần đủ `.cs`, `.Designer.cs` và `ApplicationDbContextModelSnapshot.cs` tương ứng. Không đưa migration local ngoài phạm vi hoặc tắt cảnh báo pending model changes để bỏ qua chênh lệch.

## 6. Thứ tự thực hiện và PR

| Bước | Chủ trì | Kết quả |
| --- | --- | --- |
| 1. Hợp đồng | M1, cả nhóm phối hợp | Chốt mục 3; dữ liệu, interface và người sửa file rõ ràng. |
| 2. Nền dữ liệu và công thức | M1/M4 thiết kế dữ liệu; M5 tính tiền | Model/migration nhất quán, công thức có test, nền chung build được. |
| 3. Nghiệp vụ và mô phỏng | M4; M1 tích hợp cấu hình | Service đối soát/xác nhận, nguồn giao dịch mô phỏng và test hoạt động. |
| 4. Giao diện hai phía | M3 khách; M4 nhân viên/Admin | Luồng đầy đủ, gọi backend đã có, kiểm tra quyền và lỗi. |
| 5. Nghiệm thu | M1 điều phối | M2 kiểm tra sân, M3 khách, M4 cạnh tranh, M5 tiền; review chéo trước merge. |

M3 có thể chuẩn bị giao diện sau bước 1, nhưng không merge phần gọi backend chưa tồn tại. PR chia theo phần build được, ghi rõ phụ thuộc và không tạo liên kết chết trên `main`.

Lịch đề xuất: buổi 1 chốt hợp đồng; buổi 2 làm nền/nghiệp vụ; buổi 3 tích hợp giao diện/test; buổi 4 review/nghiệm thu. Đây là thứ tự phối hợp, chưa phải cam kết thời hạn của thành viên.

Nhánh đề xuất: `feature/m1-tich-hop-coc`, `feature/m3-giao-dien-coc`, `feature/m4-doi-soat-coc`, `feature/m5-tinh-tien-coc`. M2 tạo nhánh nếu cần sửa code. Tạo từ `main` mới nhất hoặc ghi rõ nhánh phụ thuộc với PR nối tiếp.

Commit: `[MSSV] [Module] Noi dung cong viec`. PR theo template repo, ghi thay đổi, cách test, migration và phụ thuộc. Chỉ ghi `Closes #...` với Issue thực tế đã hoàn thành toàn bộ. Một thành viên khác review; Tùng merge bằng **Create a merge commit**. Tài liệu này không tự tạo Issue hoặc PR.

## 7. Điều kiện nghiệm thu

| Ca kiểm tra | Kết quả yêu cầu | Phụ trách |
| --- | --- | --- |
| Cọc đúng, đúng đơn, sân khả dụng | Biên nhận, cọc và xác nhận đơn cùng thành công | M4/M5 |
| Sai tiền/nội dung/thời điểm | Không xác nhận; thông báo rõ | M4 |
| Khách đổi ID hoặc gọi POST xác nhận | Bị chặn ở server | M3/M4 |
| Nhân viên/Admin hợp lệ; tài khoản bị khóa | Đúng quyền được đối soát, tài khoản không hợp lệ bị chặn | M4/M1 |
| Gửi lặp/đồng thời, dùng lại giao dịch | Không ghi trùng tiền/biên nhận | M4 |
| Hai đơn trùng giờ hoặc xác nhận cạnh tranh với hủy | Không xác nhận trùng lịch hoặc trạng thái mâu thuẫn | M4/M2 |
| Lỗi khi đang lưu | Rollback cọc, biên nhận và trạng thái; lưu bằng chứng ngoại lệ theo hợp đồng | M4 |
| Đổi giá sân sau tạo đơn | Không tính lại đơn giá/cọc của đơn cũ | M2/M5 |
| Hoàn thành đơn có cọc và dịch vụ | Tổng/còn lại đúng; thống kê không cộng cọc hai lần | M5 |
| Mô phỏng ngoài Development | Không hoạt động, không bỏ qua phân quyền | M1/M4 |
| Desktop/mobile và các trạng thái lỗi/chờ/đã nhận | Nhãn rõ, không báo đã trả tiền khi chưa đối soát | M3/M4 |

Mỗi PR chạy `dotnet build` và test phù hợp trên code thực tế; ghi rõ phần chưa kiểm tra. Không lấy kết quả test của bản thử nghiệm đã gỡ để nghiệm thu luồng mới.

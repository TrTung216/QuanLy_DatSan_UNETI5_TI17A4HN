# M2 — Hoàn thiện luồng tra cứu và đặt sân

Phan Giang Tâm — 23103100196. Ngày 09/11/2026.

Nhánh `feature/m2-hoan-thien-luong-dat-san`, phát triển từ main `84af5a9` sau PR #70.

## Đối chiếu nhiệm vụ

| Yêu cầu M2 trong mục 6 của De_18.docx | Phần thực hiện |
| --- | --- |
| Entity sân, khóa ngoại loại sân, migration | Đã có từ các PR M2 trước. Select loại sân lấy dữ liệu qua service/repository. |
| Danh sách, chi tiết, thêm, sửa, xóa và đổi trạng thái | Đã có, dùng quyền Admin/Nhân viên. Dữ liệu có đơn tham chiếu được giữ bằng FK. |
| Validation tên, địa chỉ, tiền, loại sân, giờ phục vụ và ngày bảo trì | ViewModel kiểm tra form, service kiểm tra nghiệp vụ và M4 kiểm tra khả dụng khi giao dịch. |
| Tìm theo tên sân, tên loại sân và địa chỉ | Đã có tìm kiếm kết hợp qua repository. |
| Lọc loại, trạng thái, khoảng giá, ngày/giờ và sân trống | Đã có. Service M2 gọi M4 tuần tự theo lô tối đa 200 mã rồi lọc trước phân trang. |
| Sắp xếp tên, giá và giờ mở cửa | Đã có, thêm mã sân làm tiêu chí phụ ổn định. |
| Phân trang trên truy vấn, giữ điều kiện | Đã có Count/Skip/Take trên database, 10 sân mỗi trang. |
| Hiển thị khả dụng và liên kết giao dịch | Bổ sung thông báo khung giờ tra cứu, nút Đặt sân và chuyển tiếp sang form M3. |
| Ảnh sân theo quy ước bổ sung | PR #69 đã merge. Tải/thay/gỡ một ảnh, quyền Admin/Nhân viên và kho ảnh dùng chung của M1. |

## Phần bổ sung trong nhánh này

- Trang chi tiết có nút Đặt sân cho khách chưa đăng nhập và tài khoản Khách hàng.
- Chuyển mã sân và khoảng ngày/giờ hợp lệ từ trang chi tiết sang GET `DonDat/Tao`.
- Khách chưa đăng nhập đi qua luồng đăng nhập M1, giữ URL quay lại cùng các tham số.
- Form tạo đơn chọn sẵn sân và điền ngày/giờ. Dữ liệu chuyển tiếp thiếu/sai hoặc sân đã ẩn có thông báo để sửa.
- Có liên kết Xem lại sân, giữ ngày/giờ. Liên kết về danh sách từ chi tiết cũng giữ khoảng giờ hợp lệ.
- Kết quả tra cứu ghi rõ khung giờ đang tìm sân trống. Trường hợp tra cứu chỉ có ngày/giờ mà hết sân được hiển thị là không có sân phù hợp.
- POST tạo đơn tiếp tục qua service M4 hiện có để kiểm tra lịch dưới khóa trước khi ghi. Kết quả tra cứu chỉ có giá trị tại thời điểm đọc.

Điểm nối M3 chỉ thay GET nhận dữ liệu và View form. Header giữ thông tin người phụ trách ban đầu, bổ sung nội dung tích hợp M2.

## Giá theo hợp đồng V1

[Hợp đồng M3/M4/M5, mục 3](../Hop_dong_M3_M4_M5.md) quy định đơn dùng `DatSan.DonGia` chụp từ server khi tạo. Giá cao điểm/cuối tuần trong `GiaThueSan` M2 là ước tính tham khảo. Áp dụng nhiều khung giá lên đơn cần hợp đồng snapshot riêng của nhóm.

Danh sách, chi tiết sân và form tạo đơn đã ghi rõ phạm vi giá này. Ví dụ sân có giá thường 200.000, cao điểm 350.000 và cuối tuần 450.000 đồng/giờ thì đơn V1 một giờ vẫn chụp 200.000 đồng, kể cả cao điểm/cuối tuần. Thay giá danh mục sau đó không đổi giá đơn đã tạo. Giá/cọc/trạng thái gửi thêm từ trình duyệt không được dùng làm dữ liệu chính thức.

Nhánh này dùng Entity, DbContext và chuỗi migration hiện có.

## Kiểm tra giao diện

1. Chạy ứng dụng với database đã cập nhật theo main. Vào Danh sách sân.
2. Nhập ngày tương lai, giờ nằm trong giờ phục vụ và từ khóa của một sân đang hoạt động. Bấm tìm kiếm.
3. Bấm Xem rồi Đặt sân. Nếu chưa đăng nhập, đăng nhập bằng tài khoản Khách hàng. Kiểm tra đúng sân, ngày và giờ đã chọn.
4. Bấm Xem lại sân và về danh sách, kiểm tra khoảng giờ còn được giữ.
5. Gửi form đặt. Kiểm tra đơn mới tại lịch cá nhân, sau đó tìm lại đúng khoảng giờ để thấy sân đã bị chiếm.
6. Mở hai tab form cùng khung giờ, gửi tab đầu rồi gửi tab sau. Tab sau phải báo khung giờ đã có người đặt và giữ dữ liệu form.
7. Đăng nhập Admin/Nhân viên để kiểm tra nút Sửa/Xóa. Khách chỉ xem sân và loại sân hoạt động. Sân có lịch sử giữ thông báo chặn xóa.

## Kiểm thử tự động và nghiệm thu

`LuongDatSanTests.cs` kiểm tra chuyển tham số qua đăng nhập, liên kết quay lại, quyền, dữ liệu sai/thiếu và sân đã ẩn. `LuongDatSanSqlTests.cs` chạy HTTP thật trên database SQL Server của fixture, tạo đơn, kiểm tra snapshot giá V1 và chặn đặt lại cùng khoảng giờ.

Các fixture dùng database/thư mục riêng của test. Kiểm thử tự động chưa thay thế nghiệm thu bố cục desktop/mobile bằng trình duyệt.

Kết quả ngày 09/11/2026, chạy toàn solution Release với SQL Server tests M1/M2 được bật và instance LocalDB tạm riêng cho M4.

| Bộ test | Đạt | Lỗi | Bỏ qua |
| --- | --- | --- | --- |
| M103 | 148 | 0 | 0 |
| M104 | 52 | 0 | 0 |
| M201 | 180 | 0 | 0 |
| M4 | 245 | 0 | 0 |
| Tổng | 625 | 0 | 0 |

Bổ sung 15 ca M2 trong hai file test của nhánh này. Build thành công, có 2 cảnh báo CS0219 sẵn có ở `Views/ThongKe/Index.cshtml`. `git diff --check` đạt. Instance M4 dùng để kiểm thử đã được dọn sau khi chạy.

Issue #13 và #15 có phần triển khai và bộ test tương ứng. Issue #16 được bổ sung luồng chuyển tiếp trong nhánh này. Khi review cần đối chiếu các PR đã merge và kết quả test, cập nhật bằng chứng trước khi đóng issue. Issue #14 đã đóng. Chủ repository thực hiện review/merge theo quy ước nhóm.

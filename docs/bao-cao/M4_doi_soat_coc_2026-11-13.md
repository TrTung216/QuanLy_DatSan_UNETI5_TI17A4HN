# M4 — Giao diện đối soát cọc, 13/11/2026

## Phạm vi

- Thêm trang `QuanLyDonDat/DoiSoatCoc/{id}`, liên kết từ chi tiết quản lý; hiển thị cọc yêu cầu/đã nhận, giao dịch và biên nhận từ service.
- POST `XacNhanCoc` gọi `XacNhanCocVaDatSanAsync`, không gọi luồng ghi cọc thủ công. Chỉ nhận mã đơn, mã giao dịch, RowVersion và xác nhận đã kiểm tra; tiền và người xác nhận do service quyết định.
- POST `TaoGiaoDichMoPhong` gọi service mô phỏng hiện có. Form chỉ hiện khi service xác định Development và cấu hình mô phỏng hợp lệ. Service tiếp tục kiểm tra điều kiện này khi POST trực tiếp. Tạo giao dịch không tự xác nhận đơn.
- Các endpoint dùng quyền Admin/Nhân viên, POST có antiforgery, lỗi quyền từ service trả Forbid. Lỗi đối soát được hiển thị cùng dữ liệu mới nhất; gửi lại cùng giao dịch không ghi cọc lần hai.
- Chi tiết quản lý ẩn xác nhận thường khi đơn chính sách 1 yêu cầu cọc; ẩn ghi cọc thủ công cho chính sách mới. Đơn chính sách 1 có cọc yêu cầu bằng 0 vẫn xác nhận thường, không tạo biên nhận 0 đồng.
- Giao dịch cần xử lý ngoại lệ được hiển thị riêng; không thêm thao tác hoàn tiền hay tự mở lại đơn.
- Đơn chính sách cũ hiển thị cọc yêu cầu theo công thức 50% tiền sân từ service tính tiền, không ghi lại dữ liệu lịch sử. Đơn chính sách mới giữ snapshot cọc yêu cầu. Đơn đã nhận cọc không hiện cảnh báo chưa đủ điều kiện xác nhận.

## Ranh giới tích hợp

Không thay Controller/View khách hàng của M3, công thức tiền M5, schema/migration hoặc cấu hình ứng dụng. `TaoDonAsync` chưa bật chính sách cọc 1 trong phần bàn giao M4 riêng này. Cần tích hợp và nghiệm thu luồng tạo đơn/trang cọc M3 trước khi kích hoạt cho đơn mới. Dữ liệu đơn cũ giữ nguyên.

`DoiSoatCocDto` bổ sung thuộc tính init `CoTheMoPhong`; `ChiTietDonQuanLyDto` bổ sung `PhienBanChinhSachCoc`, `TienCocYeuCau`. Giữ constructor/chữ ký service hiện có; caller giả lập cần cung cấp các thuộc tính này khi mô phỏng đơn chính sách mới.

## Kiểm thử

Các ca HTTP mới trong `tests/QuanLyDatSan.M4.Tests/DoiSoatCocHttpTests.cs` kiểm tra: đối soát thành công và retry, không tin tiền/actor giả mạo, thiếu checkbox, phiên bản sai/cũ, sai tiền/giao dịch, quyền khách/khách vãng lai, antiforgery, mô phỏng theo môi trường/cấu hình và đơn miễn phí.

Lệnh kiểm tra cùng hồi quy quản lý hiện có:

```powershell
dotnet test tests/QuanLyDatSan.M4.Tests/QuanLyDatSan.M4.Tests.csproj --no-restore --filter "FullyQualifiedName~QuanLyDonDatTests"
```

Kết quả: **116/116 đạt**, không bỏ qua. Log local: `tests/QuanLyDatSan.M4.Tests/obj/m4-doisoat-test.log`.

Sau sửa hiển thị cọc đơn cũ: **21/21 đạt** cho ca hồi quy mới, bộ `CocTests` và ca HTTP đơn miễn phí. Log local: `tests/QuanLyDatSan.M4.Tests/obj/m4-coc-yeu-cau-test.log`.

Bộ test dùng database SQL Server LocalDB tạm riêng; không cập nhật database ứng dụng. Chưa nghiệm thu thao tác trực quan trên trình duyệt.

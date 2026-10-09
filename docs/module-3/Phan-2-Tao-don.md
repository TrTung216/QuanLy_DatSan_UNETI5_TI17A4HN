# M3 — Phần 2: Tạo đơn đặt sân và lịch đặt cá nhân

Phụ trách: Nguyễn Văn Quý — MSSV: **23103100181**.

## Chức năng
- `/DonDat/Tao`: khách chọn sân, ngày, giờ bắt đầu và kết thúc; có thể mở sẵn sân bằng `/DonDat/Tao?maSan=<mã>` (M2 dùng cho nút "Đặt sân").
- `/DonDat/Index`: lịch đặt cá nhân, lọc theo trạng thái, 10 dòng/trang, sắp xếp theo giờ bắt đầu mới nhất.
- `/DonDat/ChiTiet/<mã>`: chi tiết đơn của chính khách; đơn của người khác trả 404.

## Quy tắc khi tạo đơn (server)
- Tài khoản và hồ sơ khách phải hoạt động; sân và loại sân phải hoạt động.
- Bắt đầu ở tương lai (giờ Việt Nam qua `IDongHo`), kết thúc sau bắt đầu, cùng ngày, nằm trong giờ mở cửa, không trùng `NgayBaoTri`.
- Trùng lịch: `batDauMoi < ketThucCu && ketThucMoi > batDauCu`; `DaHuy` và `TuChoi` không chiếm chỗ.
- Kiểm tra và ghi đơn nằm trong một transaction; trước khi kiểm tra, `KhoaSanAsync` khóa dòng của sân (`UPDLOCK`). M4 cần gọi cùng hàm này khi xác nhận đơn.
- Đơn mới: `ChoXuLy`, `TienCoc = 0`, `DonGia` chụp từ giá hiện tại của sân. Form không nhận giá, cọc, trạng thái hay mã khách.
- Tiền sân dự kiến hiển thị theo công thức mục 5 (số phút / 60 × đơn giá, làm tròn đến đồng). Đây là hàm tạm `DatSanService.TinhTienSanDuKien`; thay bằng `TinhTienService` khi M5 hoàn thành.

## Chưa làm (thuộc module khác)
Hủy/xác nhận/từ chối (M4), dịch vụ đi kèm và `ChiTietDatSan` (M5), tìm sân trống (M2).

## Kiểm thử thủ công
1. Đặt khung giờ hợp lệ → vào chi tiết, trạng thái "Chờ xác nhận", cọc 0.
2. Đặt trùng, đặt chồng một phần, đặt sát nhau (18:00–19:00 rồi 19:00–20:00 phải được).
3. Giờ quá khứ, kết thúc trước bắt đầu, ngoài giờ mở cửa, ngày bảo trì, sân bảo trì/ngừng hoạt động.
4. Đổi `maSan` hoặc `id` trên URL sang đơn của khách khác → 404.
5. Đăng nhập Admin/nhân viên vào `/DonDat/Tao` → bị từ chối truy cập.
6. Hai yêu cầu đồng thời cùng sân, cùng giờ (hai trình duyệt gửi gần như cùng lúc, hoặc script) → chỉ một đơn được tạo.

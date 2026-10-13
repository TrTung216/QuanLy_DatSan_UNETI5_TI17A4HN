# M2 — Tách xử lý sân sang service theo quy ước v1.4

Phan Giang Tâm — 23103100196.

## Công việc

Chuyển truy vấn và xử lý thêm/sửa/xóa sân từ Controller sang service. Danh sách, chi tiết, form sửa/xóa, dropdown và nạp lại form lỗi đều gọi `ISanTheThaoService`. Giữ URL, quyền Admin/Nhân viên, thông báo, phân trang và cách tính giá hiện có.

Không đổi Entity, mapping hoặc migration. Không bổ sung tìm kiếm/lọc/sort hay lịch đặt trong đầu việc này. Không sửa code M1/M5 hoặc mẫu kết quả dùng chung.

## Các file chính

| File trong project | Công việc |
| --- | --- |
| Services/QuanLySan/ISanTheThaoService.cs | Khai báo các thao tác mà Controller gọi |
| Services/QuanLySan/DuLieuSan.cs | DTO ghi sân, đọc sân, phân trang và lựa chọn loại sân |
| Services/QuanLySan/KetQuaSan.cs | Mã lỗi và kết quả; ngăn thất bại rỗng hoặc đọc dữ liệu thành công khi thao tác thất bại |
| Services/QuanLySan/SanTheThaoService.cs | Truy vấn EF, kiểm tra nghiệp vụ, lưu/xóa và gọi công thức giá |
| Controllers/SanTheThaoController.cs | Kiểm tra form/quyền đầu vào, gọi service, ánh xạ lỗi và chuyển trang |
| ViewModels/SanChiTietViewModel.cs | Dữ liệu hiển thị thay Entity trong danh sách, chi tiết và xác nhận xóa |
| ViewModels/DanhSachSanViewModel.cs | Danh sách ViewModel và thông tin phân trang |
| Views/SanTheThao/Index.cshtml, Details.cshtml, Delete.cshtml | Đọc tên loại sân từ ViewModel; giữ giao diện |
| Extensions/DependencyInjection.cs | Đăng ký ISanTheThaoService/SanTheThaoService bằng AddScoped |

Đã tích hợp main `67bd37b` ngày 13/10/2026. Giữ các đăng ký M1/M5 trong DependencyInjection và thêm AddModuleSan. Program.cs dùng lời gọi AddUngDungServices có sẵn của main trước builder.Build, không có diff trong PR M2.

Service lấy quyền đọc từ `ITaiKhoanHienTai` do M1 cung cấp. Controller không nhận cờ quản lý từ URL/form. Khách chỉ xem sân và loại sân đang hoạt động; quản lý được xem các trạng thái còn lại. Các thao tác ghi và dữ liệu form quản lý được bảo vệ bởi Authorize tại Controller.

## Hợp đồng gọi

Tất cả thao tác nhận CancellationToken và truyền tới EF. Truy vấn dùng tuần tự trên một DbContext scoped. Service không nhận ViewModel, không trả Entity/IQueryable ra Controller và không thao tác ModelState/TempData.

| Phương thức | Đầu vào / kết quả |
| --- | --- |
| LayDanhSachAsync | Trang yêu cầu; trả DTO phân trang, 10 sân/trang, clamp trang ngoài phạm vi |
| LayChiTietAsync | Mã sân; trả SanDoc hoặc null nếu không tồn tại/không được xem |
| LayLuaChonAsync | Mã loại hiện tại hoặc null; trả loại hoạt động và loại hiện tại để giữ form sửa |
| KiemTraLoaiSanAsync | Mã loại được chọn nullable và mã loại hiện tại nullable; trả bool; dùng để giữ thông báo khi form còn lỗi khác |
| TaoAsync | GhiSan; thành công trả mã sân sau khi SaveChangesAsync thành công |
| SuaAsync | Mã sân và GhiSan; thành công trả mã sân sau khi lưu |
| XoaAsync | Mã sân; thành công trả mã sân đã xóa |
| TinhGiaAsync | Mã sân, ngày và giờ thuê; trả số tiền dự kiến hoặc mã lỗi |

GhiSan nhận bảng giá do người quản lý nhập, không phải tổng tiền đặt sân do khách gửi. Service kiểm tra tên/địa chỉ bắt buộc và độ dài, giá không âm/đồng nguyên/giới hạn, enum, giờ đóng sau giờ mở và loại sân hợp lệ. Service kiểm tra lại loại sân khi ghi dù Controller đã kiểm tra trước. Tiện ích, ghi chú, ngày bảo trì là nullable; hai giá khung giờ trong DTO đọc có thể null theo dữ liệu cũ.

Tính tiền dự kiến tiếp tục dùng GiaThueSan. Chỉ tính giá, chưa kiểm tra sân trống. Việc thống nhất giá chốt với M5 và lịch đặt M3/M4 là đầu việc riêng.

## Kết quả và phản hồi

KetQuaSan<T> chỉ tạo qua factory Dat/Loi. Thành công có dữ liệu; thất bại có mã lỗi khác ThanhCong và không cho đọc DuLieu. Factory từ chối mã enum không hợp lệ. Không bắt mọi exception để đổi thành lỗi nghiệp vụ.

| Thao tác | Mã lỗi / phản hồi |
| --- | --- |
| Tạo/sửa | LoaiSanKhongHopLe → lỗi tại MaLoaiSan, giữ form và dropdown |
| Tạo/sửa | DuLieuKhongHopLe → lỗi chung trên form |
| Tạo/sửa | DuLieuLienQuanThayDoi → thông báo cũ tương ứng thao tác, giữ form |
| Sửa | KhongTonTai → 404, kể cả bản ghi mất khi đang lưu |
| Xóa | KhongTonTai → 404 |
| Xóa | DangDuocThamChieu → màn hình Delete có lỗi, hướng dẫn ngừng hoạt động |
| Xóa | DaBiXoa → cảnh báo và về Index |
| Tính giá | KhongTonTai → 404; GioThueKhongHopLe → lỗi giờ thuê trên trang chi tiết |
| Thành công | Thêm/sửa về Details theo mã sân; xóa về Index; tính giá hiển thị số tiền |

Mã ngoài hợp đồng từng thao tác gây InvalidOperationException và đi qua xử lý lỗi toàn cục. Bộ test kiểm tra các phản hồi dự kiến, trường hợp mã sai hợp đồng và cấu trúc kết quả không hợp lệ.

Các lỗi SQL Server đã biết giữ cách xử lý cũ: 547 khi tạo/sửa trả lỗi dữ liệu liên quan; 547 khi xóa trả đang được tham chiếu. Xóa đồng thời giữ cảnh báo; sửa bản ghi đã mất trả 404. Schema đặt sân chưa được tích hợp trong bản local nên chưa coi đây là bằng chứng đã nghiệm thu lịch sử đặt sân.

## Kiểm thử

Kết quả sau tích hợp main ngày 13/10/2026: build toàn solution 0 warning/0 error; cả 149 test trong solution đạt, không có ca bỏ qua: M2 80/80, M1 tài khoản/service 45/45, M1 loại sân 24/24. git diff --check đạt. Log và TRX nằm trong `%TEMP%\M2-Main-20261013`. Bộ test hiện có chưa bao phủ toàn bộ chức năng M3/M4/M5.

- TrangThaiSanTests.cs: giữ các ca trạng thái, quyền đọc, phân trang và giá dự kiến; sửa thành partial để dùng chung dữ liệu thử trong M2.
- ServiceSanTests.cs: gọi service trực tiếp với dữ liệu sai; loại sân ngừng hoạt động; không để dữ liệu sai chờ lưu; CRUD/quyền/antiforgery qua HTTP; giữ form/dropdown; giá biên 18h/21h/cuối tuần; lỗi lưu ngoài dự kiến.
- PhanHoiSanTests.cs: ánh xạ lỗi ghi/xóa/giá; thất bại không báo thành công; mã ngoài hợp đồng; mô phỏng lỗi M2 trong Production kiểm tra HTTP 500, trang lỗi tiếng Việt, RequestId và log, không lộ chi tiết exception.

Lệnh chạy từ gốc repo, dùng đầu ra tạm để tránh khóa ứng dụng đang chạy:

```powershell
$m2Output = Join-Path $env:TEMP 'M2-Main-20261013'
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --no-restore "-p:OutDir=$m2Output\build\"
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx --no-build --no-restore "-p:OutDir=$m2Output\build\" --logger trx --results-directory "$m2Output\results"
```

Bộ test dùng SQLite RAM và HTTP test host. Cơ chế giả lập lỗi nằm hoàn toàn trong project test. Chưa kiểm chứng khóa/transaction/race thực tế trên SQL Server hoặc trình duyệt desktop/mobile trong đầu việc này.

Kiểm tra thủ công bổ sung: người thực hiện đã mở giao diện local, đối chiếu quyền quản lý và thử cửa sổ ẩn danh; xác nhận luồng kiểm tra đã ổn trước khi yêu cầu gửi PR. Không coi đây là kiểm thử đầy đủ trên mọi trình duyệt/mobile. Trong lúc debug từng gặp TaskCanceledException khi lưu; chưa xác định nguyên nhân hủy request từ ảnh lỗi.

Main đã bổ sung hai cột giá vào fixture M1-04; bộ test này đã đạt sau tích hợp. Các vấn đề chung còn lại là snapshot thiếu DichVu và phiên bản package test/ứng dụng khác nhau. Không sửa các phần đó trong PR M2, không tạo migration để xử lý trong đầu việc refactor.

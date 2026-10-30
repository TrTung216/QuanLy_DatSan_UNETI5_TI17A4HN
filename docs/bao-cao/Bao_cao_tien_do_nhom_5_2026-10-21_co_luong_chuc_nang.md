# BÁO CÁO TIẾN ĐỘ — QUẢN LÝ ĐẶT SÂN THỂ THAO

Đề tài 18 • Thực hành lập trình .NET • Nhóm 5 • Lớp DHTI17A4HN

Ngày báo cáo: 21/10/2026 (giờ Việt Nam)
Người tổng hợp: Trần Trọng Tùng

## 1. Kết quả chung

Nhóm đã xây dựng nền tảng ứng dụng và tích hợp luồng chính từ đăng nhập, quản lý sân, khách tạo đơn đến nhân viên tiếp nhận và xác nhận/từ chối/hủy. Các module cùng sử dụng dữ liệu và cơ chế phân quyền chung.

Hệ thống chưa hoàn thành toàn bộ đề 18. Phần cần tập trung tiếp theo là dịch vụ gắn với đơn, tính tiền và hoàn thành đơn, thống kê, cùng các thao tác khách hàng còn chưa có giao diện. Cọc/QR là kế hoạch mở rộng, chưa tính vào kết quả đã làm.

| Module | Người phụ trách | Mã sinh viên |
| --- | --- | --- |
| M1 | Trần Trọng Tùng | 23103100202 |
| M2 | Phan Giang Tâm | 23103100196 |
| M3 | Nguyễn Văn Quý | 23103100181 |
| M4 | Phạm Thành Nghĩa | 23103100308 |
| M5 | Nguyễn Hữu Quang | 23103100184 |

### Phạm vi đánh giá

Đối chiếu De_18.docx, quy ước nhóm v1.4, code và lịch sử Git tại main local, commit ce5de11 (merge PR #51). Không fetch/pull hoặc kiểm tra nhánh cá nhân trong lần lập báo cáo; không khẳng định đây là trạng thái mới nhất trên GitHub.

“Đã tích hợp”: có trên commit nền. “Local”: thay đổi chưa commit. “Service”: có xử lý nghiệp vụ nhưng chưa đủ giao diện đầu-cuối. “Kế hoạch”: chưa triển khai. Không quy đổi số test thành tỷ lệ hoàn thành đề tài.

### Công nghệ đang sử dụng

.NET SDK 10.0.401; ASP.NET Core MVC/C#; EF Core 10; SQL Server; Razor, LINQ, HTML/CSS/JavaScript; Bootstrap; Git/GitHub. Đăng nhập dùng cookie kết hợp Session. Ứng dụng hiện tham chiếu EF 10.0.10; một số project test dùng EF 10.0.12.


---

## 2. Tiến độ M1 và M2

### M1 — Tài khoản, phân quyền và phần dùng chung

Phụ trách: Trần Trọng Tùng.

- Đã tích hợp: đăng nhập/đăng xuất, Session và lấy tài khoản hiện tại; mật khẩu băm, kiểm tra tài khoản bị khóa và kiểm soát truy cập theo vai trò.

- Đã tích hợp: Admin quản lý tài khoản, vai trò/trạng thái; bảo vệ Admin hoạt động cuối cùng và thu hồi phiên khi quyền thay đổi.

- Đã tích hợp: danh sách/chi tiết/thêm/sửa/xóa có kiểm soát loại sân; kiểm tra dữ liệu trùng và trạng thái.

- Đã tích hợp: layout/menu/thông báo dùng chung; tách service M1 và gom DI; trang chủ hiển thị sân theo loại, sắp xếp nút đăng nhập/đăng ký.

Đang ở local, chưa bàn giao qua PR: tạo/đồng bộ hồ sơ khách từ quản lý tài khoản để khắc phục tài khoản khách thiếu hồ sơ; hoàn thiện menu khách/quản trị và các test liên quan. Không ghi phần này là đã merge.

Tiếp theo: review và bàn giao các sửa đổi local; kiểm tra liên kết/menu khi module khác bổ sung màn hình; điều phối tích hợp và tài liệu chung.

Minh chứng Git: PR #1, #2, #3, #38, #39, #42 và #50. File chính: TaiKhoanController, QuanLyTaiKhoanController, LoaiSanController, Services/QuanLyTaiKhoan, Services/QuanLyLoaiSan, Services/XacThuc và Views/Shared.

### M2 — Quản lý sân và tra cứu

Phụ trách: Phan Giang Tâm.

- Đã tích hợp: danh sách/chi tiết/thêm/sửa/xóa có kiểm soát sân; dữ liệu loại sân, địa chỉ, giờ hoạt động, giá và trạng thái; controller gọi service.

- Đã tích hợp: tìm theo tên sân/loại/địa chỉ, lọc loại/trạng thái/khoảng giá, sắp xếp và phân trang trên truy vấn; giới hạn dữ liệu công khai theo quyền.

- Đã tích hợp: thông tin giá thường/cao điểm/cuối tuần và tính giá tham khảo tại chi tiết sân.

Còn thiếu: màn hình tra cứu sân trống theo ngày/khung giờ kết hợp lịch đặt chưa thấy trong action Index và hợp đồng TraCuuSan hiện tại. Kiểm tra trùng khi tạo đơn đã có ở service chung, nhưng không thay thế chức năng tra cứu này.

Lưu ý minh chứng: PR #49 có tên nhánh tra cứu sân trống nhưng diff merge chỉ sửa trùng tên ViewModel; không coi tên nhánh là bằng chứng chức năng hoàn tất. M2/M5 cần thống nhất giá hiển thị, giá lưu trên đơn và công thức chốt tiền.

Minh chứng Git: PR #4, #36, #40, #43, #48, #49. File chính: SanTheThaoController, Services/QuanLySan, GiaThueSan, Views/SanTheThao.


---

## 3. Tiến độ M3 và M4

### M3 — Khách hàng, hồ sơ và đặt sân

Phụ trách: Nguyễn Văn Quý.

- Đã tích hợp: đăng ký tài khoản khách kèm hồ sơ; xem/sửa hồ sơ cá nhân, kiểm tra dữ liệu và đồng bộ họ tên/email.

- Đã tích hợp: tạo đơn từ dữ liệu server, kiểm tra khách/sân/thời gian và trùng lịch; xem danh sách và chi tiết đơn cá nhân, giới hạn theo chủ sở hữu.

- Đã tích hợp: giao diện khách và trang điều hành quản trị cơ bản. Các phần dùng chung có phối hợp M1/M4, không quy toàn bộ đóng góp cho một người.

Còn thiếu: KhachHangHuyDonAsync đã có trong service chung nhưng DonDatController và View chi tiết chưa có action/nút khách tự hủy. Chưa thấy màn hình quản lý danh sách khách cho nhân viên/Admin. Chi tiết dịch vụ theo đơn còn phụ thuộc M5.

Cần hoàn thiện quy ước: HoSoController đã dùng service; KhachHangController (đăng ký/trang chủ) và QuanTriController vẫn truy cập DbContext. Cần tiếp tục tách nghiệp vụ/query thay vì kết luận toàn bộ M3 đã hoàn tất refactor.

Minh chứng: PR #44 và code DonDatController, HoSoController, KhachHangController, Services/QuanLyKhachHang, Services/DatSan; các thay đổi M3/M4 dùng chung được ghi nhận trong header file.

### M4 — Tiếp nhận và xử lý đơn

Phụ trách: Phạm Thành Nghĩa.

- Đã tích hợp: danh sách và chi tiết đơn cho Admin/Nhân viên; tìm kiếm, lọc loại sân/trạng thái/ngày, sắp xếp và phân trang.

- Đã tích hợp: POST xác nhận, từ chối, nhân viên hủy; kiểm tra lý do, trạng thái, phiên bản đơn và anti-forgery.

- Đã tích hợp: service dùng chung M3–M4, khóa theo sân trong transaction, kiểm tra lịch giao nhau và xử lý xung đột cập nhật.

Đã có ở service: khách hủy và HoanThanhDonAsync với kiểm tra trạng thái/thời gian/tiền. Chưa có action/View hoàn thành đơn cho nhân viên trong QuanLyDonDatController; còn cần nối nguồn tính tiền M5 và giao diện thu đủ/chốt đơn.

Tiếp theo: hoàn thiện luồng hoàn thành cùng M5; hỗ trợ M3 nối thao tác khách hủy; sau khi chốt hợp đồng mới triển khai đối soát cọc. Không báo đã có màn hình nhận cọc/QR.

Minh chứng Git: PR #47, #51; QuanLyDonDatController, DatSanService.XuLy.cs, DatSanService.DocQuanLy.cs và tests/QuanLyDatSan.M4.Tests.


---

## 4. Tiến độ M5 và mức tích hợp

### M5 — Dịch vụ, tính tiền, Dashboard và thống kê

Phụ trách: Nguyễn Hữu Quang.

Đã tích hợp: danh mục dịch vụ dành cho Admin; danh sách, tìm/lọc/sắp xếp/phân trang, thêm/sửa và đổi trạng thái hoạt động. Có kiểm tra dữ liệu form và quyền tại controller.

Đã có IDichVuService/DichVuService và đăng ký DI, nhưng DichVuController hiện vẫn inject DbContext và tự truy vấn/lưu dữ liệu. Cần chuyển controller thực sự sang service để đạt quy ước phân tầng.

Chưa thấy triển khai đầy đủ ChiTietDatSan gắn dịch vụ vào đơn, nguồn tính tiền/chốt đơn dùng chung, Dashboard và các truy vấn thống kê theo đề. Đây là nhóm chức năng cần ưu tiên để hoàn thiện luồng nghiệp vụ bắt buộc.

Trang QuanTri hiện có 3 chỉ số lấy từ database: đơn chờ chưa quá giờ kết thúc, khách hoạt động, sân hoạt động. Phần này có ghi nhận M3; không đồng nghĩa Dashboard M5 đã đủ yêu cầu. “Sân hoạt động” cũng không đồng nghĩa “sân trống trong khung giờ”.

Minh chứng Git: PR #37, #41. File chính: DichVuController, Services/QuanLyDichVu, Views/DichVu; đối chiếu QuanTriController và Views/QuanTri/Index.

### Bảng đối chiếu đầu ra theo đề

| Nhóm chức năng | Hiện trạng trên code đã kiểm tra |
| --- | --- |
| Tài khoản / loại sân | Đã tích hợp chức năng chính; sửa hồ sơ và menu M1 còn local. |
| Sân / tra cứu | CRUD, lọc/sort/phân trang đã có; tra cứu trống theo giờ còn thiếu. |
| Khách / đặt sân / lịch sử | Luồng chính đã có; tự hủy chưa nối giao diện, quản lý khách còn thiếu. |
| Xác nhận / từ chối / hủy | Đã có giao diện nghiệp vụ; hoàn thành đơn mới ở service. |
| Dịch vụ trên đơn / thanh toán | Danh mục đã có; chi tiết dịch vụ và tích hợp chốt tiền chưa đủ. |
| Dashboard / thống kê LINQ | Có số liệu cơ bản; chưa đủ doanh thu, lượt đặt, sử dụng sân/dịch vụ. |
| Cọc 50% / chuyển khoản QR | Mới có phân công và quy ước. Bản thử nghiệm đã gỡ. |

### Nhận xét tổng thể

Nhóm đã vượt giai đoạn khởi tạo và CRUD rời rạc, có luồng tiếp nhận đơn và bảo vệ cập nhật đồng thời. Tuy nhiên, khâu kết thúc giao dịch và khai thác số liệu chưa khép kín; chưa đủ cơ sở báo hoàn thành toàn bộ đề 18.


---

## 5. Kết quả kiểm tra ngày 21/10/2026

Kiểm tra trên working tree local gồm commit nền ce5de11 và các thay đổi M1 chưa commit. Các kết quả sau không phải kết quả riêng của một bản main sạch.

| Bộ kiểm tra | Đạt / tổng | Phạm vi |
| --- | --- | --- |
| Build solution Release | 0 lỗi; 0 cảnh báo | Biên dịch ứng dụng và 4 project test. |
| M103 | 75 / 75 | Tài khoản, service, hồ sơ, trang chủ/menu và HTTP liên quan. |
| M104 | 24 / 24 | Quản lý loại sân. |
| M201 | 115 / 115 | Quản lý/tra cứu sân; đã bật 7 ca SQL Server. |
| M4 | 114 / 114 | Nghiệp vụ đơn, HTTP, quyền và cập nhật đồng thời. |
| Tổng test .NET | 328 / 328 | 0 lỗi, 0 bỏ qua trong lần chạy đầy đủ. |
| EF pending model changes | Không có | Model hiện tại khớp snapshot/migration. |

### Cách kiểm tra và giới hạn

- Build: dotnet build solution với -c Release --no-restore. Test: dotnet test solution với -c Release --no-build --no-restore; bật QLDS_M2_TEST_SQLSERVER=1.

- Lần chạy sandbox ban đầu lỗi kết nối SQL Server LocalDB. Chạy lại ngoài sandbox trên database test riêng đạt đủ 328 ca; không coi lỗi môi trường ban đầu là lỗi nghiệp vụ.

- Test SQL dùng database có tên ngẫu nhiên và dọn database của test; không chạy Update-Database/reset database ứng dụng.

- Không có project test riêng M3/M5 trong solution; một phần luồng M3 được kiểm tra qua bộ M4/M103. Số test đạt không chứng minh toàn bộ module đã hoàn thiện.

- Chưa chạy lại kiểm thử trình duyệt desktop/mobile trong đợt báo cáo này. Test JavaScript M4 chưa chạy lại do Node không có trong PATH.

- Không kiểm chứng số lượng dữ liệu mẫu theo đề, tài khoản demo thực, ngân hàng/QR thật, hoặc trạng thái Issue/PR mới nhất trên GitHub. Build không cảnh báo không thay thế kiểm tra bảo mật package.

### Minh chứng lưu tại local

Log: QuanLyDatSan_UNETI5_DHTI17A4HN/obj/bao-cao-test-20261021-full.log. Kết quả TRX: obj/bao-cao-nhom-20261021-full/. Tóm tắt bằng chứng được lưu kèm báo cáo dưới dạng JSON; các thư mục obj không dùng để commit.

Phân biệt rõ: kiểm thử hiện tại xác nhận hành vi đã có, không nghiệm thu chức năng cọc/QR đã gỡ hoặc các màn hình còn chưa triển khai.


---

## 6. Kế hoạch tiếp theo và phối hợp

| Ưu tiên | Đầu việc | Phụ trách |
| --- | --- | --- |
| 1 — Tích hợp ổn định | Review/PR sửa hồ sơ thiếu và menu local; thống nhất interface và công thức tiền. | M1 + M3/M4/M5 |
| 2 — Khép kín đơn | Chi tiết dịch vụ trên đơn, tính tiền; nối hoàn thành/thu đủ và lịch sử kết quả. | M5 + M4 + M3 |
| 3 — Hoàn thiện thao tác | Khách tự hủy; quản lý khách; tra cứu sân trống theo ngày/giờ. | M3 + M2/M4 |
| 4 — Đủ yêu cầu đề | Dashboard, doanh thu, lượt đặt/hủy, dịch vụ phổ biến, tỷ lệ sử dụng; kiểm tra dữ liệu mẫu. | M5 + cả nhóm |
| 5 — Chuẩn hóa | Tách controller còn truy vấn DB, chạy hồi quy, chuẩn bị demo trên môi trường thống nhất. | M3/M5; M1 điều phối |
| 6 — Mở rộng cọc | Chốt hợp đồng trước, rồi triển khai mô phỏng cọc/chuyển khoản. | M4 chủ trì; M1/M2/M3/M5 phối hợp |

Thứ tự ưu tiên là đề xuất dựa trên phần thiếu của đề, không phải thời hạn nhóm đã cam kết. Các bước có thể làm song song sau khi thống nhất hợp đồng; phần bắt buộc của đề được ưu tiên trước QR.

### Phạm vi cọc/chuyển khoản đã thống nhất

Khách tạo đơn → chuyển cọc 50% tiền sân → nhân viên/Admin đối soát → ghi cọc và xác nhận đặt sân cùng transaction. Giai đoạn đầu mô phỏng ở Development, không chuyển tiền thật.

M3 làm phần khách; M4 làm đối soát và giao dịch mô phỏng; M5 tính tiền; M2 hỗ trợ khả dụng/giữ giờ; M1 phụ trách hợp đồng, dữ liệu và tích hợp. Đây là kế hoạch, chưa có tính năng để demo.

### Các điểm nhóm cần chốt

- Giá tham khảo theo giờ cao điểm/cuối tuần của M2 phải thống nhất với đơn giá và công thức chốt tiền M3/M5.

- Đơn chờ đang giữ chỗ, chưa tự hết hạn theo quy ước nền. Không tự thêm hạn cọc khi nhóm chưa thống nhất.

- Quy tắc đơn cũ, đơn miễn phí, hủy khi chưa đối soát và xử lý tiền đến nhưng không thể nhận sân phải có hợp đồng rõ.

- Mỗi file/phương thức chung có một người sửa chính; review chéo, giữ lịch sử đóng góp, migration và snapshot đồng bộ.


---

## 7. Kịch bản trình bày với giảng viên

Thời lượng đề xuất: 5–7 phút. Trước buổi trình bày cần chạy thử với dữ liệu và tài khoản demo hợp lệ; không tự nhận đã demo thành công từ kết quả test tự động.

| Thời lượng | Người trình bày | Nội dung |
| --- | --- | --- |
| 45 giây | Tùng — M1 | Giới thiệu đề tài, ba vai trò, nền tảng công nghệ và trạng thái tích hợp. |
| 60 giây | Tùng / Tâm | Đăng nhập; tài khoản/loại sân; danh sách sân và lọc/sắp xếp. |
| 60 giây | Quý — M3 | Hồ sơ, tạo đơn hợp lệ và lịch sử/chi tiết đúng chủ sở hữu. |
| 60 giây | Nghĩa — M4 | Đăng nhập nhân viên; tìm đơn, xác nhận hoặc từ chối/hủy trên các đơn thử phù hợp. |
| 45 giây | Quang — M5 | Danh mục dịch vụ; giải thích phần dịch vụ trên đơn, tính tiền và thống kê đang làm tiếp. |
| 60–90 giây | Tùng + cả nhóm | Kết quả 328 test; giới hạn, ưu tiên tiếp theo và kế hoạch cọc mô phỏng. |

### Lời trình bày gợi ý

“Thưa cô, nhóm 5 đã tích hợp được nền tảng tài khoản, dữ liệu sân và luồng khách đặt sân đến nhân viên xử lý đơn. Các chức năng dùng chung dữ liệu, có phân quyền và kiểm tra trùng lịch. Trong lần kiểm tra ngày 21/10, bản local build thành công và 328 ca kiểm thử tự động đều đạt. Nhóm chưa báo hoàn thành toàn bộ đề: dịch vụ trên đơn, tính tiền/hoàn thành và thống kê là phần cần ưu tiên; một số thao tác khách còn phải nối giao diện. Nhóm đã chia rõ trách nhiệm để hoàn thiện các phần này trước khi mở rộng cọc/chuyển khoản mô phỏng.”

### Nguồn đối chiếu và minh chứng

- Đề bài: De_18.docx, các mục 2 và 5–9. Quy ước nhóm: Quy_uoc_nhom_Quan_ly_dat_san_v1.4.md (13/10/2026).

- Repository: github.com/TrTung216/QuanLy_DatSan_UNETI5_TI17A4HN. Mốc local: ce5de11, merge PR #51; thông tin PR trong báo cáo lấy từ lịch sử Git local.

- Tài liệu trong repo: docs/module-1, docs/module-2, docs/module-3, docs/module-4 và docs/Huong_dan_su_dung_layout.md. Tài liệu cũ được đối chiếu lại với code, không tự coi mọi mô tả là hiện trạng.

- Kế hoạch mới: docs/Phan_cong_chuyen_khoan_QR.md và docs/Quy_uoc_dat_coc_chuyen_khoan.md. Không dùng kế hoạch làm bằng chứng đã hoàn thành.

- Người tổng hợp: Trần Trọng Tùng; Codex hỗ trợ rà soát code, chạy kiểm thử và soạn báo cáo. Mỗi thành viên xác nhận phần việc thực tế và giải thích được code mình phụ trách.


---

## 8. Quy trình các chức năng đã có

Phần này mô tả hành vi thực tế của code đã kiểm tra, dùng để giải thích và trình diễn. Luồng cọc 50% trong kế hoạch chưa thay thế luồng đặt sân hiện tại. Mỗi chức năng nêu người thao tác, các bước, kết quả và nhánh lỗi.

### 8.1. Luồng liên kết giữa các module

| Bước | Người thao tác / module | Kết quả hiện tại |
| --- | --- | --- |
| 1. Chuẩn bị dữ liệu | Admin: tài khoản, loại sân, dịch vụ; Admin/Nhân viên: sân — M1/M2/M5 | Có dữ liệu để tra cứu và tạo đơn. Dịch vụ mới ở danh mục. |
| 2. Vào hệ thống | Khách đăng ký/đăng nhập, xem hồ sơ — M1/M3 | Tài khoản và hồ sơ được xác định từ phiên. |
| 3. Tìm sân | Khách tra cứu danh sách/chi tiết sân — M2 | Lọc theo loại, giá, tên; chưa có tra cứu trống theo khung giờ. |
| 4. Tạo đơn | Khách chọn sân/ngày/giờ — M3, service chung M3/M4 | Server kiểm tra trùng và ghi đơn ChoXuLy; TienCoc = 0. |
| 5. Tiếp nhận | Nhân viên/Admin tìm và mở đơn — M4 | Đọc thông tin đơn, sân, khách, tiền dự kiến và phiên bản. |
| 6. Xử lý | Nhân viên/Admin xác nhận, từ chối hoặc hủy hợp lệ — M4 | Đổi trạng thái; lưu thời điểm/lý do tương ứng. |
| 7. Theo dõi | Khách mở lịch sử/chi tiết của mình — M3 | Thấy trạng thái mới và thông tin kết quả đã có. |

### Điểm dừng của luồng hiện tại

Hoàn thành đơn và khách tự hủy mới có service, chưa có màn hình/action đầy đủ. Gắn dịch vụ vào đơn, thu tiền/chốt tiền, Dashboard đầy đủ và QR chưa phải luồng có thể trình diễn đầu-cuối.

### Sơ đồ trạng thái theo mức triển khai

| Chuyển trạng thái | Mức triển khai |
| --- | --- |
| Chờ xử lý → Đang xử lý | Đã có giao diện xác nhận của nhân viên/Admin. |
| Chờ xử lý → Từ chối | Đã có giao diện; cần lý do, chưa nhận cọc. |
| Chờ/Đang xử lý → Đã hủy | Đã có giao diện nhân viên; chưa bắt đầu và chưa nhận cọc. |
| Chờ xử lý → Đã hủy bởi khách | Service đã có; chưa có action/nút khách hủy. |
| Đang xử lý → Hoàn thành | Service đã có; cần giao diện và nguồn tính tiền M5. |

Các bước dưới đây là mô tả code, không thay thế kết quả kiểm thử trình duyệt. Những nhánh lỗi chưa thực hiện demo phải được ghi nhận đúng khi trình bày.


---

## 8.2. Luồng M1 — Tùng

### A. Đăng nhập, phân quyền và tài khoản hiện tại

Người dùng mở Đăng nhập → nhập tên/mật khẩu → form gửi POST có anti-forgery → service kiểm tra tài khoản, mật khẩu băm và trạng thái → tạo cookie/Session → chuyển đến trang khách hoặc trang điều hành; nếu có URL trở lại nội bộ hợp lệ thì ưu tiên URL đó.

Sai thông tin hoặc tài khoản khóa: giữ trang và báo lỗi chung. Request sau đăng nhập tiếp tục được kiểm tra phiên/quyền; truy cập chức năng không được phép bị chặn. Trang Thông tin tài khoản đọc dữ liệu người đang đăng nhập, không cho nhập mã người khác.

### B. Đăng xuất

Người đã đăng nhập chọn Đăng xuất → gửi POST có token → xóa Session và cookie → về Đăng nhập. Khi mở lại URL yêu cầu quyền phải đăng nhập lại; request thiếu token không được thực hiện.

### C. Quản lý tài khoản — chỉ Admin

Admin mở danh sách → tìm/lọc vai trò, trạng thái và chuyển trang → chọn tạo hoặc sửa → nhập thông tin → service kiểm tra dữ liệu và lưu → thông báo thành công, về danh sách. Tạo mới phải kiểm tra tên đăng nhập trùng và băm mật khẩu; sửa không nhận đổi tên đăng nhập/mật khẩu từ form này.

Khi thay vai trò/trạng thái, hệ thống chặn tự khóa/hạ quyền và chặn mất Admin hoạt động cuối cùng. Thay đổi quyền hợp lệ làm phiên cũ bị thu hồi. Sai dữ liệu/xung đột: không báo lưu thành công, hiển thị lỗi để kiểm tra lại.

Bổ sung LOCAL: Admin tạo khách hoặc sửa tài khoản khách thiếu hồ sơ phải cung cấp điện thoại hợp lệ; lưu tài khoản/hồ sơ cùng transaction. Khi hồ sơ đã có, đồng bộ họ tên/email, giữ dữ liệu hồ sơ khác và lịch sử. Phần này chưa merge.

### D. Quản lý loại sân — chỉ Admin

Mở danh sách → tìm/lọc/sắp xếp/phân trang hoặc xem chi tiết → tạo/sửa tên, mô tả, sức chứa, giá và trạng thái → kiểm tra dữ liệu, tên trùng → lưu. Chỉnh loại sân không tự cập nhật giá/trạng thái của các sân đang tham chiếu.

Xóa: mở xác nhận xóa → POST → kiểm tra sân tham chiếu và ràng buộc database → chỉ xóa khi được phép. Nếu đã có sân sử dụng loại đó, báo không thể xóa; có thể sửa trạng thái theo nhu cầu quản lý.

### E. Trang chủ, menu và thông báo chung

Mở trang chủ → service đọc sân theo loại → View hiển thị dữ liệu thật. Header/menu chọn mục theo vai trò, đánh dấu trang hiện tại; nội dung module được render trong layout. Thông báo và section CSS/JS dùng chung; ẩn menu không thay kiểm tra quyền server.

Nguồn: TaiKhoanController, QuanLyTaiKhoanController, LoaiSanController, TrangChuService, Views/Shared. Hoàn thiện menu và đồng bộ hồ sơ được đánh dấu local trong phần tiến độ.


---

## 8.3. Luồng M2 — Tâm

### A. Tìm kiếm, lọc, sắp xếp và phân trang

Khách/nhân viên/Admin mở danh sách sân → nhập từ khóa, loại, khoảng giá và cách sắp xếp → gửi GET → service giới hạn dữ liệu theo vai trò trước, áp dụng bộ lọc rồi đếm/sắp xếp/Skip–Take → hiển thị tối đa 10 dòng mỗi trang.

Khách chỉ thấy sân và loại sân hoạt động. Admin/Nhân viên có thêm dữ liệu quản lý, lọc trạng thái. Điều kiện giá sai hoặc sắp xếp không hợp lệ được báo lỗi; không có kết quả thì hiển thị trạng thái rỗng. Chuyển trang giữ các điều kiện tra cứu.

### B. Chi tiết sân và giá tham khảo

Chọn một sân → server tìm sân trong phạm vi người dùng được xem → hiển thị địa chỉ, tiện ích, loại, giờ hoạt động và các mức giá. Nếu nhập ngày/giờ tính giá, service kiểm tra khung giờ rồi dùng GiaThueSan tính giá tham khảo.

Không tồn tại/không được xem: không trả chi tiết sân. Thiếu ngày/giờ hoặc ngoài giờ phục vụ: báo lỗi. Giá tham khảo chưa phải bằng chứng sân còn trống; công thức này còn cần thống nhất với giá lưu trên đơn và chốt tiền.

### C. Thêm/sửa sân — Admin hoặc Nhân viên

Mở form → chọn loại sân từ database, nhập tên/địa chỉ/giờ/giá/trạng thái và thông tin khác → POST có token → kiểm tra ModelState và service → lưu → trở về màn hình kết quả theo controller.

Service kiểm tra loại sân tồn tại và điều kiện hoạt động, giờ đóng lớn hơn giờ mở, giá không âm và theo đồng nguyên. Sai dữ liệu: giữ form và nạp lại danh sách loại. Sửa cho phép giữ loại hiện tại theo quy tắc service; cần rà soát thêm bảo toàn lịch sử nếu đổi loại sân đã có đơn.

### D. Xóa và quản lý trạng thái sân

Xóa: mở trang xác nhận → POST → thử xóa qua service → database kiểm soát khóa ngoại. Có dữ liệu tham chiếu: giữ sân và trả lỗi; không xóa dây chuyền lịch sử đặt sân. Trạng thái được quản lý trong form sửa, không đồng nghĩa với “trống/đã đặt” tại một khung giờ.

### Giới hạn khi trình diễn

Đã có tra cứu danh mục sân. Chưa có luồng lọc sân trống theo ngày/giờ trong danh sách hiện tại; không mô tả chức năng này là đã hoàn thành. Khả dụng và trùng lịch được kiểm tra lại khi khách tạo đơn ở M3/M4.

Nguồn: SanTheThaoController, Services/QuanLySan/SanTheThaoService.cs, TraCuuSan.cs, GiaThueSan và Views/SanTheThao.


---

## 8.4. Luồng M3 — Quý

### A. Đăng ký tài khoản khách

Khách chưa đăng nhập mở Đăng ký → nhập tài khoản/mật khẩu và thông tin cá nhân → POST có token → kiểm tra form, tên đăng nhập trùng → băm mật khẩu → tạo TaiKhoan gắn KhachHang trong cùng lần lưu → thông báo thành công và chuyển Đăng nhập.

Vai trò KhachHang, trạng thái, ngày đăng ký và điểm ban đầu do server đặt. Form lỗi hoặc tên đã dùng: quay lại form; không tự cấp quyền nhân viên/Admin theo dữ liệu gửi từ trình duyệt.

### B. Xem và sửa hồ sơ

Khách đăng nhập → mở Hồ sơ → lấy mã tài khoản từ phiên → tìm hồ sơ tương ứng → xem hoặc sửa dữ liệu cho phép → POST → kiểm tra dữ liệu và trạng thái hồ sơ → cập nhật hồ sơ cùng họ tên/email tài khoản → hiển thị kết quả.

Không nhận mã khách cần sửa từ form để chọn tùy ý. Hồ sơ bị khóa, không có hoặc ngày sinh sai bị chặn. Trường hợp Admin tạo tài khoản thiếu hồ sơ có bản sửa M1 ở local, chưa coi đã tích hợp vào main sạch.

### C. Tạo đơn đặt sân

Khách chọn sân/ngày/giờ → gửi form → controller lấy mã tài khoản từ phiên → service kiểm tra khách và thời gian → mở transaction, khóa sân → đọc lại điều kiện khách/sân/thời gian sau khi chờ khóa → kiểm tra giờ mở cửa, bảo trì và trùng lịch → ghi đơn rồi commit.

Kết quả: đơn ChoXuLy, tiền cọc 0, ngày tạo và đơn giá lấy từ server → chuyển chi tiết đơn và thông báo đang chờ nhân viên xác nhận. Không phải xác nhận đặt sân đã hoàn tất; chưa có bước chuyển khoản/QR.

Sân không nhận đặt, khách không hoạt động, giờ sai/quá khứ, ngoài giờ hoặc trùng lịch: trả mã lỗi, hiển thị form với thông báo phù hợp. Hai yêu cầu cùng sân dùng cơ chế khóa chung để kiểm tra và ghi nhất quán.

### D. Lịch sử, chi tiết và trang chủ khách

Khách mở Lịch của tôi → truy vấn chỉ đơn của tài khoản hiện tại → lọc trạng thái/phân trang → chọn chi tiết → hiển thị sân, thời gian, trạng thái, tiền dự kiến hoặc tiền đã chốt và lý do/mốc xử lý nếu có. Đổi ID sang đơn người khác không lấy được dữ liệu.

Trang chủ khách đọc hồ sơ, số đơn sắp tới và các đơn gần nhất của chính khách. Nếu không có hồ sơ, thông báo và chuyển về thông tin tài khoản.

Khách tự hủy: đã có service kiểm tra chủ sở hữu/đơn chờ/chưa bắt đầu/chưa có cọc/lý do và phiên bản; chưa có action/nút khách hủy nên không trình diễn như luồng đã hoàn chỉnh.

Nguồn: KhachHangController, HoSoController, DonDatController, KhachHangService và DatSanService dùng chung.


---

## 8.5. Luồng M4 — Nghĩa

### A. Tiếp nhận và đọc đơn

Nhân viên/Admin đăng nhập → mở danh sách đơn → tìm khách/sân, lọc loại/trạng thái/khoảng ngày và sắp xếp/phân trang → chọn chi tiết → service trả thông tin khách, sân, đơn, tiền và phiên bản; View hiện nút phù hợp trạng thái.

Khách không được truy cập controller quản lý. Mã đơn không tồn tại trả 404; dữ liệu lọc không hợp lệ được báo trên form. Nút bị ẩn không thay thế kiểm tra phía server.

### B. Xác nhận đơn

Tại đơn chờ, chọn Xác nhận → POST mã đơn + phiên bản + token → service mở transaction và khóa sân → đọc lại đơn/kiểm tra RowVersion → kiểm tra còn chờ, chưa đến giờ, sân/loại hoạt động, đúng giờ, không bảo trì và không trùng đơn khác → chuyển DangXuLy, ghi NgayXacNhan → commit và hiển thị lại chi tiết.

Sai điều kiện hoặc phiên bản cũ: không xác nhận, báo lỗi và nạp lại trạng thái/phiên bản để người dùng kiểm tra. Luồng đang có chưa yêu cầu nhận cọc 50%; yêu cầu đó mới nằm trong kế hoạch.

### C. Từ chối đơn chờ

Nhập lý do → POST → kiểm tra lý do hợp lệ, phiên bản, đơn đang ChoXuLy và chưa nhận cọc → lưu TuChoi, NgayHuy và LyDoHuy trong transaction → hiển thị kết quả. Lý do thiếu/sai hoặc đơn đã đổi trạng thái thì không áp dụng; giữ lý do nhập để sửa.

### D. Nhân viên/Admin hủy đơn

Nhập lý do hủy tại đơn → POST → kiểm tra phiên bản, đơn ChoXuLy hoặc DangXuLy, chưa tới giờ bắt đầu và chưa nhận cọc → lưu DaHuy cùng thời điểm/lý do → commit. Đơn đã có cọc hoặc ngoài điều kiện bị chặn; không xóa đơn hay lịch sử.

### E. Hoàn thành đơn — hiện mới có service

HoanThanhDonAsync nhận số tiền từ nguồn nội bộ tin cậy, kiểm tra đã thu đủ, tiền không âm → khóa/kiểm tra phiên bản → đơn phải DangXuLy, đã đến giờ kết thúc, tổng không thấp hơn cọc → lưu tiền sân/dịch vụ/tổng, NgayHoanThanh và HoanThanh cùng transaction.

Chưa có action/View hoàn thành và chưa nối đầy đủ nguồn tính tiền M5. Đây là nền nghiệp vụ đã viết và có test, chưa phải chức năng người dùng thao tác được qua website.

Nguồn: QuanLyDonDatController, Services/DatSan/DatSanService.XuLy.cs, DatSanService.DocQuanLy.cs. Khi xung đột, hệ thống trả lỗi thay vì ghi đè trạng thái mới.


---

## 8.6. Luồng M5 và số liệu hiện có

### A. Tra cứu danh mục dịch vụ — chỉ Admin

Admin mở Dịch vụ → nhập từ khóa/trạng thái/sắp xếp → controller truy vấn database, đếm và phân trang 10 dòng → hiển thị danh sách. Nhân viên/khách bị chặn theo quyền controller.

Lưu ý kiến trúc: controller hiện còn truy cập DbContext dù đã có service đăng ký DI. Luồng mô tả đúng code hiện tại; cần refactor trước khi tuyên bố đã đáp ứng toàn bộ quy ước service.

### B. Thêm và sửa dịch vụ

Admin chọn Thêm/Sửa → nhập tên, đơn vị, giá, mô tả → POST có token → kiểm tra form và giá theo đồng nguyên → tạo mới hoặc tải dịch vụ cần sửa → lưu → thông báo thành công và về danh sách.

Trạng thái khi tạo mới do server đặt là hoạt động. Mã sửa không tồn tại trả 404; dữ liệu không hợp lệ quay lại form. Chưa mô tả kiểm tra tên trùng là đã có vì controller hiện không thực hiện bước đó.

### C. Đổi trạng thái dịch vụ

Admin chọn đổi trạng thái → POST mã dịch vụ + token → tải bản ghi → chuyển HoatDong/NgungHoatDong → lưu → thông báo. Không có xóa cứng trong controller danh mục dịch vụ hiện tại.

### D. Trang điều hành với số liệu cơ bản

Admin/Nhân viên mở trang điều hành → QuanTriController đếm đơn chờ chưa quá giờ kết thúc, khách hoạt động và sân hoạt động → hiển thị ba chỉ số và các liên kết chức năng theo vai trò. Đây là số liệu thật từ database, có ghi nhận đóng góp M3.

Không dùng ba chỉ số này để báo Dashboard M5 đã hoàn chỉnh. Chưa có đầy đủ doanh thu theo tháng/loại, lượt đặt/hủy, dịch vụ phổ biến hay tỷ lệ sử dụng; sân hoạt động không đồng nghĩa sân còn trống trong khung giờ.

### Phần chưa có luồng hoàn chỉnh

| Chức năng | Cách trình bày đúng hiện trạng |
| --- | --- |
| Thêm dịch vụ vào đơn | Chưa có đầy đủ ChiTietDatSan và giao diện gắn dịch vụ. |
| Tính tiền / thu đủ / hoàn thành | Có nền xử lý hoàn thành ở M4; còn cần M5 cung cấp tiền và giao diện tích hợp. |
| Cọc 50% / QR | Mới có quy ước và phân công; bản thử nghiệm đã gỡ. |
| Dashboard / thống kê theo đề | Cần bổ sung truy vấn và màn hình, không suy từ danh mục dịch vụ. |

Nguồn: DichVuController, Views/DichVu, Services/QuanLyDichVu; QuanTriController và Views/QuanTri/Index. Các luồng ở mục 8 được đối chiếu code ngày 21/10/2026; không thay đổi kết quả kiểm thử hoặc phạm vi đã ghi ở mục 5.

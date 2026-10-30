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

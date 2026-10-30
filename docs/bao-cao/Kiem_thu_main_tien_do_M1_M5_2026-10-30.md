# Kiểm thử main và tiến độ M1–M5 — 30/10/2026

Thực hiện: Codex theo yêu cầu của Trần Trọng Tùng. Đây là báo cáo kiểm tra, không ghi nhận việc triển khai của thành viên khác thành đóng góp của người kiểm tra.

## 1. Phạm vi và kết luận

- Nhánh kiểm tra: `main`, commit **`7c08860`**, bằng `origin/main` đang lưu tại local. Không fetch thêm trong lần kiểm tra này.
- Căn cứ: `De_18.docx` và `Quy_uoc_nhom_Quan_ly_dat_san_v1.4.md` trong Downloads; [quy ước phân tầng](../Quy_uoc_Controller_Service_Repository.md); [hợp đồng M3/M4/M5](../Hop_dong_M3_M4_M5.md); code và test hiện có trên main.
- Build Release: **0 warning, 0 error**. Bộ test .NET hiện có: **417/417 pass, 0 skip**. EF báo không có thay đổi model chưa được migration ghi nhận.
- **Chưa đủ điều kiện kết luận hoàn thành cả 5 module.** Test hiện có pass nhưng còn lỗi tích hợp tái hiện được và chức năng bắt buộc chưa triển khai.
- Sáu kiểm tra bổ sung trên database SQL test riêng đã xác nhận các vấn đề ở mục 3. Đây là phép kiểm tra tái hiện lỗi, **không cộng vào 417 test hồi quy**.
- Không sửa source ứng dụng, Entity, migration; không commit/push. Không cập nhật/reset database ứng dụng. Các fixture tự tạo và dọn database test tên riêng, có guard cleanup.
- Stash `local-m1-before-pull-main-20261030` được giữ nguyên. Phần code nằm trong stash không được tính là đã có trên main.

## 2. Kiểm thử đã thực chạy

Môi trường: Windows, SDK .NET **10.0.401**, SQL Server LocalDB và SQLite theo từng fixture. Test SQL chạy ngoài sandbox vì cần truy cập instance LocalDB. Không dùng dữ liệu/tài khoản vận hành để kiểm thử.

| Bộ test | Pass | Fail | Skip | Phạm vi chính |
| --- | ---: | ---: | ---: | --- |
| `QuanLyDatSan.M103.Tests` | 76 | 0 | 0 | Tài khoản, xác thực, trang chủ, service/repository và transaction M1 |
| `QuanLyDatSan.M104.Tests` | 24 | 0 | 0 | Loại sân, validation, quyền, dữ liệu tham chiếu |
| `QuanLyDatSan.M201.Tests` | 115 | 0 | 0 | CRUD sân, trạng thái, tìm/lọc/sắp xếp/phân trang, cả test SQL Server |
| `QuanLyDatSan.M4.Tests` | 197 | 0 | 0 | Tạo/đọc/xử lý đơn, HTTP, quyền, chống trùng, cạnh tranh, cọc/hoàn thành, repository/UoW |
| `QuanLyDatSan.M5.Tests` | 5 | 0 | 0 | Công thức tính tiền dịch vụ |
| **Tổng** | **417** | **0** | **0** | Không đồng nghĩa đã kiểm đủ mọi yêu cầu đề |

M3 có các ca đăng ký/tạo/đọc đơn và hợp đồng hủy được kiểm thử trong bộ M4; không có project test M3 riêng trong solution. M5 còn được kiểm tra tích hợp thông qua các ca HTTP và cọc/hoàn thành trong bộ M4. Không dùng tên project test để suy ra toàn bộ công lao hoặc độ hoàn thành của một thành viên.

Các ca hiện có đã thực thi gồm quyền endpoint, cookie/antiforgery, tạo đơn trùng giờ đồng thời, RowVersion cũ, thao tác lặp, rollback, cọc vượt tổng, hoàn thành sai trạng thái/thời gian và thay đổi dịch vụ cạnh tranh với M4. Chúng chưa bao phủ các lỗi tích hợp ở mục 3.

Chưa kiểm thử:

- Giao diện thực trong trình duyệt, desktop/mobile, focus bàn phím và thao tác người dùng xuyên suốt. HTTP integration test không thay thế nghiệm thu giao diện.
- Test JavaScript `tests/QuanLyDatSan.M4.Tests/quan-ly-don-dat.test.cjs`: lệnh `node` không khả dụng trong môi trường hiện tại; chưa chạy được, không tính là pass.
- Tình trạng migration đã áp dụng và dữ liệu mẫu của database ứng dụng đang dùng. Kiểm tra pending model chỉ đối chiếu model với snapshot; không chứng minh database vận hành đã cập nhật.
- Dữ liệu mẫu đủ số lượng theo đề, tài khoản demo, video/báo cáo nộp bài và load test.
- QR/chuyển khoản thật, hoặc luồng mô phỏng cọc 50% mới: chưa có triển khai đầy đủ để nghiệm thu.

## 3. Lỗi và điểm lệch đã xác nhận

### P1 — Giá dự kiến M2 khác giá lưu/tính trên đơn M3/M4/M5

**Đã tái hiện bằng code thật và SQL test:** sân giá thường 200.000đ, giá cuối tuần 450.000đ; đặt thứ Bảy 05/01/2030, 10:00–11:00. `GiaThueSan.TinhTien` trả **450.000đ**, nhưng `TaoDonAsync` lưu đơn giá **200.000đ**, `ITinhTienService` tính tiền sân của đơn thành **200.000đ**.

Bằng chứng: [GiaThueSan.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Services/GiaThueSan.cs), dòng 18–32; [DatSanService.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Services/DatSanService.cs), dòng 54; trang `Views/SanTheThao/Details.cshtml` công khai giá cao điểm/cuối tuần. Đây không phải chênh lệch làm tròn.

**Phụ trách phối hợp:** M2 cung cấp cấu hình giá; M5 chốt một nguồn công thức; M3/M4 dùng cùng chính sách khi tạo, xem và hoàn thành đơn. Cần thống nhất snapshot giá cho khoảng giờ cắt qua cao điểm trước khi sửa. Thêm test đối chiếu tiền hiển thị → tiền tạo đơn → tiền hoàn thành cho ngày thường, cao điểm, cuối tuần. Không tự tính lại đơn lịch sử từ giá danh mục mới.

### P1 — M1 tạo tài khoản khách không tạo hồ sơ; sửa tài khoản không đồng bộ hồ sơ

Hai kiểm tra SQL đều tái hiện:

1. Admin gọi `QuanLyTaiKhoanService.TaoAsync` với vai trò `KhachHang`: trả thành công, có `TaiKhoan` nhưng không có `KhachHang`. Khách đi vào hồ sơ/trang chủ khách sẽ gặp nhánh “Không tìm thấy hồ sơ khách hàng”; không đủ điều kiện đặt sân.
2. Với tài khoản đã có hồ sơ, Admin đổi họ tên/email qua `SuaAsync`: tài khoản đổi, hồ sơ vẫn giữ dữ liệu cũ.

Bằng chứng: [QuanLyTaiKhoanService.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyTaiKhoan/QuanLyTaiKhoanService.cs), dòng 31–37 và 58–61. Luồng đăng ký công khai M3 đã tạo cả tài khoản/hồ sơ; lỗi này nằm ở luồng quản trị M1, không phải mọi trường hợp đăng ký.

**Ưu tiên M1**, phối hợp M3 chốt trường hồ sơ bắt buộc và cách xử lý tài khoản cũ thiếu hồ sơ. Đồng bộ trong cùng phạm vi lưu/giao dịch; giữ kiểm tra Admin cuối cùng và thu hồi phiên. Rà lại phần đang ở stash trước khi làm mới, không áp dụng toàn bộ stash chồng lên main.

### P2 — M5 cho sửa dịch vụ khi đơn chờ và xóa cứng dòng khi hủy

**Đã tái hiện SQL:** thêm dịch vụ cho đơn `ChoXuLy` thành công; gọi hủy thì bản ghi `ChiTietDatSan` bị xóa khỏi DB.

Bằng chứng: [DichVuTrongDonService.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyDichVuTrongDon/DichVuTrongDonService.cs), dòng 18–19 và 80–88. Quy ước v1.4/hợp đồng mới yêu cầu chỉ sửa ở `DangXuLy`, hủy bằng trạng thái để giữ lịch sử và chỉ tính dòng hiệu lực.

**M5 sửa**, M4 phối hợp RowVersion/transaction. Chốt schema và tạo migration mới khi thêm trạng thái/ghi chú/thành tiền; không sửa migration cũ. Kiểm thử cả tổng tiền và cạnh tranh với thao tác chốt đơn.

### P2 — Validation service hồ sơ M3 chưa đầy đủ

**Đã tái hiện SQL khi gọi trực tiếp service:** `CapNhatHoSoAsync` nhận email `not-an-email`, điện thoại `not-a-phone` và lưu thành công. Service chỉ kiểm tra ngày sinh rồi Trim/gán các trường khác.

Bằng chứng: [KhachHangService.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLyKhachHang/KhachHangService.cs), dòng 19–44.

Đây là thiếu bảo vệ tại service theo quy ước phân tầng; **không kết luận form HTTP hiện tại cho qua**, vì Controller có ModelState/Data Annotation. M3 bổ sung kiểm tra định dạng, bắt buộc, độ dài, enum tại service, trả mã lỗi và cập nhật mapping Controller; thêm test service và HTTP hồ sơ/ownership.

### P3 — Đăng ký DI còn trùng và chưa gom hết

[Program.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Program.cs), dòng 35–36 và 42–43 đăng ký lặp `IDongHo`, `IDatSanService`; `IDatSanService` còn được đăng ký trong `AddModuleDatSan` của `Extensions/DependencyInjection.cs`.

Chưa làm các test hiện tại thất bại, nhưng lệch quy ước một đầu mối đăng ký và gây khó bảo trì. M1 phối hợp M3/M4 gom mỗi đăng ký về đúng một nơi; giữ lifetime hiện hành.

## 4. Tiến độ từng module

Đánh giá theo chức năng, không quy đổi số test thành phần trăm hoàn thành. “Có code và test đạt” chỉ áp dụng phạm vi đã kiểm tra; không thay thế review của thành viên và nghiệm thu trình duyệt.

### M1 — Trần Trọng Tùng

| Nhóm việc | Hiện trạng main | Còn phải làm |
| --- | --- | --- |
| Dữ liệu nền, đăng nhập/đăng xuất | Có Entity, migration, băm mật khẩu, cookie/Session, đăng xuất POST/antiforgery; test đạt | Nghiệm thu giao diện thực theo các vai trò |
| Quản lý tài khoản/phân quyền (M1-03) | Có quản trị tài khoản, vai trò/trạng thái, bảo vệ Admin cuối cùng, thu hồi phiên; test hiện có đạt | Sửa hai lỗi hồ sơ/tài khoản ở mục 3; bổ sung hồi quy tích hợp M1–M3 |
| Quản lý loại sân (M1-04) | Có danh sách/chi tiết/thêm/sửa/xóa có kiểm soát tham chiếu; test đạt | Nghiệm thu thủ công và dữ liệu mẫu |
| Layout/menu/trang chủ | Có layout theo khu vực, menu quyền, trang chủ sân theo loại; code phân tầng đã trên main | Dọn DI trùng; kiểm tra mobile/menu sau khi các module thêm endpoint |
| Controller–Service–Repository | Đã tách và có UoW/repository test | Giữ đồng bộ tài khoản/hồ sơ khi hoàn thiện nghiệp vụ; phần stash chưa được tính |

**Kết luận:** phần nền M1 đã có nhiều chức năng chạy được, nhưng còn lỗi tích hợp cần sửa trước khi kết luận M1 hoàn tất.

### M2 — Phan Giang Tâm

| Nhóm việc | Hiện trạng main | Còn phải làm |
| --- | --- | --- |
| CRUD sân, trạng thái, loại sân, validation | Có giao diện và service/repository; test quyền và dữ liệu tham chiếu đạt | Nghiệm thu trình duyệt |
| Tìm/lọc/sắp xếp/phân trang | Có tìm tên sân/loại/địa chỉ, lọc loại/trạng thái/khoảng giá, 6 cách sort; phân trang tại DB | Bổ sung lọc ngày sử dụng và sân trống theo khoảng giờ |
| Khả dụng theo lịch | M4 đã cung cấp `KiemTraKhaDungAsync` và tài liệu tích hợp | M2 chưa gọi trong luồng tìm sân; cần kết hợp lọc trước Count/Skip/Take, giữ điều kiện chuyển trang |
| Giá dự kiến | Có giá thường/cao điểm/cuối tuần | Sửa lệch tiền với đơn theo mục 3 |

Bằng chứng phần còn thiếu: [TraCuuSan.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Services/QuanLySan/TraCuuSan.cs) không có ngày/giờ/cờ còn trống; `SanTheThaoService` chuyển thẳng bộ lọc xuống repository, không gọi khả dụng. Sân “Hoạt động” không đồng nghĩa còn trống.

**Kết luận:** CRUD và tìm kiếm cơ bản đã đạt bộ test, nhưng chưa đủ yêu cầu tìm sân theo ngày/khả dụng và chưa thống nhất giá liên module.

### M3 — Nguyễn Văn Quý

| Đầu việc | Hiện trạng main | Kết luận |
| --- | --- | --- |
| M3-01 — Khách hàng/hồ sơ | Có quan hệ tài khoản–khách hàng, đăng ký tạo hồ sơ, khách xem/sửa chính mình, đồng bộ khi khách tự sửa | **Một phần:** chưa có quản lý khách hàng cho Admin/Nhân viên; thiếu validation service; bị ảnh hưởng bởi lỗi quản trị tài khoản M1 |
| M3-02 — Tạo đơn | Có form, lấy khách/giá/thời điểm từ server, dùng service/transaction khóa sân; test tạo đơn và cạnh tranh đạt | Luồng cơ bản đã có; phải sửa lệch chính sách giá trước nghiệm thu tích hợp |
| M3-03 — Lịch sử/chi tiết | Có danh sách cá nhân, lọc/phân trang, chi tiết có ownership, trạng thái và tổng tiền/cọc | **Một phần:** chưa hiển thị từng dịch vụ, số lượng/đơn giá và thông tin thanh toán đầy đủ; tiền dịch vụ chỉ hiện tổng khi có `TongTien` |
| M3-04 — Khách hủy đơn | Service M4 đã có `KhachHangHuyDonAsync`, kiểm tra chủ đơn/trạng thái/giờ/lý do/RowVersion, có test | **Chưa xong giao diện:** `DonDatController` chưa có action hủy; View chi tiết không có form/nút hủy |

Bằng chứng: [DonDatController.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Controllers/DonDatController.cs), [chi tiết khách](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Views/DonDat/ChiTiet.cshtml), `KhachHangController` chỉ có TrangChu/DangKy; `MenuQuanTri` còn ghi chờ màn hình quản lý khách hàng.

**Kết luận:** chưa thể đóng toàn bộ M3-01/03/04. Test hủy ở service không chứng minh khách đã có thao tác hủy trên website.

### M4 — Phạm Thành Nghĩa

| Đầu việc | Hiện trạng main | Còn phải làm |
| --- | --- | --- |
| M4-01 — Khả dụng/chống trùng | Có service dùng chung, kiểm tra giờ mở cửa/bảo trì/lịch, khóa sân và test đồng thời | Phối hợp M2 tích hợp vào tìm sân |
| M4-02 — Danh sách/chi tiết quản lý | Có tìm khách/sân, lọc loại/trạng thái/ngày, sort/phân trang và thông tin tiền | Nghiệm thu trình duyệt theo vai trò |
| M4-03 — Xác nhận/từ chối/hủy | Có endpoint và form phía Admin/Nhân viên; kiểm tra trạng thái/quyền/RowVersion/lý do/thời gian; test đạt | M3 nối luồng khách hủy; thống nhất với quy tắc cọc mới khi triển khai |
| M4-04 — Ghi cọc/hoàn thành | Đã có giao diện và backend; hoàn thành tính lại tiền phía server, kiểm tra thu đủ/giờ kết thúc/cọc; test đạt | **Luồng hiện tại vẫn xác nhận đơn trước, ghi cọc sau**, chưa phải cọc trước rồi xác nhận đồng thời |

Bằng chứng: `DatSanService.XuLy.cs` chỉ cho `GhiCocAsync` khi `DangXuLy`; `XacNhanDonAsync` chuyển đơn chờ sang xác nhận mà chưa thực thi chính sách cọc 50%. Đây là khác biệt với hợp đồng mới, không được gọi là đã xong QR/cọc trước. M4 cần phân biệt luồng cũ với luồng đích, bảo toàn dữ liệu đơn cũ khi bổ sung.

**Kết luận:** nghiệp vụ xử lý đơn hiện hành đã có code và bộ kiểm thử khá đầy đủ; phần cọc mới và tích hợp toàn hệ thống vẫn chưa hoàn thành.

### M5 — Nguyễn Hữu Quang

| Nhóm việc | Hiện trạng main | Còn phải làm |
| --- | --- | --- |
| Danh mục dịch vụ | Có danh sách/tìm lọc/phân trang, thêm/sửa/đổi trạng thái cho Admin | Kiểm tra nghiệm thu theo phạm vi CRUD nhóm đã chốt |
| Dịch vụ trong đơn | Có thêm/sửa số lượng/hủy, snapshot giá, kiểm tra RowVersion | Chặn sửa ở đơn chờ, hủy giữ lịch sử, bổ sung trường còn thiếu và test |
| Tính tiền | Có `ITinhTienService`, M4 đã dùng khi đọc/chốt tiền; test hiện có đạt | Thống nhất với bảng giá M2; thêm công thức cọc 50%/còn phải thu theo hợp đồng mới |
| Entity `ChiTietDatSan` | Có 5 trường scalar: MaChiTiet, MaDatSan, MaDichVu, SoLuong, DonGia | Thiếu **ThanhTien, GhiChu, TrangThai** trong 8 trường tối thiểu của đề/quy ước; DTO có thành tiền không thay thế đầy đủ Entity |
| Dashboard | Trang điều hành hiện chỉ có 3 số đếm: đơn chờ chưa hết giờ, khách hoạt động, sân hoạt động | Chưa đủ 9 chỉ số đề yêu cầu; không tính 3 chỉ số này thành Dashboard M5 đã hoàn thành |
| Thống kê | Chưa có triển khai đầy đủ 8 thống kê yêu cầu trong Controller/Service/Repository/View | Số sân theo loại, lượt đặt theo sân, sân dùng nhiều nhất, doanh thu tháng, doanh thu loại sân, dịch vụ dùng nhiều nhất, lượt hủy, tỷ lệ sử dụng khung giờ |

Bằng chứng: [ChiTietDatSan.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Models/ChiTietDatSan.cs); [DieuHanhRepository.cs](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Repositories/DieuHanh/DieuHanhRepository.cs) chỉ có 3 `CountAsync`; [trang điều hành](../../QuanLyDatSan_UNETI5_DHTI17A4HN/Views/QuanTri/Index.cshtml). Đếm sân hoạt động không phải kiểm tra sân khả dụng theo mốc thời gian; doanh thu phải lấy đơn hoàn thành và không cộng cọc lần nữa.

**Kết luận:** dịch vụ và tính tiền đã có nền, nhưng M5 còn nhiều yêu cầu bắt buộc; ưu tiên sửa quy tắc dịch vụ và nguồn tiền rồi hoàn thiện Dashboard/thống kê.

## 5. Đối chiếu quy ước chung

- Đã có MVC/Razor, .NET 10, EF/SQL Server, Session, LINQ, migration, ViewModel và kiểm tra quyền phía server. Build/test chứng minh code đang chạy được trong môi trường kiểm thử.
- Controller nghiệp vụ đã gọi interface service; truy vấn chính đã chuyển repository; UoW quản lý lưu/giao dịch. `KhoiTaoAdmin` và `PhienDangNhap` là ngoại lệ hạ tầng EF đã được ghi trong tài liệu refactor.
- Còn lệch: DI lặp; service hồ sơ chưa tự validate đầy đủ; M2 vẫn có công thức giá riêng; M5 sai trạng thái sửa dịch vụ/hủy lịch sử; Entity chi tiết thiếu trường; nhiều yêu cầu chức năng chưa có.
- Một số file repository mới chỉ ghi chú refactor của Codex. Khi bàn giao, người thực sự phụ trách cần rà header Họ tên/MSSV/phần việc theo đề; không tự thêm tên để nhận phần chưa thực hiện hoặc chưa hiểu.
- Lịch sử gần đây có commit nguồn không theo mẫu, ví dụ `5002833 Refactor controller service repository`, `f2bc95a Ghi nhan coc va hoan thanh don`. Áp dụng mẫu `[MSSV] [Module] ...` cho commit tiếp theo; không rewrite lịch sử main đã chia sẻ chỉ để sửa tên commit. Merge commit tự sinh là trường hợp riêng.
- Chưa đối chiếu trạng thái đóng/mở của toàn bộ GitHub Issue trong lần này. Kết luận ở đây dựa trên main đang kiểm tra, không dựa vào nhãn “merged” hoặc tick checklist.

## 6. Thứ tự làm tiếp

1. **M1 + M3:** sửa tạo/đồng bộ hồ sơ từ quản trị tài khoản, thêm test bảo đảm khách vừa được tạo có thể dùng các luồng được phép.
2. **M2 + M5, M3/M4 phối hợp:** chốt và thống nhất giá, thêm test đối chiếu từ trang sân tới đơn và thanh toán. Đây là lỗi ảnh hưởng tiền, nên sửa trước demo nghiệp vụ.
3. **M5:** sửa quy tắc dịch vụ và lịch sử; thống nhất Entity/migration trước khi thêm trường bắt buộc. **M3** bổ sung validation service song song.
4. **M3:** nối form/action hủy vào service sẵn có; hoàn thiện quản lý khách hàng và chi tiết dịch vụ/thanh toán theo ownership.
5. **M2:** nối lọc ngày/khung giờ/sân trống với M4, đảm bảo lọc trước phân trang và giữ điều kiện.
6. **M5:** hoàn thiện 9 chỉ số Dashboard và 8 thống kê; kiểm chứng kết quả bằng bộ dữ liệu biết trước.
7. **M3/M4/M5:** triển khai cọc 50% theo hợp đồng đã chốt bằng PR từng phần. Không coi ghi cọc thủ công trên đơn đã xác nhận là đã hoàn thành luồng mới.
8. **M1 điều phối cả nhóm:** dọn DI, chuẩn hóa header/commit mới, nghiệm thu trình duyệt các vai trò, kiểm đủ dữ liệu mẫu và hồ sơ nộp bài.

## 7. Chạy lại và bằng chứng local

Tại thư mục gốc repo:

```powershell
dotnet build QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release --no-restore -v quiet
$env:QLDS_M1_TEST_SQLSERVER = '1'
$env:QLDS_M2_TEST_SQLSERVER = '1'
dotnet test QuanLyDatSan_UNETI5_DHTI17A4HN.slnx -c Release --no-build --no-restore --logger trx --results-directory QuanLyDatSan_UNETI5_DHTI17A4HN/obj/main-review-20261030
dotnet ef migrations has-pending-model-changes --project QuanLyDatSan_UNETI5_DHTI17A4HN --configuration Release --no-build -- --environment Development
```

Máy mới cần `dotnet restore` trước; lệnh SQL yêu cầu LocalDB phù hợp với fixture. Không cần chạy Update-Database cho database ứng dụng để chạy các fixture này.

Bằng chứng nằm trong `QuanLyDatSan_UNETI5_DHTI17A4HN/obj/`, được Git bỏ qua:

- `main-review-20261030.log`: kết quả chạy solution.
- `main-review-20261030/*.trx`: kết quả từng bộ test.
- `main-review-probes/Probes.csproj`, `main-review-probes/Program.cs`: chương trình kiểm tra bổ sung dùng lại fixture SQL M1, chỉ chạy local.
- `main-review-probes.log`: 6 quan sát tái hiện đều `True`, gồm lệch giá 450.000/200.000.

Các file trong obj là bằng chứng cục bộ và có thể mất khi clean; chưa được commit/chia sẻ cho thành viên. Quy trình tái hiện và nguồn code đã ghi ở mục 3 để nhóm chuyển thành regression test trong PR sửa tương ứng.

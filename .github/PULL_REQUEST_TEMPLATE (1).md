# Pull Request

<!--
Template dành cho nhóm Quản lý Đặt Sân – DHTI17A4HN.

Trước khi tạo PR:
- Chỉ đưa các thay đổi liên quan đến task hiện tại vào PR.
- Tự kiểm tra code và Git diff trước.
- Build và chạy thử phải pass.
- Không push / merge trực tiếp vào main.
- Nhánh đặt tên theo mẫu: feature/mX-ten-chuc-nang
-->

## 1. Thông tin chung

**Module / Task liên quan:**  
<!-- Ví dụ: M1 - Đăng nhập, M3 - Tạo đơn đặt sân, M5 - Tính tiền -->  
M

**Người thực hiện:**  
<!-- Họ tên - MSSV -->  


**Mục đích của PR:**

<!--
Mô tả ngắn gọn:
- Làm chức năng gì?
- Sửa vấn đề gì?
- Thêm / chỉnh sửa phần nào?
-->



---

## 2. Loại thay đổi

<!-- Chọn loại phù hợp nhất. -->

- [ ] `feat` — Thêm tính năng mới
- [ ] `fix` — Sửa lỗi
- [ ] `refactor` — Cấu trúc lại code, không thay đổi nghiệp vụ
- [ ] `docs` — Cập nhật tài liệu
- [ ] `chore` — Cấu hình, dependency, migration hoặc công việc kỹ thuật khác
- [ ] `style` — Chỉnh giao diện / CSS / layout

---

## 3. Phạm vi thay đổi

<!-- Đánh dấu các phần có thay đổi. -->

- [ ] Models / Enums
- [ ] Data / DbContext / Migrations
- [ ] Controllers / ViewModels
- [ ] Services
- [ ] Views / Layout / wwwroot
- [ ] docs
- [ ] Khác: _______________

**Module bị ảnh hưởng:**

<!-- Ví dụ: M1, M2, M3, M4, M5 hoặc phần dùng chung (DatSanService, TinhTienService, Layout...) -->



**File / phần dùng chung đã thay đổi (nếu có):**

<!-- Ghi rõ nếu đụng đến layout, service dùng chung, DbContext, enum... để người review chú ý -->



---

## 4. Database / EF Core Migration

**Có thay đổi database không?**

- [ ] Không
- [ ] Có

**Tên Migration:** `N/A`

Nếu có migration:

- [ ] Đã kiểm tra code migration được generate
- [ ] Đã chạy `dotnet ef database update` thành công
- [ ] Database sau migration hoạt động bình thường
- [ ] Không làm mất dữ liệu hiện có
- [ ] Đã cập nhật seed data / dữ liệu mẫu nếu cần

**Ghi chú migration:**

<!-- N/A hoặc mô tả thay đổi schema, ràng buộc, dữ liệu -->



---

## 5. Build & Code Quality

- [ ] Đã chạy `dotnet build` thành công
- [ ] Không còn error / warning bất thường
- [ ] Không còn unused `using`, code thừa hoặc commented code
- [ ] Không còn debug log / code test tạm
- [ ] Naming tuân theo quy ước nhóm (Entity/PascalCase, biến/camelCase, tiếng Việt không dấu)
- [ ] Đầu file có ghi họ tên, MSSV và nội dung thực hiện
- [ ] Không tự tạo tên đồng nghĩa với quy ước đã thống nhất

---

## 6. Kiểm thử nghiệp vụ

- [ ] Đã build và chạy thử trên máy
- [ ] Kiểm tra phân quyền (đúng vai trò / sai vai trò / tài khoản bị khóa)
- [ ] Kiểm tra nghiệp vụ chính của task (ngày giờ, trùng lịch, chuyển trạng thái, tính tiền, cọc...)
- [ ] Kiểm tra giao diện (lỗi rõ ràng, nút đúng trạng thái, tìm/lọc/sort/phân trang nếu có)
- [ ] Không áp dụng (chỉ docs / chore)

**Cách đã kiểm thử:**

<!-- Mô tả ngắn các bước test đã làm -->



**Kết quả test:**

```text
N/A
```

---

## 7. Phân quyền & Bảo mật

- [ ] Không hardcode mật khẩu, connection string, token
- [ ] Không commit file chứa bí mật (User Secrets, .env...)
- [ ] Input từ client được validate (ModelState + nghiệp vụ server)
- [ ] Kiểm tra quyền sở hữu dữ liệu (khách không xem/sửa đơn người khác)
- [ ] Form thay đổi dữ liệu dùng POST + antiforgery
- [ ] Không áp dụng

---

## 8. Self-review trước khi yêu cầu review

- [ ] PR chỉ chứa code liên quan đến task hiện tại
- [ ] Không có file bị sửa ngoài ý muốn
- [ ] Đã xem lại toàn bộ Git diff
- [ ] Đã giải quyết conflict với `main` (nếu có)
- [ ] Commit message rõ ràng, phản ánh đúng việc làm
- [ ] Tuân thủ quy ước nhóm (Quy_uoc_nhom_Quan_ly_dat_san)

---

## 9. Checklist theo quy ước nhóm (trước khi merge)

| Kiểm tra              | Tiêu chí đạt                                                                 |
|-----------------------|-------------------------------------------------------------------------------|
| Build và dữ liệu      | Build thành công; Migration chạy được; không làm mất dữ liệu hiện có          |
| Phân quyền            | Đúng/sai vai trò; tài khoản bị khóa; khách đổi mã URL đều được kiểm tra       |
| Nghiệp vụ             | Ngày giờ, trùng lịch, chuyển trạng thái, giá và cọc hợp lệ                    |
| Giao diện             | Lỗi rõ ràng; nút đúng trạng thái; tìm/lọc/sort/phân trang kết hợp được        |
| Lịch sử cá nhân       | Header tác giả đúng; commit phản ánh việc thực tế                             |
| Nội dung PR           | Nêu chức năng, file chung thay đổi, Migration nếu có và cách đã kiểm thử      |

---

## 10. Ghi chú cho Reviewer

**Phần cần review kỹ:**

<!--
Ví dụ:
- Logic trùng lịch / transaction
- Công thức tính tiền
- Chuyển trạng thái đơn
- Phân quyền
- Migration
-->



**Cách test nhanh:**

<!--
Ghi account test, dữ liệu mẫu, bước thực hiện cụ thể để reviewer kiểm tra nhanh.
Ví dụ:
- Đăng nhập: admin / 123456
- Vào menu Đơn đặt sân → tạo đơn mới với sân X, giờ Y
-->



**Screenshot / minh họa (nếu cần):**

<!-- Đính kèm ảnh hoặc mô tả -->

N/A

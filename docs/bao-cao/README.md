# Báo cáo tiến độ và kiểm thử

Người tổng hợp: Trần Trọng Tùng — MSSV 23103100202. Codex hỗ trợ soạn tài liệu và kiểm thử theo yêu cầu. Báo cáo ghi nhận phần việc thực tế của từng thành viên; không coi việc tổng hợp là đóng góp triển khai thay các module.

| Tài liệu | Phạm vi |
| --- | --- |
| `Bao_cao_tien_do_M1_Tran_Trong_Tung_2026-10-19.docx` | Tiến độ và dự định M1 tại ngày 19/10/2026. |
| `Bao_cao_tien_do_nhom_5_2026-10-21.*` | Báo cáo toàn nhóm tại ngày 21/10; bản Markdown, Word và PDF. |
| `Bao_cao_tien_do_nhom_5_2026-10-21_co_luong_chuc_nang.*` | Bản bổ sung quy trình và luồng chức năng tại ngày 21/10. |
| Các file `*_kiem_thu.json` | Số liệu kiểm thử lịch sử kèm mốc code/phạm vi. Đường dẫn TRX là bằng chứng local, không phải file đã được đưa lên GitHub. |
| [Kiểm thử main ngày 30/10](Kiem_thu_main_tien_do_M1_M5_2026-10-30.md) | Đánh giá main sạch `7c08860`, **trước khi khôi phục thay đổi hồ sơ/menu M1 từ stash**. 417 test pass không đồng nghĩa hoàn thành tất cả module. |

Các bản Word/PDF là tài liệu lưu theo ngày, không được tự động cập nhật khi code thay đổi. Không dùng số test hoặc kết luận của báo cáo cũ để đại diện cho commit mới.

Sau lần kiểm tra main ngày 30/10, phần tạo/đồng bộ hồ sơ và menu M1 đã được khôi phục, giải quyết xung đột với refactor mới và kiểm thử lại tại local: **445/445 test pass, 0 skip**. Do đó hai lỗi tạo/đồng bộ hồ sơ M1 và đăng ký DI trùng nêu trong báo cáo main là hiện trạng trước thay đổi này. Những khoảng thiếu M2–M5 trong báo cáo không được coi là đã sửa bởi PR M1.

Chi tiết phần khôi phục và test: [M1 phần 6](../module-1/Phan-6-Dong-bo-ho-so-khach.md), [M1 phần 7](../module-1/Phan-7-Tich-hop-menu.md), [M1 phần 8](../module-1/Phan-8-Repository-UnitOfWork.md). PR ghi kết quả kiểm tra riêng trên bản code chuẩn bị push và các giới hạn kiểm thử.

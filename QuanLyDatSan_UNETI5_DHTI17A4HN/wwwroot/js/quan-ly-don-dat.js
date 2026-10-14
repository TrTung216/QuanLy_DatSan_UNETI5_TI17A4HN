// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: M4-03 - xác nhận trước khi từ chối/hủy; chặn gửi lặp các form xử lý đơn và khôi phục nút khi quay lại.
(() => {
    const forms = document.querySelectorAll('[data-xu-ly-don]');
    let dangGui = false;
    forms.forEach(form => form.addEventListener('submit', event => {
        if (dangGui) { event.preventDefault(); return; }
        if (!form.checkValidity()) { event.preventDefault(); form.reportValidity(); return; }
        if (form.dataset.xacNhan && !window.confirm(form.dataset.xacNhan)) {
            event.preventDefault();
            return;
        }
        dangGui = true;
        forms.forEach(item => item.querySelectorAll('button[type="submit"]').forEach(button => {
            button.disabled = true;
        }));
    }));
    window.addEventListener('pageshow', () => {
        dangGui = false;
        forms.forEach(form => form.querySelectorAll('button[type="submit"]').forEach(button => {
            button.disabled = false;
        }));
    });
})();

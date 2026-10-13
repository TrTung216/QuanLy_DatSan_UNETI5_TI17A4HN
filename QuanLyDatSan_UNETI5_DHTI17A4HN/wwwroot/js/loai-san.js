// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ chống gửi lặp và khôi phục form loại sân.
(() => {
    const forms = document.querySelectorAll('[data-loai-san-form]');
    forms.forEach(form => form.addEventListener('submit', event => {
        if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
        if (!form.checkValidity() || (window.jQuery?.fn.valid && !window.jQuery(form).valid())) return;
        form.dataset.submitting = 'true';
        form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = true; });
    }));
    window.addEventListener('pageshow', () => forms.forEach(form => {
        delete form.dataset.submitting;
        form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = false; });
    }));
})();

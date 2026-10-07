// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ chống gửi lặp form M1-03.
(() => {
    "use strict";
    const form = document.getElementById("form-quan-ly-tai-khoan");
    if (!form) return;
    const submit = form.querySelector('button[type="submit"]');
    form.addEventListener("submit", (event) => {
        if (form.getAttribute("aria-busy") === "true") {
            event.preventDefault();
            return;
        }
        if (event.defaultPrevented || !form.checkValidity()) return;
        if (window.jQuery?.validator && !window.jQuery(form).valid()) return;
        form.setAttribute("aria-busy", "true");
        if (submit) submit.disabled = true;
    });
    window.addEventListener("pageshow", () => {
        form.removeAttribute("aria-busy");
        if (submit) submit.disabled = false;
    });
})();

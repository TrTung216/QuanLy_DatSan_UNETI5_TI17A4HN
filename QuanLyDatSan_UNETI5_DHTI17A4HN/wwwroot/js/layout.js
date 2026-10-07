// M1: Trần Trọng Tùng; MSSV: 23103100202. Codex hỗ trợ menu bàn phím và chống gửi lặp form đăng xuất chung.
(() => {
    "use strict";
    const menu = document.getElementById("menu-chinh");
    const toggle = document.querySelector('[aria-controls="menu-chinh"]');
    if (menu && toggle && window.bootstrap) menu.addEventListener("keydown", (event) => {
        if (event.key !== "Escape" || !menu.classList.contains("show") ||
            !window.matchMedia("(max-width: 1199.98px)").matches) return;
        event.preventDefault();
        bootstrap.Collapse.getOrCreateInstance(menu, { toggle: false }).hide();
        toggle.focus();
    });

    // Chỉ áp dụng cho form đăng xuất của header, không can thiệp form module.
    const logout = document.getElementById("dang-xuat");
    if (!logout) return;
    const submit = logout.querySelector('button[type="submit"]');
    logout.addEventListener("submit", (event) => {
        if (logout.getAttribute("aria-busy") === "true") {
            event.preventDefault();
            return;
        }
        if (event.defaultPrevented) return;
        logout.setAttribute("aria-busy", "true");
        if (submit) submit.disabled = true;
    });
    // Khôi phục nút khi trình duyệt trả lại trang từ bộ nhớ lịch sử.
    window.addEventListener("pageshow", () => {
        logout.removeAttribute("aria-busy");
        if (submit) submit.disabled = false;
    });
})();

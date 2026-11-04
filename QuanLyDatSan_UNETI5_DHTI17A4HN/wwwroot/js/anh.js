// M1: Trần Trọng Tùng - 23103100202. 404/ảnh mất chỉ thay placeholder của component, không hỏng trang.
(() => {
    document.querySelectorAll('[data-anh-dai-dien]').forEach(container => {
        const img = container.querySelector('img');
        if (!img) return;
        const fallback = container.querySelector('[data-anh-trong]');
        const hienThi = ok => { img.hidden = !ok; fallback.hidden = ok; };
        img.addEventListener('load', () => hienThi(true));
        img.addEventListener('error', () => hienThi(false));
        if (img.complete) hienThi(img.naturalWidth > 0);
    });
})();

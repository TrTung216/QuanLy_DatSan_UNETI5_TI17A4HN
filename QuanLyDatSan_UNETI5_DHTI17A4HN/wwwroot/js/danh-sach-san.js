// M1: Trần Trọng Tùng - 23103100202. Chỉ thu gọn bộ lọc trên điện thoại; form GET hoạt động cả khi tắt JS.
(() => {
    const page = document.querySelector('.san-explore');
    const filters = page?.querySelector('.san-filter-panel details');
    if (filters && window.matchMedia('(max-width: 767.98px)').matches
        && !page.querySelector('.validation-summary-errors')) {
        filters.open = false;
    }
})();

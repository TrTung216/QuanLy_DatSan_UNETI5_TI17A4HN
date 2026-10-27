// M4: Phạm Thành Nghĩa; MSSV: 23103100308.
// Nội dung: kiểm thử JS xác nhận hủy/từ chối, form không hợp lệ, chặn gửi lặp và khôi phục nút.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { resolve } = require('node:path');
const { runInNewContext } = require('node:vm');

const source = readFileSync(resolve(__dirname,
    '../../QuanLyDatSan_UNETI5_DHTI17A4HN/wwwroot/js/quan-ly-don-dat.js'), 'utf8');

function setup() {
    const pageEvents = {};
    const confirms = [];
    let answer = true;
    const forms = [undefined, 'Từ chối đơn?', 'Hủy đơn?', 'Ghi nhận cọc?', 'Hoàn thành đơn?'].map(message => ({
        dataset: { xacNhan: message }, valid: true, reported: false, buttons: [{ disabled: false }],
        checkValidity() { return this.valid; },
        reportValidity() { this.reported = true; },
        querySelectorAll() { return this.buttons; },
        addEventListener(name, handler) { this[name] = handler; }
    }));
    runInNewContext(source, {
        document: { querySelectorAll: () => forms },
        window: {
            confirm(message) { confirms.push(message); return answer; },
            addEventListener(name, handler) { pageEvents[name] = handler; }
        }
    });
    return {
        forms, confirms, pageEvents, answer(value) { answer = value; },
        submit(index) {
            const event = { prevented: false, preventDefault() { this.prevented = true; } };
            forms[index].submit(event);
            return event;
        }
    };
}

test('Từ chối/hủy cần xác nhận; chọn không giữ nguyên form', () => {
    const page = setup();
    page.answer(false);
    assert.equal(page.submit(1).prevented, true);
    assert.equal(page.submit(2).prevented, true);
    assert.deepEqual(page.confirms, ['Từ chối đơn?', 'Hủy đơn?']);
    assert.ok(page.forms.every(form => !form.buttons[0].disabled));
});

test('Form chưa hợp lệ không gửi hoặc mở xác nhận', () => {
    const page = setup();
    page.forms[2].valid = false;
    assert.equal(page.submit(2).prevented, true);
    assert.equal(page.forms[2].reported, true);
    assert.equal(page.confirms.length, 0);
    assert.ok(page.forms.every(form => !form.buttons[0].disabled));
});

test('Chỉ gửi một thao tác và khóa mọi nút xử lý', () => {
    const page = setup();
    assert.equal(page.submit(0).prevented, false);
    assert.equal(page.submit(0).prevented, true);
    assert.equal(page.submit(2).prevented, true);
    assert.ok(page.forms.every(form => form.buttons[0].disabled));
    assert.equal(page.confirms.length, 0);
});

test('Ghi cọc và hoàn thành cần xác nhận; gửi cọc khóa cả hoàn thành', () => {
    const page = setup();
    page.answer(false);
    assert.equal(page.submit(3).prevented, true);
    assert.equal(page.submit(4).prevented, true);
    page.answer(true);
    assert.equal(page.submit(3).prevented, false);
    assert.equal(page.submit(4).prevented, true);
    assert.ok(page.forms.every(form => form.buttons[0].disabled));
    page.pageEvents.pageshow();
    assert.equal(page.submit(4).prevented, false);
});

test('Quay lại bằng lịch sử khôi phục nút và cho phép xác nhận thao tác mới', () => {
    const page = setup();
    assert.equal(page.submit(2).prevented, false);
    page.pageEvents.pageshow();
    assert.ok(page.forms.every(form => !form.buttons[0].disabled));
    assert.equal(page.submit(1).prevented, false);
    assert.deepEqual(page.confirms, ['Hủy đơn?', 'Từ chối đơn?']);
});

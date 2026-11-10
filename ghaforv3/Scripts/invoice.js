(function () {
    'use strict';
    // چاپ و بازگشت (بدون کد inline تا Content-Security-Policy سخت‌گیر بماند)
    document.addEventListener('click', function (e) {
        var p = e.target.closest ? e.target.closest('[data-print]') : null;
        if (p) { window.print(); return; }
        var b = e.target.closest ? e.target.closest('[data-back]') : null;
        if (b) {
            e.preventDefault();
            if (window.history.length > 1) window.history.back(); else window.location.href = b.getAttribute('href');
        }
    });
    // آدرس با ?print=1 باز شود، بعد از آماده شدن فونت‌ها پنجرهٔ چاپ/ذخیره PDF باز می‌شود
    if (/[?&]print=1/.test(window.location.search)) {
        var go = function () { window.setTimeout(function () { window.print(); }, 300); };
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(go); else window.addEventListener('load', go);
    }
})();

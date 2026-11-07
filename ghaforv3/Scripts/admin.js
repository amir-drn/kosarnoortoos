// اسکریپت پنل مدیریت (بدون کد inline تا Content-Security-Policy بتواند سخت‌گیر باشد)
(function () {
    var body = document.body;
    var menu = document.getElementById('aMenu');
    var overlay = document.getElementById('aOverlay');
    function toggle(open) { body.classList.toggle('a-open', open); }
    if (menu) menu.addEventListener('click', function () { toggle(true); });
    if (overlay) overlay.addEventListener('click', function () { toggle(false); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') toggle(false); });

    // فرم‌هایی که data-confirm دارند قبل از ارسال تأیید می‌گیرند
    document.addEventListener('submit', function (e) {
        var msg = e.target && e.target.getAttribute ? e.target.getAttribute('data-confirm') : null;
        if (msg && !window.confirm(msg)) e.preventDefault();
    });
})();

// این فایل کوچک در <head> و قبل از رسم صفحه اجرا می‌شود
(function () {
    var d = document.documentElement;
    d.className += ' js';
    try {
        // مقدمهٔ «روشن شدن لامپ» فقط یک بار در هر نشست و فقط روی صفحهٔ اصلی، و اگر کاربر انیمیشن کم خواسته باشد نمایش داده نمی‌شود
        var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (!reduce && d.getAttribute('data-page') === 'Home' && !sessionStorage.getItem('gh-intro')) {
            d.className += ' intro-on';
            sessionStorage.setItem('gh-intro', '1');
        }
    } catch (e) { }
})();

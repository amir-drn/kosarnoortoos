// غفوریان - رفتار سمت کاربر (موبایل و دسکتاپ)
// نکته: این فایل هیچ کد inline ندارد تا Content-Security-Policy سخت‌گیر بماند.
(function () {
    'use strict';
    var d = document;
    var each = function (list, fn) { Array.prototype.forEach.call(list, fn); };
    var mq = function (q) { return !!(window.matchMedia && window.matchMedia(q).matches); };
    var reduce = mq('(prefers-reduced-motion: reduce)');
    var fine = mq('(hover: hover) and (pointer: fine)');
    var wide = function () { return mq('(min-width: 960px)'); };

    // ---------- شمارندهٔ معکوس «پیشنهاد امروز» ----------
    var timerEl = d.getElementById('timer');
    if (timerEl) {
        var end = Date.now() + parseInt(timerEl.getAttribute('data-seconds'), 10) * 1000;
        var pad = function (n) { return n < 10 ? '0' + n : n; };
        var tick = function () {
            var s = Math.max(0, Math.floor((end - Date.now()) / 1000));
            timerEl.textContent = pad(Math.floor(s / 3600)) + ':' + pad(Math.floor(s % 3600 / 60)) + ':' + pad(s % 60);
        };
        tick();
        setInterval(tick, 1000);
    }

    // ---------- اسکرول: سایهٔ هدر، نوار پیشرفت، پارالاکس ----------
    var headers = d.querySelectorAll('.header,#dkHead');
    var bar = d.getElementById('progress');
    var par = d.querySelectorAll('[data-parallax]');
    var pending = false;
    function onScroll() {
        pending = false;
        var y = window.pageYOffset || d.documentElement.scrollTop || 0;
        each(headers, function (h) { h.classList.toggle('scrolled', y > 10); });
        if (bar) {
            var max = d.documentElement.scrollHeight - window.innerHeight;
            bar.style.setProperty('--p', max > 0 ? Math.min(1, y / max).toFixed(4) : 0);
        }
        if (!reduce) {
            each(par, function (el) {
                var r = el.getBoundingClientRect();
                var c = r.top + r.height / 2 - window.innerHeight / 2;
                el.style.setProperty('--py', (c * -0.06).toFixed(1) + 'px');
            });
        }
    }
    function queueScroll() { if (!pending) { pending = true; window.requestAnimationFrame(onScroll); } }
    window.addEventListener('scroll', queueScroll, { passive: true });
    window.addEventListener('resize', queueScroll, { passive: true });
    onScroll();

    // ---------- منوی کشویی موبایل ----------
    var body = d.body;
    function menu(open) { body.classList.toggle('menu-open', open); }
    each(d.querySelectorAll('#menuBtn,[data-open-menu]'), function (b) { b.addEventListener('click', function () { menu(true); }); });
    each(d.querySelectorAll('[data-close-menu]'), function (b) { b.addEventListener('click', function () { menu(false); }); });
    d.addEventListener('keydown', function (e) { if (e.key === 'Escape') menu(false); });

    // ---------- هیرو: تیتر کلمه‌به‌کلمه + نوری که دنبال ماوس می‌آید ----------
    function splitWords(root) {
        var idx = 0;
        (function walk(node) {
            Array.prototype.slice.call(node.childNodes).forEach(function (ch) {
                if (ch.nodeType === 3) {
                    var frag = d.createDocumentFragment();
                    ch.nodeValue.split(/(\s+)/).forEach(function (part) {
                        if (!part) return;
                        if (/^\s+$/.test(part)) { frag.appendChild(d.createTextNode(' ')); return; }
                        var w = d.createElement('span'); w.className = 'w';
                        var s = d.createElement('span'); s.textContent = part; s.style.setProperty('--d', idx++);
                        w.appendChild(s); frag.appendChild(w);
                    });
                    node.replaceChild(frag, ch);
                } else if (ch.nodeType === 1 && ch.tagName !== 'BR') { walk(ch); }
            });
        })(root);
    }
    var hero = d.querySelector('[data-hero]');
    if (hero) {
        var h1 = hero.querySelector('[data-split]');
        if (h1 && !reduce) splitWords(h1);
        if (fine && !reduce) {
            var tx = 0, ty = 0, cx = 0, cy = 0, raf = 0, w = 1, h = 1;
            var follow = function () {
                cx += (tx - cx) * 0.12; cy += (ty - cy) * 0.12;
                hero.style.setProperty('--mx', cx.toFixed(1) + 'px');
                hero.style.setProperty('--my', cy.toFixed(1) + 'px');
                hero.style.setProperty('--px', ((cx / w - 0.5) * -26).toFixed(1) + 'px');
                hero.style.setProperty('--py', ((cy / h - 0.5) * -18).toFixed(1) + 'px');
                raf = (Math.abs(tx - cx) > 0.4 || Math.abs(ty - cy) > 0.4) ? window.requestAnimationFrame(follow) : 0;
            };
            hero.addEventListener('pointermove', function (e) {
                var r = hero.getBoundingClientRect();
                w = r.width; h = r.height;
                tx = e.clientX - r.left; ty = e.clientY - r.top;
                if (!raf) raf = window.requestAnimationFrame(follow);
            });
        }
    }

    // ---------- ظاهر شدن با اسکرول ----------
    each(d.querySelectorAll('.grid,.features,.h-scroll'), function (g) {
        each(g.children, function (c, i) { c.style.setProperty('--i', i % 4); });
    });
    var items = d.querySelectorAll('.reveal');
    if (!('IntersectionObserver' in window) || reduce) {
        each(items, function (e) { e.classList.add('in'); });
    } else {
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (!en.isIntersecting) return;
                var el = en.target;
                el.classList.add('in');
                io.unobserve(el);
                el.addEventListener('transitionend', function (ev) {
                    if (ev.target === el) el.classList.remove('reveal', 'in');
                });
            });
        }, { threshold: 0.12 });
        each(items, function (e) { io.observe(e); });
    }

    // ---------- موج لمس روی دکمه‌ها ----------
    d.addEventListener('pointerdown', function (e) {
        var b = e.target.closest ? e.target.closest('.btn,.add-btn,.cart-btn') : null;
        if (!b || b.disabled || reduce) return;
        var r = b.getBoundingClientRect();
        var size = Math.max(r.width, r.height) * 2;
        var s = d.createElement('span');
        s.className = 'rip';
        s.style.width = s.style.height = size + 'px';
        s.style.left = (e.clientX - r.left - size / 2) + 'px';
        s.style.top = (e.clientY - r.top - size / 2) + 'px';
        b.appendChild(s);
        setTimeout(function () { if (s.parentNode) s.parentNode.removeChild(s); }, 650);
    });

    // ---------- افزودن به سبد (AJAX با توکن CSRF) ----------
    var toast = d.getElementById('toast'), toastTimer;
    function say(text) {
        if (!toast) return;
        toast.textContent = text;
        toast.classList.add('show');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { toast.classList.remove('show'); }, 2600);
    }
    function csrf() {
        var i = d.querySelector('#csrf input[name="__RequestVerificationToken"]');
        return i ? i.value : '';
    }
    function setBadge(n) {
        each(d.querySelectorAll('.cart-count'), function (b) {
            b.textContent = Number(n).toLocaleString('fa-IR');
            b.hidden = n <= 0;
            b.style.animation = 'none'; void b.offsetWidth; b.style.animation = '';
        });
    }
    function cartTarget() {
        var list = d.querySelectorAll('.cart-target');
        for (var i = 0; i < list.length; i++) if (list[i].getClientRects().length) return list[i];
        return null;
    }
    // عکس محصول به سمت سبد «پرواز» می‌کند
    function fly(img) {
        var target = cartTarget();
        if (reduce || !img || !target || !img.getClientRects().length || !img.animate) return;
        var a = img.getBoundingClientRect(), b = target.getBoundingClientRect();
        var size = Math.min(a.width, a.height, 160);
        var c = d.createElement('img');
        c.className = 'fly'; c.alt = ''; c.src = img.currentSrc || img.src;
        c.style.width = c.style.height = size + 'px';
        c.style.left = (a.left + a.width / 2 - size / 2) + 'px';
        c.style.top = (a.top + a.height / 2 - size / 2) + 'px';
        d.body.appendChild(c);
        var dx = (b.left + b.width / 2) - (a.left + a.width / 2);
        var dy = (b.top + b.height / 2) - (a.top + a.height / 2);
        var anim = c.animate([
            { transform: 'translate(0,0) scale(1)', opacity: 1 },
            { transform: 'translate(' + (dx * 0.5) + 'px,' + (dy * 0.5 - 70) + 'px) scale(.6)', opacity: .95, offset: .55 },
            { transform: 'translate(' + dx + 'px,' + dy + 'px) scale(.12)', opacity: .35 }
        ], { duration: 800, easing: 'cubic-bezier(.5,0,.25,1)' });
        anim.onfinish = function () {
            if (c.parentNode) c.parentNode.removeChild(c);
            target.classList.remove('bump'); void target.offsetWidth; target.classList.add('bump');
        };
    }
    function markDone(b) {
        if (b._busy) return;
        b._busy = true;
        var old = b.textContent;
        b.textContent = b.classList.contains('add-btn') ? '✓ به سبد اضافه شد' : '✓';
        b.classList.add('is-done');
        setTimeout(function () { b.textContent = old; b.classList.remove('is-done'); b._busy = false; }, 1600);
    }
    d.addEventListener('click', function (e) {
        var b = e.target.closest ? e.target.closest('.cart-btn,[data-add]') : null;
        if (!b || b.disabled || !toast) return;
        var qtyEl = b.hasAttribute('data-add') ? d.getElementById('qty') : null;
        var qty = qtyEl ? parseInt(qtyEl.value, 10) || 1 : 1;
        var card = b.closest('.card');
        var img = card ? card.querySelector('.card-img img') : d.querySelector('.slide img');

        // گزینه‌های رنگ بدنه / رنگ نور (فقط صفحهٔ جزئیات): انتخاب هر گروه الزامی است
        var body = '', light = '';
        if (b.hasAttribute('data-add')) {
            var groups = d.querySelectorAll('.opt');
            for (var gi = 0; gi < groups.length; gi++) {
                var checked = groups[gi].querySelector('input:checked');
                if (!checked) {
                    groups[gi].classList.remove('need'); void groups[gi].offsetWidth; groups[gi].classList.add('need');
                    say(groups[gi].getAttribute('data-opt') === 'body' ? 'رنگ بدنه را انتخاب کن' : 'رنگ نور را انتخاب کن');
                    if (groups[gi].scrollIntoView) groups[gi].scrollIntoView({ block: 'center', behavior: reduce ? 'auto' : 'smooth' });
                    return;
                }
                if (groups[gi].getAttribute('data-opt') === 'body') body = checked.value; else light = checked.value;
            }
        }

        fetch(toast.getAttribute('data-add-url'), {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest', 'X-CSRF-Token': csrf() },
            body: 'id=' + encodeURIComponent(b.getAttribute('data-id')) + '&qty=' + encodeURIComponent(qty) + '&body=' + encodeURIComponent(body) + '&light=' + encodeURIComponent(light)
        }).then(function (r) { return r.json(); })
          .then(function (res) {
              say(res.message);
              if (res.ok) { setBadge(res.count); fly(img); markDone(b); }
          })
          .catch(function () { say('خطا در ارتباط با سرور'); });
    });

    // ---------- کارت محصول: کج‌شدن سه‌بعدی و برق نور (فقط ماوس، فقط دسکتاپ) ----------
    if (fine && !reduce) {
        d.addEventListener('pointermove', function (e) {
            if (!wide()) return;
            var c = e.target.closest ? e.target.closest('.card') : null;
            if (!c) return;
            var r = c.getBoundingClientRect();
            var px = (e.clientX - r.left) / r.width, py = (e.clientY - r.top) / r.height;
            c.style.setProperty('--rx', ((0.5 - py) * 7).toFixed(2) + 'deg');
            c.style.setProperty('--ry', ((px - 0.5) * 9).toFixed(2) + 'deg');
            c.style.setProperty('--gx', (px * 100).toFixed(0) + '%');
            c.style.setProperty('--gy', (py * 100).toFixed(0) + '%');
        });
        d.addEventListener('pointerout', function (e) {
            var c = e.target.closest ? e.target.closest('.card') : null;
            if (c && !c.contains(e.relatedTarget)) { c.style.removeProperty('--rx'); c.style.removeProperty('--ry'); }
        });
        // دکمه‌های «مغناطیسی»
        each(d.querySelectorAll('[data-magnet]'), function (m) {
            m.addEventListener('pointermove', function (e) {
                if (!wide()) return;
                var r = m.getBoundingClientRect();
                m.style.transform = 'translate(' + ((e.clientX - r.left - r.width / 2) * 0.22).toFixed(1) + 'px,' + ((e.clientY - r.top - r.height / 2) * 0.3).toFixed(1) + 'px)';
            });
            m.addEventListener('pointerleave', function () { m.style.transform = ''; });
        });
    }

    // ---------- مرتب‌سازی: با تغییر انتخاب، فرم ارسال می‌شود ----------
    d.addEventListener('change', function (e) {
        var t = e.target;
        if (t && t.hasAttribute && t.hasAttribute('data-autosubmit') && t.form) t.form.submit();
    });

    // ---------- صفحهٔ جزئیات: گالری، بزرگ‌نمایی و انتخاب تعداد ----------
    var slides = d.getElementById('slides'), dots = d.getElementById('dots');
    if (slides) {
        var gallery = slides.parentNode;
        var count = slides.children.length;
        var rtl = window.getComputedStyle(slides).direction === 'rtl';
        var current = function () { return Math.round(Math.abs(slides.scrollLeft) / (slides.clientWidth || 1)); };
        var go = function (i) {
            i = Math.max(0, Math.min(count - 1, i));
            slides.scrollTo({ left: (rtl ? -1 : 1) * i * slides.clientWidth, behavior: reduce ? 'auto' : 'smooth' });
        };
        if (count > 1) {
            var thumbs = d.createElement('div'); thumbs.className = 'thumbs';
            for (var i = 0; i < count; i++) {
                if (dots) { var dot = d.createElement('span'); if (i === 0) dot.className = 'on'; dots.appendChild(dot); }
                var im = slides.children[i].querySelector('img');
                if (im) {
                    var tb = d.createElement('button'); tb.type = 'button'; tb.className = 'thumb' + (i === 0 ? ' on' : '');
                    tb.setAttribute('aria-label', 'عکس ' + (i + 1));
                    var ti = d.createElement('img'); ti.src = im.currentSrc || im.src; ti.alt = '';
                    tb.appendChild(ti);
                    (function (n) { tb.addEventListener('click', function () { go(n); }); })(i);
                    thumbs.appendChild(tb);
                }
            }
            gallery.appendChild(thumbs);
            var mk = function (cls, icon, label, step) {
                var b = d.createElement('button'); b.type = 'button'; b.className = 'g-nav ' + cls; b.setAttribute('aria-label', label);
                b.innerHTML = '<svg class="i"><use href="#' + icon + '"/></svg>';
                b.addEventListener('click', function () { go(current() + step); });
                gallery.appendChild(b);
            };
            mk('g-prev', 'i-right', 'عکس قبلی', -1);
            mk('g-next', 'i-left', 'عکس بعدی', 1);
            slides.addEventListener('scroll', function () {
                var idx = current();
                if (dots) each(dots.children, function (el, k) { el.className = k === idx ? 'on' : ''; });
                each(thumbs.children, function (el, k) { el.classList.toggle('on', k === idx); });
            }, { passive: true });
            slides.addEventListener('keydown', function (e) {
                if (e.key === 'ArrowLeft') go(current() + 1);
                if (e.key === 'ArrowRight') go(current() - 1);
            });
        }
        if (fine && !reduce) {
            slides.addEventListener('pointermove', function (e) {
                if (!wide()) return;
                var s = e.target.closest ? e.target.closest('.slide') : null;
                if (!s || !s.querySelector('img')) return;
                var r = s.getBoundingClientRect();
                s.style.setProperty('--zx', ((e.clientX - r.left) / r.width * 100).toFixed(1) + '%');
                s.style.setProperty('--zy', ((e.clientY - r.top) / r.height * 100).toFixed(1) + '%');
                s.classList.add('zoom');
            });
            slides.addEventListener('pointerleave', function () {
                each(slides.children, function (s) { s.classList.remove('zoom'); });
            });
        }
    }
    var qty = d.getElementById('qty');
    if (qty) {
        var max = parseInt(qty.getAttribute('max'), 10) || 99;
        var plus = d.getElementById('qtyPlus'), minus = d.getElementById('qtyMinus');
        if (plus) plus.addEventListener('click', function () { qty.value = Math.min(max, (parseInt(qty.value, 10) || 1) + 1); });
        if (minus) minus.addEventListener('click', function () { qty.value = Math.max(1, (parseInt(qty.value, 10) || 1) - 1); });
    }

    // ---------- قیمت زندهٔ صفحهٔ محصول (فقط نمایش؛ قیمت واقعی همیشه در سرور از دیتابیس حساب می‌شود) ----------
    var priceBox = d.querySelector('.price-lg[data-base]');
    function updatePrice() {
        if (!priceBox) return;
        var base = parseFloat(priceBox.getAttribute('data-base')) || 0, delta = 0;
        each(d.querySelectorAll('.opt input:checked'), function (i) { delta += parseFloat(i.getAttribute('data-delta')) || 0; });
        var b = priceBox.querySelector('b');
        if (b) b.textContent = Math.max(0, base + delta).toLocaleString('fa-IR');
        var changed = delta !== 0;
        priceBox.classList.toggle('priced', changed);
        var badge = d.querySelector('.gallery .badge');
        if (badge) badge.style.display = changed ? 'none' : '';
    }
    updatePrice();

    // ---------- انتخاب رنگ: متن کنار عنوان گروه به‌روز می‌شود ----------
    d.addEventListener('change', function (e) {
        var inp = e.target;
        if (!inp || !inp.closest || inp.type !== 'radio') return;
        var group = inp.closest('.opt');
        if (!group) return;
        var label = group.querySelector('[data-opt-label]');
        if (label) label.textContent = inp.value;
        group.classList.remove('need');
        updatePrice();
    });

    // ---------- علاقه‌مندی‌ها (قلب) ----------
    var bd = d.body;
    var loggedIn = bd.getAttribute('data-auth') === '1';
    var hearts = d.querySelectorAll('.heart[data-wish]');
    function paintHeart(h, on) { h.classList.toggle('on', on); h.setAttribute('aria-pressed', on ? 'true' : 'false'); }
    if (hearts.length && loggedIn) {
        fetch(bd.getAttribute('data-wish-ids'), { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (!res || !res.ids) return;
                var set = {}; res.ids.forEach(function (i) { set[i] = true; });
                each(hearts, function (h) { paintHeart(h, !!set[h.getAttribute('data-wish')]); });
            }).catch(function () { });
    }
    d.addEventListener('click', function (e) {
        var h = e.target.closest ? e.target.closest('.heart[data-wish]') : null;
        if (!h) return;
        e.preventDefault();
        if (!loggedIn) {
            window.location.href = bd.getAttribute('data-login') + '?returnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            return;
        }
        fetch(bd.getAttribute('data-wish-url'), {
            method: 'POST', credentials: 'same-origin',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-CSRF-Token': csrf() },
            body: 'id=' + encodeURIComponent(h.getAttribute('data-wish'))
        }).then(function (r) { return r.json(); })
          .then(function (res) {
              say(res.message);
              if (!res.ok) return;
              paintHeart(h, res.liked);
              h.classList.remove('pop'); void h.offsetWidth; h.classList.add('pop');
              var card = h.closest('.card');
              if (!res.liked && card && d.querySelector('[data-page="Wishlist"]')) {
                  card.style.transition = 'opacity .3s, transform .3s'; card.style.opacity = '0'; card.style.transform = 'scale(.92)';
                  setTimeout(function () { if (card.parentNode) card.parentNode.removeChild(card); }, 320);
              }
          }).catch(function () { say('خطا در ارتباط با سرور'); });
    });

    // ---------- ثبت سفارش: فرم آدرس جدید فقط وقتی «آدرس جدید» انتخاب شده نمایش داده می‌شود ----------
    var addrRadios = d.querySelectorAll('[data-addr]'), newAddr = d.getElementById('newAddr');
    if (newAddr && addrRadios.length) {
        var syncAddr = function () {
            var v = '0';
            each(addrRadios, function (r) { if (r.checked) v = r.value; });
            newAddr.classList.toggle('hide', v !== '0');
        };
        each(addrRadios, function (r) { r.addEventListener('change', syncAddr); });
        syncAddr();
    }

    // ---------- فرم‌هایی که data-confirm دارند قبل از ارسال تأیید می‌گیرند ----------
    d.addEventListener('submit', function (e) {
        var msg = e.target && e.target.getAttribute ? e.target.getAttribute('data-confirm') : null;
        if (msg && !window.confirm(msg)) e.preventDefault();
    });

    // ---------- پیشنهاد زندهٔ جستجو ----------
    var suggestUrl = bd.getAttribute('data-suggest');
    each(d.querySelectorAll('.search,.dk-search'), function (form) {
        var input = form.querySelector('input[type=search]');
        if (!input || !suggestUrl) return;
        var box = d.createElement('div'); box.className = 'suggest'; box.setAttribute('role', 'listbox');
        form.appendChild(box);
        var timer = 0, seq = 0, sel = -1;
        function close() { box.classList.remove('open'); sel = -1; }
        function render(items) {
            box.textContent = '';
            if (!items.length) { close(); return; }
            items.forEach(function (it) {
                var a = d.createElement('a'); a.href = it.url; a.setAttribute('role', 'option');
                if (it.img) { var im = d.createElement('img'); im.src = it.img; im.alt = ''; im.loading = 'lazy'; a.appendChild(im); }
                var t = d.createElement('span'); t.textContent = it.title; a.appendChild(t);
                var p = d.createElement('small'); p.textContent = it.price + ' تومان'; a.appendChild(p);
                box.appendChild(a);
            });
            box.classList.add('open'); sel = -1;
        }
        input.addEventListener('input', function () {
            clearTimeout(timer);
            var q = input.value.trim();
            if (q.length < 2) { close(); return; }
            timer = setTimeout(function () {
                var my = ++seq;
                fetch(suggestUrl + '?q=' + encodeURIComponent(q), { credentials: 'same-origin' })
                    .then(function (r) { return r.json(); })
                    .then(function (res) { if (my === seq) render(res.items || []); })
                    .catch(close);
            }, 220);
        });
        input.addEventListener('keydown', function (e) {
            var links = box.querySelectorAll('a');
            if (e.key === 'Escape') { close(); return; }
            if (!links.length || (e.key !== 'ArrowDown' && e.key !== 'ArrowUp')) return;
            e.preventDefault();
            sel = (sel + (e.key === 'ArrowDown' ? 1 : -1) + links.length) % links.length;
            each(links, function (l, i) { l.classList.toggle('sel', i === sel); });
        });
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { var s = box.querySelector('a.sel'); if (s) { e.preventDefault(); window.location.href = s.href; } }
        });
        d.addEventListener('click', function (e) { if (!form.contains(e.target)) close(); });
    });

    // ---------- مقدمه: بعد از پایان انیمیشن از صفحه حذف می‌شود ----------
    var intro = d.querySelector('.intro');
    if (intro) {
        var removeIntro = function () { if (intro.parentNode) intro.parentNode.removeChild(intro); };
        intro.addEventListener('animationend', function (e) { if (e.animationName === 'introOut') removeIntro(); });
        setTimeout(removeIntro, 3000);
    }
})();

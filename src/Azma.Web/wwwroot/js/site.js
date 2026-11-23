// فروشگاه آزما — progressive enhancements for the static SSR pages.
// Everything here is optional: pages work without JavaScript. Re-runs after Blazor enhanced navigation.
(function () {
    "use strict";
    var reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var faDigits = "۰۱۲۳۴۵۶۷۸۹";
    function toFa(n) { return String(n).replace(/\d/g, function (d) { return faDigits[+d]; }); }
    function groupFa(n) { return toFa(Math.round(n).toLocaleString("en-US")).replace(/,/g, "٬"); }

    // ---- Scroll reveal + counters ----
    var observer = null;
    function animateCount(el) {
        var target = parseFloat(el.getAttribute("data-count")) || 0;
        var suffix = el.getAttribute("data-suffix") || "";
        if (reduceMotion || target <= 0) { el.textContent = groupFa(target) + suffix; return; }
        var start = null, duration = 1400;
        function step(ts) {
            if (!start) start = ts;
            var p = Math.min(1, (ts - start) / duration);
            var eased = 1 - Math.pow(1 - p, 3);
            el.textContent = groupFa(target * eased) + suffix;
            if (p < 1) requestAnimationFrame(step);
        }
        requestAnimationFrame(step);
    }
    function initReveal() {
        var items = document.querySelectorAll(".reveal:not(.in), [data-count]:not([data-counted])");
        if (!("IntersectionObserver" in window)) {
            items.forEach(function (el) { el.classList.add("in"); if (el.hasAttribute("data-count")) { el.setAttribute("data-counted", ""); animateCount(el); } });
            return;
        }
        if (observer) observer.disconnect();
        observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                var el = entry.target;
                el.classList.add("in");
                if (el.hasAttribute("data-count") && !el.hasAttribute("data-counted")) { el.setAttribute("data-counted", ""); animateCount(el); }
                observer.unobserve(el);
            });
        }, { threshold: 0.15, rootMargin: "0px 0px -40px 0px" });
        items.forEach(function (el) { observer.observe(el); });
    }

    // ---- Header shadow on scroll ----
    var header = null;
    function onScroll() { if (header) header.classList.toggle("scrolled", window.scrollY > 8); }

    // ---- Copy to clipboard ----
    function onClick(e) {
        var btn = e.target.closest ? e.target.closest("[data-copy]") : null;
        if (!btn) {
            // close the mobile drawer after picking a link
            var link = e.target.closest ? e.target.closest(".drawer a") : null;
            if (link) { var t = document.getElementById("nav-drawer"); if (t) t.checked = false; }
            return;
        }
        e.preventDefault();
        var text = btn.getAttribute("data-copy");
        var done = function () {
            btn.classList.add("copied");
            var icon = btn.querySelector("i");
            var old = icon ? icon.className : null;
            if (icon) icon.className = "bi bi-check2";
            var label = btn.getAttribute("aria-label");
            btn.setAttribute("aria-label", "کپی شد");
            setTimeout(function () { btn.classList.remove("copied"); if (icon && old) icon.className = old; if (label) btn.setAttribute("aria-label", label); }, 1600);
        };
        if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(text).then(done, function () { });
        else {
            var ta = document.createElement("textarea");
            ta.value = text; ta.setAttribute("readonly", ""); ta.style.position = "fixed"; ta.style.opacity = "0";
            document.body.appendChild(ta); ta.select();
            try { document.execCommand("copy"); done(); } catch (_) { }
            document.body.removeChild(ta);
        }
    }

    // ---- Receipt file preview + drag & drop styling ----
    function initDropzones() {
        document.querySelectorAll(".dropzone:not([data-ready])").forEach(function (zone) {
            zone.setAttribute("data-ready", "");
            var input = zone.querySelector("input[type=file]");
            var img = zone.querySelector(".dz-preview");
            var name = zone.querySelector(".dz-name");
            if (!input) return;
            ["dragenter", "dragover"].forEach(function (ev) { zone.addEventListener(ev, function () { zone.classList.add("dragover"); }); });
            ["dragleave", "drop"].forEach(function (ev) { zone.addEventListener(ev, function () { zone.classList.remove("dragover"); }); });
            input.addEventListener("change", function () {
                var file = input.files && input.files[0];
                if (!file) { zone.classList.remove("has-file"); return; }
                zone.classList.add("has-file");
                if (name) name.textContent = file.name + " — " + toFa((file.size / 1024 / 1024).toFixed(1)) + " مگابایت";
                if (img && file.type.indexOf("image/") === 0 && window.URL) {
                    if (img.src) URL.revokeObjectURL(img.src);
                    img.src = URL.createObjectURL(file);
                }
            });
        });
        // Prevent double submits on slow mobile networks
        document.querySelectorAll("form[data-once]:not([data-ready])").forEach(function (form) {
            form.setAttribute("data-ready", "");
            form.addEventListener("submit", function () {
                var btn = form.querySelector("[type=submit]");
                if (btn) { setTimeout(function () { btn.disabled = true; }, 0); btn.classList.add("is-loading"); }
            });
        });
    }

    // ---- Hero slider (autoplay, dots, arrows, swipe; pauses on hover/focus/hidden tab) ----
    var sliderTimer = null;
    function initSlider() {
        if (sliderTimer) { clearInterval(sliderTimer); sliderTimer = null; }
        var root = document.querySelector(".hero-slider");
        if (!root) return;
        var slides = root.querySelectorAll(".hero-slide");
        var dots = root.querySelectorAll(".hero-dots button");
        if (slides.length < 2) return;
        var interval = Math.max(3, parseInt(root.getAttribute("data-interval"), 10) || 6) * 1000;
        root.style.setProperty("--hero-interval", interval / 1000 + "s");
        var index = 0, paused = false;
        function show(i) {
            index = (i + slides.length) % slides.length;
            slides.forEach(function (s, n) {
                var on = n === index;
                s.classList.toggle("is-active", on);
                s.setAttribute("aria-hidden", on ? "false" : "true");
                s.querySelectorAll("a,button").forEach(function (el) { if (on) el.removeAttribute("tabindex"); else el.setAttribute("tabindex", "-1"); });
            });
            dots.forEach(function (d, n) {
                d.classList.remove("is-active");
                if (n === index) { void d.offsetWidth; d.classList.add("is-active"); }
                d.setAttribute("aria-current", n === index ? "true" : "false");
            });
        }
        function restart() {
            if (sliderTimer) clearInterval(sliderTimer);
            if (reduceMotion) return;
            sliderTimer = setInterval(function () { if (!paused && !document.hidden) show(index + 1); }, interval);
        }
        function setPaused(p) { paused = p; root.classList.toggle("is-paused", p); }
        if (!root.hasAttribute("data-ready")) {
            root.setAttribute("data-ready", "");
            dots.forEach(function (d, n) { d.addEventListener("click", function () { show(n); restart(); }); });
            var prev = root.querySelector("[data-slide=prev]"), next = root.querySelector("[data-slide=next]");
            if (prev) prev.addEventListener("click", function () { show(index - 1); restart(); });
            if (next) next.addEventListener("click", function () { show(index + 1); restart(); });
            root.addEventListener("mouseenter", function () { setPaused(true); });
            root.addEventListener("mouseleave", function () { setPaused(false); });
            root.addEventListener("focusin", function () { setPaused(true); });
            root.addEventListener("focusout", function () { setPaused(false); });
            root.addEventListener("keydown", function (e) {
                // RTL: the right arrow goes to the previous slide
                if (e.key === "ArrowLeft") { show(index + 1); restart(); }
                else if (e.key === "ArrowRight") { show(index - 1); restart(); }
            });
            var x0 = null;
            root.addEventListener("touchstart", function (e) { x0 = e.touches[0].clientX; }, { passive: true });
            root.addEventListener("touchend", function (e) {
                if (x0 === null) return;
                var dx = e.changedTouches[0].clientX - x0; x0 = null;
                if (Math.abs(dx) > 40) { show(dx > 0 ? index + 1 : index - 1); restart(); }
            }, { passive: true });
        }
        show(0);
        restart();
    }

    // ---- Thin progress bar while a page or form submit is loading ----
    var bar = null;
    function progressStart() {
        if (!bar) { bar = document.createElement("div"); bar.className = "nav-progress"; bar.setAttribute("aria-hidden", "true"); document.body.appendChild(bar); }
        bar.classList.remove("is-active"); void bar.offsetWidth; bar.classList.add("is-active");
    }
    function progressDone() { if (bar) bar.classList.remove("is-active"); }
    document.addEventListener("click", function (e) {
        if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        var a = e.target.closest ? e.target.closest("a[href]") : null;
        if (!a || a.target === "_blank" || a.hasAttribute("download")) return;
        var url;
        try { url = new URL(a.href, location.href); } catch (_) { return; }
        if (url.origin !== location.origin || (url.pathname === location.pathname && url.search === location.search)) return;
        progressStart();
    });
    document.addEventListener("submit", function (e) { if (!e.defaultPrevented) progressStart(); });
    window.addEventListener("pageshow", progressDone);

    function init() {
        document.documentElement.classList.remove("no-js");
        progressDone();
        initSlider();
        header = document.querySelector(".site-header");
        onScroll();
        initReveal();
        initDropzones();
        var drawer = document.getElementById("nav-drawer");
        if (drawer) drawer.checked = false;
    }

    document.addEventListener("click", onClick);
    // Labels acting as buttons (drawer open/close): support Enter / Space and Escape.
    document.addEventListener("keydown", function (e) {
        var t = document.getElementById("nav-drawer");
        if (e.key === "Escape" && t && t.checked) { t.checked = false; return; }
        var lbl = e.target.closest ? e.target.closest("label[for=nav-drawer][role=button]") : null;
        if (lbl && (e.key === "Enter" || e.key === " ")) { e.preventDefault(); if (t) t.checked = !t.checked; }
    });
    window.addEventListener("scroll", onScroll, { passive: true });
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", init); else init();
    // Blazor enhanced navigation swaps page content without a full reload.
    if (window.Blazor && window.Blazor.addEventListener) window.Blazor.addEventListener("enhancedload", init);
    else document.addEventListener("DOMContentLoaded", function () {
        if (window.Blazor && window.Blazor.addEventListener) window.Blazor.addEventListener("enhancedload", init);
    });
})();

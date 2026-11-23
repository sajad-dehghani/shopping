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

    function init() {
        document.documentElement.classList.remove("no-js");
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

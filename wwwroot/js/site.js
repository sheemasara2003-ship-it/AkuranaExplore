/* ==========================================================================
   AkuranaExplore — client interactions
   ========================================================================== */

(function () {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    /* ----------------------------------------------------------------------
       Sticky header state
       ---------------------------------------------------------------------- */

    var header = document.querySelector("[data-header]");

    function updateHeader() {
        if (!header) return;
        header.classList.toggle("is-scrolled", window.scrollY > 12);
    }

    updateHeader();
    window.addEventListener("scroll", updateHeader, { passive: true });

    /* ----------------------------------------------------------------------
       Mobile navigation
       ---------------------------------------------------------------------- */

    var navToggle = document.querySelector("[data-nav-toggle]");

    function closeNav() {
        if (!header || !navToggle) return;
        header.classList.remove("nav-open");
        navToggle.setAttribute("aria-expanded", "false");
    }

    if (header && navToggle) {
        navToggle.addEventListener("click", function () {
            var isOpen = header.classList.toggle("nav-open");
            navToggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
        });

        header.querySelectorAll(".primary-nav a").forEach(function (link) {
            link.addEventListener("click", closeNav);
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape") closeNav();
            if (event.key === "Escape") closeOpenDetails();
        });

        document.addEventListener("click", function (event) {
            if (!header.classList.contains("nav-open")) return;
            if (!header.contains(event.target)) closeNav();
        });
    }

    /* ----------------------------------------------------------------------
       Auto-submit filter selects
       ---------------------------------------------------------------------- */

    document.querySelectorAll("select[data-auto-submit]").forEach(function (select) {
        select.addEventListener("change", function () {
            var form = select.form;
            if (!form) return;
            if (typeof form.requestSubmit === "function") {
                form.requestSubmit();
            } else {
                form.submit();
            }
        });
    });

    /* ----------------------------------------------------------------------
       Flash messages
       ---------------------------------------------------------------------- */

    document.querySelectorAll("[data-flash]").forEach(function (flash) {
        var dismiss = flash.querySelector("[data-dismiss]");

        function remove() {
            flash.style.transition = "opacity 0.25s ease, transform 0.25s ease";
            flash.style.opacity = "0";
            flash.style.transform = "translateY(-6px)";
            window.setTimeout(function () {
                flash.remove();
            }, 260);
        }

        if (dismiss) dismiss.addEventListener("click", remove);

        var timer = window.setTimeout(remove, 6000);
        flash.addEventListener("mouseenter", function () { window.clearTimeout(timer); });
    });

    /* ----------------------------------------------------------------------
       Confirmation for destructive actions
       ---------------------------------------------------------------------- */

    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var message = form.getAttribute("data-confirm") || "Are you sure?";
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    /* ----------------------------------------------------------------------
       Planner — copy and print
       ---------------------------------------------------------------------- */

    var copyButton = document.querySelector("[data-copy-plan]");

    function buildPlanText() {
        var stops = Array.prototype.slice.call(document.querySelectorAll("[data-plan-stop]"));
        if (stops.length === 0) return "";

        var lines = ["AkuranaExplore — one-day visit plan", ""];

        stops.forEach(function (stop, index) {
            var name = stop.getAttribute("data-plan-name") || "Stop";
            var time = stop.getAttribute("data-plan-time") || "";
            var meta = stop.getAttribute("data-plan-meta") || "";
            lines.push((index + 1) + ". " + name);
            if (time) lines.push("   " + time);
            if (meta) lines.push("   " + meta);
        });

        var note = document.querySelector(".plan-finish-note");
        if (note) {
            lines.push("");
            lines.push(note.textContent.replace(/\s+/g, " ").trim());
        }

        lines.push("");
        lines.push("Distances and travel times are estimates. Confirm opening hours before travelling.");
        return lines.join("\n");
    }

    if (copyButton) {
        copyButton.addEventListener("click", function () {
            var text = buildPlanText();
            if (!text) return;

            function done() {
                var original = copyButton.innerHTML;
                copyButton.innerHTML = "✓ Copied to clipboard";
                window.setTimeout(function () {
                    copyButton.innerHTML = original;
                }, 2200);
            }

            if (navigator.clipboard && window.isSecureContext) {
                navigator.clipboard.writeText(text).then(done).catch(function () {
                    fallbackCopy(text, done);
                });
            } else {
                fallbackCopy(text, done);
            }
        });
    }

    function fallbackCopy(text, done) {
        var textarea = document.createElement("textarea");
        textarea.value = text;
        textarea.setAttribute("readonly", "");
        textarea.style.position = "fixed";
        textarea.style.top = "-1000px";
        document.body.appendChild(textarea);
        textarea.select();

        try {
            document.execCommand("copy");
            done();
        } catch (error) {
            window.alert("Copying is not available in this browser. You can print the plan instead.");
        } finally {
            document.body.removeChild(textarea);
        }
    }

    var printButton = document.querySelector("[data-print-plan]");
    if (printButton) {
        printButton.addEventListener("click", function () {
            window.print();
        });
    }

    /* ----------------------------------------------------------------------
       Close open category editors when clicking elsewhere
       ---------------------------------------------------------------------- */

    function closeOpenDetails() {
        document.querySelectorAll("details[open].row-edit").forEach(function (details) {
            if (!details.contains(document.activeElement)) details.removeAttribute("open");
        });
    }

    document.addEventListener("click", function (event) {
        document.querySelectorAll("details[open].row-edit").forEach(function (details) {
            if (!details.contains(event.target)) details.removeAttribute("open");
        });
    });

    /* ----------------------------------------------------------------------
       Scroll to validation problems after a failed submit
       ---------------------------------------------------------------------- */

    window.addEventListener("load", function () {
        var summary = document.querySelector(".validation-summary-errors, .field-validation-error");
        if (!summary) return;

        var container = summary.closest(".panel, .auth-card, form") || summary;
        container.scrollIntoView({ behavior: reduceMotion ? "auto" : "smooth", block: "center" });

        var firstInvalid = container.querySelector(".input-validation-error");
        if (firstInvalid) {
            window.setTimeout(function () { firstInvalid.focus({ preventScroll: true }); }, 350);
        }
    });

    /* ----------------------------------------------------------------------
       Gentle reveal on scroll
       ---------------------------------------------------------------------- */

    if (!reduceMotion && "IntersectionObserver" in window) {
        var targets = document.querySelectorAll(
            ".place-card, .category-card, .stat-card, .panel, .stop-card, .nearby-item, .steps-list li"
        );

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                entry.target.classList.add("is-visible");
                observer.unobserve(entry.target);
            });
        }, { threshold: 0.08, rootMargin: "0px 0px -30px 0px" });

        targets.forEach(function (target, index) {
            target.classList.add("reveal");
            target.style.transitionDelay = Math.min(index % 4, 3) * 55 + "ms";
            observer.observe(target);
        });
    }
})();

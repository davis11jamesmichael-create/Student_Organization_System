/* site.js — UI behaviour for the Student Organization System */
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {

        /* ── Sidebar drawer (small screens) ── */
        var toggle = document.getElementById("sidebarToggle");
        var backdrop = document.getElementById("sidebarBackdrop");
        var sidebar = document.getElementById("appSidebar");
        var MOBILE_BREAKPOINT = 1024;

        function isOpen() {
            return document.body.classList.contains("sidebar-open");
        }

        function openSidebar() {
            document.body.classList.add("sidebar-open");
            if (toggle) toggle.setAttribute("aria-expanded", "true");
        }

        function closeSidebar() {
            document.body.classList.remove("sidebar-open");
            if (toggle) toggle.setAttribute("aria-expanded", "false");
        }

        if (toggle) {
            toggle.addEventListener("click", function () {
                if (isOpen()) {
                    closeSidebar();
                } else {
                    openSidebar();
                }
            });
        }

        if (backdrop) {
            backdrop.addEventListener("click", closeSidebar);
        }

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && isOpen()) {
                closeSidebar();
            }
        });

        // Tapping a menu item on a small screen closes the drawer
        if (sidebar) {
            var links = sidebar.querySelectorAll("a.nav-item:not(.is-soon)");
            for (var i = 0; i < links.length; i++) {
                links[i].addEventListener("click", function () {
                    if (window.innerWidth < MOBILE_BREAKPOINT) {
                        closeSidebar();
                    }
                });
            }

            // Placeholder items for modules that are not built yet do nothing
            var placeholders = sidebar.querySelectorAll("a.nav-item.is-soon");
            for (var j = 0; j < placeholders.length; j++) {
                placeholders[j].addEventListener("click", function (event) {
                    event.preventDefault();
                });
            }
        }

        // Never leave the drawer open after resizing back to desktop
        window.addEventListener("resize", function () {
            if (window.innerWidth >= MOBILE_BREAKPOINT && isOpen()) {
                closeSidebar();
            }
        });

        /* ── Auto-hide success alerts ── */
        var alerts = document.querySelectorAll(".alert-success");
        for (var a = 0; a < alerts.length; a++) {
            (function (alertBox) {
                window.setTimeout(function () {
                    alertBox.style.transition = "opacity 400ms ease";
                    alertBox.style.opacity = "0";
                    window.setTimeout(function () {
                        if (alertBox.parentNode) {
                            alertBox.parentNode.removeChild(alertBox);
                        }
                    }, 420);
                }, 5000);
            })(alerts[a]);
        }
        /* ── Attendance: tri-state marking (module 3F) ── */
        var attendanceForm = document.getElementById("attendance-form");

        if (attendanceForm) {
            var counter = document.getElementById("unmarked-count");
            var rows = Array.prototype.slice.call(
                attendanceForm.querySelectorAll("[data-member-row]"));

            var updateCounter = function () {
                var marked = 0;
                for (var r = 0; r < rows.length; r++) {
                    if (rows[r].querySelector('input[type="radio"]:checked')) {
                        marked++;
                    }
                }

                var unmarked = rows.length - marked;

                if (counter) {
                    counter.dataset.unmarked = unmarked;
                    counter.textContent = unmarked === 0
                        ? "All members marked"
                        : unmarked + " member(s) not yet marked";
                    counter.classList.toggle("is-clear", unmarked === 0);
                }
            };

            attendanceForm.addEventListener("change", function (event) {
                var radio = event.target;
                if (radio.type !== "radio") { return; }

                var group = radio.closest(".radio-group");
                if (group) {
                    var options = group.querySelectorAll(".radio-option");
                    for (var o = 0; o < options.length; o++) {
                        options[o].classList.remove("selected");
                    }
                }

                var label = radio.closest(".radio-option");
                if (label) { label.classList.add("selected"); }

                updateCounter();
            });

            var markAllButtons = attendanceForm.querySelectorAll("[data-mark-all]");
            for (var m = 0; m < markAllButtons.length; m++) {
                markAllButtons[m].addEventListener("click", function () {
                    var value = this.dataset.markAll;

                    for (var r2 = 0; r2 < rows.length; r2++) {
                        var radio = rows[r2].querySelector(
                            'input[type="radio"][value="' + value + '"]');

                        if (radio) {
                            radio.checked = true;
                            radio.dispatchEvent(new Event("change", { bubbles: true }));
                        }
                    }

                    updateCounter();
                });
            }

            attendanceForm.addEventListener("submit", function (event) {
                var unmarked = parseInt(
                    (counter && counter.dataset.unmarked) || "0", 10);

                if (unmarked > 0) {
                    var message = unmarked === 1
                        ? "1 member has not been marked. Saving now will not record a status for them. Continue?"
                        : unmarked + " members have not been marked. Saving now will not record a status for them. Continue?";

                    if (!window.confirm(message)) {
                        event.preventDefault();
                    }
                }
            });

            updateCounter();
        }

        /* ── Participation: clear every type box (module 3H) ── */
        var clearAll = document.getElementById("clear-all");

        if (clearAll) {
            clearAll.addEventListener("click", function () {
                var inputs = document.querySelectorAll(".participation-input");
                for (var i = 0; i < inputs.length; i++) {
                    inputs[i].value = "";
                }
            });
        }

        /* ── Reports: the Print button (module 3I) ── */
        var printReport = document.getElementById("print-report");

        if (printReport) {
            printReport.addEventListener("click", function () {
                window.print();
            });
        }

        /* ── Show / hide password fields (data-toggle-password) ── */
        var passwordToggles = document.querySelectorAll("[data-toggle-password]");

        for (var p = 0; p < passwordToggles.length; p++) {
            passwordToggles[p].addEventListener("click", function () {
                var field = this.closest(".password-field");
                var input = field ? field.querySelector("input") : null;

                if (!input) { return; }

                var isHidden = input.type === "password";

                input.type = isHidden ? "text" : "password";
                this.textContent = isHidden ? "Hide" : "Show";
                this.setAttribute("aria-pressed", isHidden ? "true" : "false");
                this.setAttribute(
                    "aria-label",
                    isHidden ? "Hide password" : "Show password");
            });
        }

        /* ── Any POST form may ask for confirmation with data-confirm="..." ── */
        document.addEventListener("submit", function (event) {
            var form = event.target;
            var message = form && form.getAttribute
                ? form.getAttribute("data-confirm")
                : null;

            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });

        /* ── Back buttons (data-back): prefer history, fall back to the href ── */
        var backLinks = document.querySelectorAll("[data-back]");

        for (var b = 0; b < backLinks.length; b++) {
            backLinks[b].addEventListener("click", function (event) {
                if (!document.referrer) {
                    return;
                }

                var referrer;

                try {
                    referrer = new URL(document.referrer);
                } catch (error) {
                    return; // Unparsable referrer: just follow the href.
                }

                // Only a page from this same site is a safe place to go back to;
                // otherwise the link's own href (a known-good page) is followed.
                if (referrer.origin !== window.location.origin) {
                    return;
                }

                event.preventDefault();
                window.history.back();
            });
        }
    });
})();

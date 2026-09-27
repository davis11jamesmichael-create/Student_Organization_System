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
    });
})();

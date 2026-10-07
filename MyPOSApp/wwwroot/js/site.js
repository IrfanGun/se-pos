(() => {
    const shell = document.querySelector(".app-shell");
    const toggle = document.getElementById("sidebarToggle");
    const toggleIcon = document.getElementById("sidebarToggleIcon");
    if (!shell || !toggle || !toggleIcon) return;

    const storageKey = "mypos.sidebar.collapsed";
    const setCollapsed = (collapsed) => {
        shell.classList.toggle("is-sidebar-collapsed", collapsed);
        toggle.setAttribute("aria-expanded", String(!collapsed));
        const label = collapsed ? "Tampilkan sidebar" : "Ciutkan sidebar";
        toggle.setAttribute("aria-label", label);
        toggle.setAttribute("title", label);
        toggleIcon.classList.toggle("lucide-panel-left-open", collapsed);
        toggleIcon.classList.toggle("lucide-panel-left-close", !collapsed);
    };

    try {
        setCollapsed(localStorage.getItem(storageKey) === "true");
    } catch {
        setCollapsed(false);
    }

    toggle.addEventListener("click", () => {
        const collapsed = !shell.classList.contains("is-sidebar-collapsed");
        setCollapsed(collapsed);
        try {
            localStorage.setItem(storageKey, String(collapsed));
        } catch {
            // Sidebar remains usable when browser storage is unavailable.
        }
    });
})();

window.positionFilterPanel = (buttonId, panelId) => {
    const btn = document.getElementById(buttonId);
    const panel = document.getElementById(panelId);

    if (!btn || !panel) return;

    const panelContent = panel.querySelector(".budget-filter-panel-content");
    const panelWidth = panelContent ? panelContent.offsetWidth : panel.offsetWidth;
    const btnCenter = btn.offsetLeft + btn.offsetWidth / 2;

    panel.style.position = "absolute";
    panel.style.top = (btn.offsetTop + btn.offsetHeight + 6) + "px";
    panel.style.left = (btnCenter - panelWidth / 2) + "px";
};

window.registerClickOutside = (dropdownId, dotNetRef) => {
    document.addEventListener('click', function (event) {
        const dropdown = document.getElementById(dropdownId);
        if (!dropdown) return;

        if (!dropdown.contains(event.target)) {
            dotNetRef.invokeMethodAsync('CloseDropdown');
        }
    });
};

window.sidebarState = (() => {
    const storageKey = "collapsed";
    const expandedDelayMs = 100;
    const collapsedDelayMs = 750;
    let hoverExpandTimeout = null;
    let hoverCollapseTimeout = null;
    let hoverPreviewExpanded = false;
    let startupHoverPreviewBlocked = false;
    let hoverBound = false;
    let initialized = false;

    const getElements = () => ({
        sidebar: document.getElementById("app-sidebar"),
        mainContent: document.getElementById("app-main-content"),
        navmenu: document.getElementById("app-navmenu"),
        toggleButton: document.querySelector(".sidebar-toggle")
    });

    const getCollapsed = () => {
        try {
            return window.localStorage.getItem(storageKey) === "true";
        } catch {
            return false;
        }
    };

    const setCollapsed = (value) => {
        try {
            window.localStorage.setItem(storageKey, value ? "true" : "false");
        } catch {
        }
    };

    const isDesktop = () => window.matchMedia("(min-width: 641px)").matches;

    const clearHoverExpandTimeout = () => {
        if (hoverExpandTimeout) {
            window.clearTimeout(hoverExpandTimeout);
            hoverExpandTimeout = null;
        }
    };

    const clearHoverCollapseTimeout = () => {
        if (hoverCollapseTimeout) {
            window.clearTimeout(hoverCollapseTimeout);
            hoverCollapseTimeout = null;
        }
    };

    const clearHoverTimeouts = () => {
        clearHoverExpandTimeout();
        clearHoverCollapseTimeout();
    };

    const hasRequiredElements = () => {
        const { sidebar, mainContent, navmenu } = getElements();
        return Boolean(sidebar && mainContent && navmenu);
    };

    const apply = (collapsed) => {
        const { sidebar, mainContent, navmenu, toggleButton } = getElements();
        const effectiveCollapsed = collapsed && !(isDesktop() && hoverPreviewExpanded);

        if (!sidebar || !mainContent || !navmenu) {
            return;
        }

        sidebar.classList.toggle("collapsed", effectiveCollapsed);
        sidebar.classList.toggle("expanded", !effectiveCollapsed);

        mainContent.classList.toggle("collapsed", effectiveCollapsed);
        mainContent.classList.toggle("expanded", !effectiveCollapsed);

        navmenu.classList.toggle("collapsed", effectiveCollapsed);
        navmenu.classList.toggle("expanded", !effectiveCollapsed);

        if (toggleButton) {
            toggleButton.setAttribute("aria-expanded", effectiveCollapsed ? "false" : "true");
            toggleButton.setAttribute("aria-label", collapsed ? "Przypnij panel" : "Odepnij panel");
            toggleButton.setAttribute("title", collapsed ? "Przypnij panel" : "Odepnij panel");
        }
    };

    const bindHoverPreview = () => {
        if (hoverBound) {
            return;
        }

        const { sidebar } = getElements();

        if (!sidebar) {
            return;
        }

        sidebar.addEventListener("mouseenter", () => {
            if (!isDesktop() || !getCollapsed()) {
                return;
            }

            clearHoverCollapseTimeout();
            clearHoverExpandTimeout();

            if (startupHoverPreviewBlocked) {
                return;
            }

            hoverExpandTimeout = window.setTimeout(() => {
                hoverPreviewExpanded = true;
                apply(getCollapsed());
                hoverExpandTimeout = null;
            }, expandedDelayMs);
        });

        sidebar.addEventListener("mouseleave", () => {
            if (!isDesktop() || !getCollapsed()) {
                return;
            }

            clearHoverExpandTimeout();
            clearHoverCollapseTimeout();
            startupHoverPreviewBlocked = false;

            hoverCollapseTimeout = window.setTimeout(() => {
                hoverPreviewExpanded = false;
                apply(getCollapsed());
                hoverCollapseTimeout = null;
            }, collapsedDelayMs);
        });

        hoverBound = true;
    };

    const init = () => {
        const { sidebar } = getElements();

        if (!hasRequiredElements()) {
            return false;
        }

        hoverPreviewExpanded = false;
        startupHoverPreviewBlocked = isDesktop() && getCollapsed() && sidebar.matches(":hover");
        clearHoverTimeouts();
        apply(getCollapsed());
        bindHoverPreview();
        initialized = true;
        return true;
    };

    const toggle = () => {
        const collapsed = !getCollapsed();
        hoverPreviewExpanded = false;
        startupHoverPreviewBlocked = false;
        clearHoverTimeouts();
        setCollapsed(collapsed);
        apply(collapsed);
    };

    if (hasRequiredElements()) {
        init();
    } else {
        document.addEventListener("DOMContentLoaded", () => {
            if (!initialized) {
                init();
            }
        }, { once: true });
    }

    return {
        getCollapsed,
        setCollapsed,
        apply,
        init,
        toggle
    };
})();

window.themeStorage = (() => {
    const storageKey = "theme";
    const darkThemeValue = "dark";
    const lightThemeValue = "light";

    const applyTheme = (enabled) => {
        document.documentElement.dataset.theme = enabled ? darkThemeValue : lightThemeValue;
        return enabled;
    };

    const getDarkMode = () => {
        try {
            return window.localStorage.getItem(storageKey) === darkThemeValue;
        } catch {
            return false;
        }
    };

    const setDarkMode = (enabled) => {
        try {
            window.localStorage.setItem(storageKey, enabled ? darkThemeValue : lightThemeValue);
        } catch {
        }
        return applyTheme(enabled);
    };

    const applyStoredTheme = () => applyTheme(getDarkMode());

    const keepThemeAttributeInSync = () => {
        const observer = new MutationObserver(() => {
            const expectedTheme = getDarkMode() ? darkThemeValue : lightThemeValue;

            if (document.documentElement.dataset.theme !== expectedTheme) {
                applyStoredTheme();
            }
        });

        observer.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ["data-theme"]
        });
    };

    const registerEnhancedNavigationHandler = () => {
        if (!window.Blazor || typeof window.Blazor.addEventListener !== "function") {
            return false;
        }

        window.Blazor.addEventListener("enhancedload", applyStoredTheme);
        return true;
    };

    applyStoredTheme();
    keepThemeAttributeInSync();

    if (!registerEnhancedNavigationHandler()) {
        window.setTimeout(registerEnhancedNavigationHandler, 0);
        document.addEventListener("DOMContentLoaded", registerEnhancedNavigationHandler, { once: true });
    }

    return {
        getDarkMode,
        setDarkMode,
        applyTheme
    };
})();

window.adaptivePagination = (() => {
    const registrations = new Map();
    let nextRegistrationId = 1;

    const clamp = (value, min, max) => Math.min(Math.max(value, min), max);

    const getNumericStyleValue = (style, propertyName) => {
        const value = Number.parseFloat(style.getPropertyValue(propertyName));
        return Number.isFinite(value) ? value : 0;
    };

    const getElementHeightWithMargins = (element) => {
        if (!element) {
            return 0;
        }

        const rect = element.getBoundingClientRect();
        const style = window.getComputedStyle(element);

        return rect.height
            + getNumericStyleValue(style, "margin-top")
            + getNumericStyleValue(style, "margin-bottom");
    };

    const getVisibleItems = (container, itemSelector) => {
        return Array.from(container.querySelectorAll(itemSelector))
            .filter((item) => item.getClientRects().length > 0);
    };

    const getItemStepHeight = (items, fallbackItemHeight) => {
        if (items.length > 1) {
            const firstTop = items[0].getBoundingClientRect().top;

            for (const item of items.slice(1)) {
                const nextTop = item.getBoundingClientRect().top;
                const stepHeight = nextTop - firstTop;

                if (Number.isFinite(stepHeight) && stepHeight > 0) {
                    return stepHeight;
                }
            }
        }

        const measuredItemHeight = getElementHeightWithMargins(items[0]);
        return measuredItemHeight > 0 ? measuredItemHeight : fallbackItemHeight;
    };

    const normalizeOptions = (options) => {
        const minItems = Number(options?.minItems ?? options?.MinItems ?? 6);
        const maxItems = Number(options?.maxItems ?? options?.MaxItems ?? 100);
        const fallbackItemHeight = Number(options?.fallbackItemHeight ?? options?.FallbackItemHeight ?? 44);

        return {
            minItems: Math.max(1, Number.isFinite(minItems) ? Math.floor(minItems) : 6),
            maxItems: Math.max(1, Number.isFinite(maxItems) ? Math.floor(maxItems) : 100),
            fallbackItemHeight: Math.max(1, Number.isFinite(fallbackItemHeight) ? fallbackItemHeight : 44),
            reservedSelector: options?.reservedSelector ?? options?.ReservedSelector ?? null,
            useViewportBottom: Boolean(options?.useViewportBottom ?? options?.UseViewportBottom ?? false)
        };
    };

    const calculatePageSize = (container, itemSelector, options) => {
        if (!container || !container.isConnected) {
            return null;
        }

        const items = getVisibleItems(container, itemSelector);
        if (items.length === 0) {
            return null;
        }

        const normalizedOptions = normalizeOptions(options);
        const minItems = normalizedOptions.minItems;
        const maxItems = Math.max(normalizedOptions.maxItems, minItems);
        const containerRect = container.getBoundingClientRect();
        const firstItem = items[0];
        const itemRect = firstItem.getBoundingClientRect();
        const itemHeight = getItemStepHeight(items, normalizedOptions.fallbackItemHeight);

        const viewportBottom = document.documentElement.clientHeight || window.innerHeight;
        const visibleContainerBottom = normalizedOptions.useViewportBottom
            ? viewportBottom
            : Math.min(containerRect.bottom, viewportBottom);
        let availableHeight = visibleContainerBottom - itemRect.top;

        if (normalizedOptions.reservedSelector) {
            const reservedElement = container.querySelector(normalizedOptions.reservedSelector);

            if (reservedElement && reservedElement.getClientRects().length > 0) {
                availableHeight = reservedElement.getBoundingClientRect().top - itemRect.top;
            }
        }

        if (!Number.isFinite(availableHeight) || availableHeight <= 0) {
            return minItems;
        }

        return clamp(Math.floor(availableHeight / itemHeight), minItems, maxItems);
    };

    const unregister = (registrationId) => {
        const registration = registrations.get(registrationId);
        if (!registration) {
            return;
        }

        if (registration.animationFrameId) {
            window.cancelAnimationFrame(registration.animationFrameId);
        }

        registration.resizeObserver.disconnect();
        registration.mutationObserver.disconnect();
        window.removeEventListener("resize", registration.notify);
        registrations.delete(registrationId);
    };

    const register = (container, itemSelector, dotNetRef, options) => {
        const registrationId = `adaptive-pagination-${nextRegistrationId++}`;
        let lastPageSize = null;
        const registration = {
            animationFrameId: 0,
            resizeObserver: null,
            mutationObserver: null,
            notify: null
        };

        if (!container) {
            return registrationId;
        }

        const notify = () => {
            if (registration.animationFrameId) {
                return;
            }

            registration.animationFrameId = window.requestAnimationFrame(async () => {
                registration.animationFrameId = 0;

                const pageSize = calculatePageSize(container, itemSelector, options);
                if (pageSize === null || pageSize === lastPageSize) {
                    return;
                }

                lastPageSize = pageSize;

                try {
                    await dotNetRef.invokeMethodAsync("OnPageSizeChanged", pageSize);
                } catch {
                    unregister(registrationId);
                }
            });
        };

        const resizeObserver = new ResizeObserver(notify);
        const mutationObserver = new MutationObserver(notify);
        registration.resizeObserver = resizeObserver;
        registration.mutationObserver = mutationObserver;
        registration.notify = notify;

        resizeObserver.observe(container);
        mutationObserver.observe(container, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ["class", "style"]
        });

        window.addEventListener("resize", notify, { passive: true });

        registrations.set(registrationId, registration);

        notify();
        window.setTimeout(notify, 50);
        window.setTimeout(notify, 150);
        window.setTimeout(notify, 350);
        return registrationId;
    };

    return {
        register,
        unregister
    };
})();

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

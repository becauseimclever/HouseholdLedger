window.householdLedger = {
    setTheme(theme) {
        const supportedThemes = {
            "workbench-dark": "#181818",
            "workbench-light": "#f3f3f3"
        };
        const themeColor = supportedThemes[theme];
        if (!themeColor) {
            return;
        }

        document.documentElement.dataset.theme = theme;
        document.querySelector('meta[name="theme-color"]')?.setAttribute("content", themeColor);
    }
};

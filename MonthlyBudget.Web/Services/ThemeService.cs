using Microsoft.JSInterop;

namespace MonthlyBudget.Web.Services;

public sealed class ThemeService
{
    private readonly IJSRuntime jsRuntime;
    private bool isInitialized;

    public ThemeService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public event Action? ThemeChanged;

    public bool IsDarkMode { get; private set; }

    public async Task InitializeAsync()
    {
        if (isInitialized)
        {
            return;
        }

        IsDarkMode = await jsRuntime.InvokeAsync<bool>("themeStorage.getDarkMode");
        isInitialized = true;
        await jsRuntime.InvokeVoidAsync("themeStorage.applyTheme", IsDarkMode);
        NotifyThemeChanged();
    }

    public async Task SetDarkModeAsync(bool isDarkMode)
    {
        if (isInitialized && IsDarkMode == isDarkMode)
        {
            return;
        }

        IsDarkMode = isDarkMode;
        isInitialized = true;

        await jsRuntime.InvokeVoidAsync("themeStorage.setDarkMode", isDarkMode);
        await jsRuntime.InvokeVoidAsync("themeStorage.applyTheme", isDarkMode);
        NotifyThemeChanged();
    }

    public Task ToggleThemeAsync()
    {
        return SetDarkModeAsync(!IsDarkMode);
    }

    private void NotifyThemeChanged()
    {
        ThemeChanged?.Invoke();
    }
}

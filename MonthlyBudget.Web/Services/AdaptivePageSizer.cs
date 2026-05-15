using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MonthlyBudget.Web.Services;

public sealed class AdaptivePageSizerOptions
{
    [JsonPropertyName("minItems")]
    public int MinItems { get; set; } = 6;

    [JsonPropertyName("maxItems")]
    public int MaxItems { get; set; } = 100;

    [JsonPropertyName("fallbackItemHeight")]
    public double FallbackItemHeight { get; set; } = 44;

    [JsonPropertyName("reservedSelector")]
    public string? ReservedSelector { get; set; }

    [JsonPropertyName("useViewportBottom")]
    public bool UseViewportBottom { get; set; }
}

public sealed class AdaptivePageSizer : IAsyncDisposable
{
    private readonly IJSRuntime jsRuntime;
    private readonly Func<int, Task> onPageSizeChanged;
    private DotNetObjectReference<AdaptivePageSizer>? dotNetReference;
    private string? registrationId;
    private bool isDisposed;
    private int currentPageSize;

    public AdaptivePageSizer(IJSRuntime jsRuntime, Func<int, Task> onPageSizeChanged)
    {
        this.jsRuntime = jsRuntime;
        this.onPageSizeChanged = onPageSizeChanged;
    }

    public async Task RegisterAsync(ElementReference container, string itemSelector, AdaptivePageSizerOptions options)
    {
        if (isDisposed)
        {
            return;
        }

        dotNetReference ??= DotNetObjectReference.Create(this);
        registrationId = await jsRuntime.InvokeAsync<string>(
            "adaptivePagination.register",
            container,
            itemSelector,
            dotNetReference,
            options);
    }

    [JSInvokable]
    public async Task OnPageSizeChanged(int pageSize)
    {
        if (isDisposed || pageSize < 1 || pageSize == currentPageSize)
        {
            return;
        }

        currentPageSize = pageSize;
        await onPageSizeChanged(pageSize);
    }

    public async ValueTask DisposeAsync()
    {
        isDisposed = true;

        if (!string.IsNullOrWhiteSpace(registrationId))
        {
            try
            {
                await jsRuntime.InvokeVoidAsync("adaptivePagination.unregister", registrationId);
            }
            catch (Exception ex) when (ex is InvalidOperationException
                || ex is JSException
                || ex is JSDisconnectedException
                || ex is ObjectDisposedException)
            {
            }
        }

        dotNetReference?.Dispose();
    }
}

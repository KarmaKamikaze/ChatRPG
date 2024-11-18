using Microsoft.JSInterop;

namespace ChatRPG.Services;

public class JsInteropService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private IJSObjectReference? _scrollModule;
    private IJSObjectReference? _detectScrollBarModule;

    public async Task<IJSObjectReference> GetScrollModuleAsync()
    {
        return _scrollModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/scroll.js");
    }

    public async Task<IJSObjectReference> GetDetectScrollBarModuleAsync()
    {
        return _detectScrollBarModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/detectScrollBar.js");
    }

    public async ValueTask DisposeAsync()
    {
        if (_scrollModule != null)
        {
            await _scrollModule.DisposeAsync();
        }
    }
}

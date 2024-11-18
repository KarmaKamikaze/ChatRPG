using Microsoft.JSInterop;

namespace ChatRPG.Services;

public class JsInteropService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _scrollModule;
    private IJSObjectReference? _detectScrollBarModule;

    public JsInteropService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<IJSObjectReference> GetScrollModuleAsync()
    {
        return _scrollModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/scroll.js");
    }

    public async Task<IJSObjectReference> GetDetectScrollBarModuleAsync()
    {
        return _detectScrollBarModule ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/detectScrollBar.js");
    }

    public async ValueTask DisposeAsync()
    {
        if (_scrollModule != null)
        {
            await _scrollModule.DisposeAsync();
        }
    }
}

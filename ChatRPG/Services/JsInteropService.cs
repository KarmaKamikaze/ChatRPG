using Microsoft.JSInterop;

namespace ChatRPG.Services;

public class JsInteropService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private IJSObjectReference? _scrollModule;
    private IJSObjectReference? _detectScrollBarModule;
    private IJSObjectReference? _autoResizeModule;
    private IJSObjectReference? _textAreaHandlerModule;

    public async Task<IJSObjectReference> GetScrollModuleAsync()
    {
        return _scrollModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/scroll.js");
    }

    public async Task<IJSObjectReference> GetDetectScrollBarModuleAsync()
    {
        return _detectScrollBarModule ??=
            await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/detectScrollBar.js");
    }

    public async Task<IJSObjectReference> GetAutoResizeModuleAsync()
    {
        return _autoResizeModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/autoResize.js");
    }

    public async Task<IJSObjectReference> GetTextAreaHandlerModuleAsync()
    {
        return _textAreaHandlerModule ??=
            await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/textareaHandler.js");
    }

    public async ValueTask DisposeAsync()
    {
        if (_scrollModule is not null)
        {
            await _scrollModule.DisposeAsync();
        }

        if (_detectScrollBarModule is not null)
        {
            await _detectScrollBarModule.DisposeAsync();
        }

        if (_autoResizeModule is not null)
        {
            await _autoResizeModule.DisposeAsync();
        }

        if (_textAreaHandlerModule is not null)
        {
            await _textAreaHandlerModule.DisposeAsync();
        }
    }
}

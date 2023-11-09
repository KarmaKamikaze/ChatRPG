namespace ChatRPG.Pages;

public partial class Index
{
    private readonly string _fullTitleText = "ChatRPG";
    private string _titleDisplayText = "";
    private bool _cursorVisible = true;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        _cts = new CancellationTokenSource();
        // Start cursor blinking on a separate thread
        await Task.Factory.StartNew(() => BlinkCursorAsync(_cts.Token), _cts.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await TypingAnimateAsync(_cts!.Token);
        }
    }

    private async Task TypingAnimateAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i <= _fullTitleText.Length; i++)
        {
            _titleDisplayText = _fullTitleText.Substring(0, i);
            await Task.Delay(100, cancellationToken); // Adjust the typing delay as needed
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task BlinkCursorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _cursorVisible = !_cursorVisible; // Toggle cursor visibility
            await Task.Delay(500, cancellationToken); // Adjust the delay between blinks
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _cts?.Cancel(); // Request cancellation when the component is about to be removed
        _cts?.Dispose();
    }
}

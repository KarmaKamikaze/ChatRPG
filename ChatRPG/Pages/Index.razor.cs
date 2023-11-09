namespace ChatRPG.Pages;

public partial class Index
{
    private readonly string _fullTitleText = "ChatRPG";
    private string _titleDisplayText = "";
    private bool _cursorVisible = true;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Initializes the component and starts the cursor blinking.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization process.</returns>
    protected override async Task OnInitializedAsync()
    {
        _cts = new CancellationTokenSource();
        // Start cursor blinking on a separate thread
        await Task.Factory.StartNew(() => BlinkCursorAsync(_cts.Token), _cts.Token,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <summary>
    /// Executes after the component has rendered and starts the typing animation.
    /// </summary>
    /// <param name="firstRender">A boolean indicating if it is the first rendering the component.</param>
    /// <returns>A task representing the asynchronous rendering process.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await TypingAnimateAsync(_cts!.Token);
        }
    }

    /// <summary>
    /// Animates the typing effect by gradually revealing characters of the full title text.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cease the animation's progress at a specific time.</param>
    /// <returns>A task representing the asynchronous animation process.</returns>
    private async Task TypingAnimateAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i <= _fullTitleText.Length; i++)
        {
            _titleDisplayText = _fullTitleText.Substring(0, i);
            await Task.Delay(100, cancellationToken); // Adjust the typing delay as needed
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Animates the cursor blinking effect.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cease the cursor animation's progress at a specific time.</param>
    /// <returns>A task representing the asynchronous cursor animation process.</returns>
    private async Task BlinkCursorAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _cursorVisible = !_cursorVisible; // Toggle cursor visibility
            await Task.Delay(500, cancellationToken); // Adjust the delay between blinks
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Disposes of the component and cancels any ongoing animation tasks.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel(); // Request cancellation when the component is about to be removed
        _cts?.Dispose();
    }
}

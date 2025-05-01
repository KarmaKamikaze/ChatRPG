using Microsoft.AspNetCore.Components;
using Radzen;

namespace ChatRPG.Pages;

public partial class ProgressModal : ComponentBase
{
    public ProgressModal()
    {
    }

    public ProgressModal(string title, string message, int progressValue)
    {
        Title = title;
        Message = message;
        ProgressValue = progressValue;
    }

    [Parameter]
    public string Title { get; set; } = "Processing...";

    [Parameter]
    public string Message { get; set; } = "Please wait while we process the scenario document.";

    [Parameter]
    public int ProgressValue { get; set; }

    public async Task OpenDialog()
    {
        await DialogService.OpenAsync("Processing...", ds => DialogFragment(),
            new DialogOptions
            {
                Width = "400px",
                CloseDialogOnOverlayClick = false,
                CloseDialogOnEsc = false,
                ShowClose = false
            });
    }

    public void UpdateProgress(int newProgress)
    {
        ProgressValue = newProgress;
        DialogService.Refresh();
    }
}

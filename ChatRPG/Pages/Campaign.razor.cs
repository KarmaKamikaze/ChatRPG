using ChatRPG.Data;
using ChatRPG.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OpenAiGptMessage = ChatRPG.API.OpenAiGptMessage;

namespace ChatRPG.Pages;

public partial class Campaign
{
    private string? _loggedInUsername;
    private bool _shouldSave;
    private IJSObjectReference? _scrollJsScript;
    private FileUtility? _fileUtil;
    readonly List<OpenAiGptMessage> _conversation = new();
    private string _userInput = "";
    private string _tempMessage = "";
    private bool _shouldStream;
    private bool _isWaitingForResponse;

    [Inject] private IConfiguration? Configuration { get; set; }
    [Inject] private IOpenAiLlmClient? OpenAiLlmClient { get; set; }
    [Inject] private IJSRuntime? JsRuntime { get; set; }
    [Inject] private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
        _loggedInUsername = authenticationState.User.Identity?.Name;
        if (_loggedInUsername != null) _fileUtil = new FileUtility(_loggedInUsername);
        _shouldSave = Configuration!.GetValue<bool>("SaveConversationsToFile");
        _shouldStream = !Configuration!.GetValue<bool>("UseMocks") &&
                        Configuration!.GetValue<bool>("StreamChatCompletions");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _scrollJsScript ??= await JsRuntime!.InvokeAsync<IJSObjectReference>("import", "./js/scroll.js");
        }
    }

    async Task EnterKeyHandler(KeyboardEventArgs e)
    {
        if (e.Code is "Enter" or "NumpadEnter")
        {
            await SendPrompt();
        }
    }

    private async Task SendPrompt()
    {
        if (string.IsNullOrWhiteSpace(_userInput))
        {
            return;
        }

        _isWaitingForResponse = true;

        OpenAiGptMessage userInput = new OpenAiGptMessage("user", _userInput);
        _conversation.Add(userInput);

        if (_shouldStream)
        {
            await HandleStreamedResponse(OpenAiLlmClient!.GetStreamedChatCompletion(userInput));
        }
        else
        {
            string response = await OpenAiLlmClient!.GetChatCompletion(userInput);
            HandleResponse(response);
        }

        if (_shouldSave && _fileUtil != null)
        {
            string assistantOutput = _conversation.Last().Content;
            await _fileUtil.UpdateSaveFileAsync(new MessagePair(_userInput, assistantOutput));
        }

        _userInput = "";
        StateHasChanged();
        await ScrollToElement("bottom-id");
        _isWaitingForResponse = false;
    }

    private void HandleResponse(string response)
    {
        OpenAiGptMessage assistantOutput = new OpenAiGptMessage("assistant", response);
        _conversation.Add(assistantOutput);
    }

    private async Task HandleStreamedResponse(IAsyncEnumerable<string> streamedResponse)
    {
        await foreach (string res in streamedResponse)
        {
            _tempMessage += res;
            StateHasChanged();
            await ScrollToElement("bottom-id");
        }

        HandleResponse(_tempMessage);
        _tempMessage = "";
    }

    private async Task ScrollToElement(string elementId)
    {
        await _scrollJsScript!.InvokeVoidAsync("ScrollToId", elementId);
    }
}

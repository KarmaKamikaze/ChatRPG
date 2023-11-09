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

    /// <summary>
    /// Initializes the Campaign page component by setting up configuration parameters.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization process.</returns>
    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
        _loggedInUsername = authenticationState.User.Identity?.Name;
        if (_loggedInUsername != null) _fileUtil = new FileUtility(_loggedInUsername);
        _shouldSave = Configuration!.GetValue<bool>("SaveConversationsToFile");
        _shouldStream = !Configuration!.GetValue<bool>("UseMocks") &&
                        Configuration!.GetValue<bool>("StreamChatCompletions");
    }

    /// <summary>
    /// Executes after the component has rendered and initializes JavaScript interop for scrolling.
    /// </summary>
    /// <param name="firstRender">A boolean indicating if it is the first rendering of the component.</param>
    /// <returns>A task representing the asynchronous rendering process.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _scrollJsScript ??= await JsRuntime!.InvokeAsync<IJSObjectReference>("import", "./js/scroll.js");
        }
    }

    /// <summary>
    /// Handles the Enter key press event and sends the user input as a prompt to the LLM API.
    /// </summary>
    /// <param name="e">A KeyboardEventArgs representing the keyboard event.</param>
    async Task EnterKeyHandler(KeyboardEventArgs e)
    {
        if (e.Code is "Enter" or "NumpadEnter")
        {
            await SendPrompt();
        }
    }

    /// <summary>
    /// Sends the user input as a prompt to the AI model, handles the response, and updates the conversation UI.
    /// </summary>
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

    /// <summary>
    /// Handles the AI model's response and updates the conversation UI with the assistant's message.
    /// </summary>
    /// <param name="response">The response received from the AI model.</param>
    private void HandleResponse(string response)
    {
        OpenAiGptMessage assistantOutput = new OpenAiGptMessage("assistant", response);
        _conversation.Add(assistantOutput);
    }

    /// <summary>
    /// Handles a streamed response from the AI model and updates the conversation UI with the assistant's message.
    /// </summary>
    /// <param name="streamedResponse">An asynchronous stream of responses from the AI model.</param>
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

    /// <summary>
    /// Scrolls to the specified document element using JavaScript interop.
    /// </summary>
    /// <param name="elementId">The ID of the element to scroll to.</param>
    private async Task ScrollToElement(string elementId)
    {
        await _scrollJsScript!.InvokeVoidAsync("ScrollToId", elementId);
    }
}

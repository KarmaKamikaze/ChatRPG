using ChatRPG.Data;
using ChatRPG.Services;
using ChatRPG.Data.Models;
using ChatRPG.Services.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OpenAI_API.Chat;
using OpenAiGptMessage = ChatRPG.API.OpenAiGptMessage;

namespace ChatRPG.Pages;

public partial class CampaignPage
{
    private string? _loggedInUsername;
    private bool _shouldSave;
    private IJSObjectReference? _scrollJsScript;
    private IJSObjectReference? _detectScrollBarJsScript;
    private bool _hasScrollBar = false;
    private FileUtility? _fileUtil;
    private List<OpenAiGptMessage> _conversation = new();
    private string _userInput = "";
    private bool _isWaitingForResponse;
    private OpenAiGptMessage? _latestPlayerMessage;
    private const string BottomId = "bottom-id";
    private Campaign? _campaign;
    private PromptType _activePromptType = PromptType.Do;

    [Inject] private IConfiguration? Configuration { get; set; }
    [Inject] private IJSRuntime? JsRuntime { get; set; }
    [Inject] private AuthenticationStateProvider? AuthenticationStateProvider { get; set; }
    [Inject] private IPersisterService? PersisterService { get; set; }
    [Inject] private ICampaignMediatorService? CampaignMediatorService { get; set; }
    [Inject] private GameInputHandler? GameInputHandler { get; set; }
    [Inject] private NavigationManager? NavMan { get; set; }

    /// <summary>
    /// Initializes the Campaign page component by setting up configuration parameters.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization process.</returns>
    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authenticationState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();
        _loggedInUsername = authenticationState.User.Identity?.Name;
        if (_loggedInUsername is null || !CampaignMediatorService!.UserCampaignDict.ContainsKey(_loggedInUsername!))
        {
            NavMan!.NavigateTo("/", forceLoad: true);
        }

        _campaign = await PersisterService!.LoadFromCampaignIdAsync(
            CampaignMediatorService!.UserCampaignDict[_loggedInUsername!]);
        if (_campaign != null)
        {
            _conversation = _campaign.Messages.Select(OpenAiGptMessage.FromMessage).ToList();
        }

        if (_loggedInUsername != null) _fileUtil = new FileUtility(_loggedInUsername);
        _shouldSave = Configuration!.GetValue<bool>("SaveConversationsToFile");
        GameInputHandler!.ChatCompletionReceived += OnChatCompletionReceived;
        GameInputHandler!.ChatCompletionChunkReceived += OnChatCompletionChunkReceived;
        if (_conversation.Count == 0)
        {
            InitializeCampaign();
        }
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
            _detectScrollBarJsScript ??= await JsRuntime!.InvokeAsync<IJSObjectReference>("import", "./js/detectScrollBar.js");
            await ScrollToElement(BottomId); // scroll down to latest message
        }
    }

    private void InitializeCampaign()
    {
        string content = $"The player is {_campaign!.Player.Name}, described as \"{_campaign.Player.Description}\".";
        if (_campaign.StartScenario != null)
        {
            content += "\n" + _campaign.StartScenario;
        }
        OpenAiGptMessage message = new(ChatMessageRole.System, content);
        _conversation.Add(message);
        GameInputHandler?.HandleUserPrompt(_campaign, _conversation);
    }

    /// <summary>
    /// Handles the Enter key press event and sends the user input as a prompt to the LLM API.
    /// </summary>
    /// <param name="e">A KeyboardEventArgs representing the keyboard event.</param>
    private async Task EnterKeyHandler(KeyboardEventArgs e)
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
        if (string.IsNullOrWhiteSpace(_userInput) || _campaign is null)
        {
            return;
        }

        _isWaitingForResponse = true;
        OpenAiGptMessage userInput = new(ChatMessageRole.User, _userInput);
        _conversation.Add(userInput);
        _latestPlayerMessage = userInput;
        _userInput = string.Empty;
        await GameInputHandler!.HandleUserPrompt(_campaign, _conversation);
    }

    /// <summary>
    /// Scrolls to the specified document element using JavaScript interop.
    /// </summary>
    /// <param name="elementId">The ID of the element to scroll to.</param>
    private async Task ScrollToElement(string elementId)
    {
        await _scrollJsScript!.InvokeVoidAsync("ScrollToId", elementId);
        _hasScrollBar = await _detectScrollBarJsScript!.InvokeAsync<bool>("DetectScrollBar");
        StateHasChanged();
    }

    /// <summary>
    /// Handles the AI model's response and updates the conversation UI with the assistant's message.
    /// </summary>
    /// <param name="sender">Sender of the event.</param>
    /// <param name="eventArgs">The arguments for this event, including the response text.</param>
    private void OnChatCompletionReceived(object? sender, ChatCompletionReceivedEventArgs eventArgs)
    {
        _conversation.Add(eventArgs.Message);
        UpdateSaveFile(eventArgs.Message.Content);
        Task.Run(() => ScrollToElement(BottomId));
        _isWaitingForResponse = false;
    }

    /// <summary>
    /// Handles a streamed response from the AI model and updates the conversation UI with the assistant's message.
    /// </summary>
    /// <param name="sender">Sender of the event.</param>
    /// <param name="eventArgs">The arguments for this event, including the text chunk and whether the streaming is done.</param>
    private void OnChatCompletionChunkReceived(object? sender, ChatCompletionChunkReceivedEventArgs eventArgs)
    {
        OpenAiGptMessage message = _conversation.LastOrDefault(new OpenAiGptMessage(ChatMessageRole.Assistant, ""));
        if (eventArgs.IsStreamingDone)
        {
            _isWaitingForResponse = false;
            UpdateSaveFile(message.Content);
        }
        else if (eventArgs.Chunk is not null)
        {
            message.AddChunk(eventArgs.Chunk);
            StateHasChanged();
        }

        Task.Run(() => ScrollToElement(BottomId));
    }

    private void UpdateSaveFile(string asstMessage)
    {
        if (!_shouldSave || _fileUtil == null || string.IsNullOrEmpty(asstMessage)) return;
        MessagePair messagePair = new MessagePair(_latestPlayerMessage?.Content ?? "", asstMessage);
        Task.Run(() => _fileUtil.UpdateSaveFileAsync(messagePair));
    }
}

public enum PromptType
{
    Do,
    Say,
    Attack
}

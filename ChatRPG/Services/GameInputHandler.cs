using ChatRPG.API;
using ChatRPG.Data.Models;
using ChatRPG.Pages;
using ChatRPG.Services.Events;

namespace ChatRPG.Services;

public class GameInputHandler
{
    private readonly ILogger<GameInputHandler> _logger;
    private readonly IReActLlmClient _llmClient;
    private readonly ReActExaminerAgent _reActExaminerAgent;
    private readonly ReActNavigatorAgent _reActNavigatorAgent;
    private readonly ReActArchivistAgent _reActArchivistAgent;
    private readonly bool _streamChatCompletions;
    private readonly Dictionary<SystemPromptType, string> _systemPrompts = new();
    private readonly Dictionary<SystemPromptType, string> _systemPromptsWithVerdict = new();
    private readonly AutoResetEvent _autoResetEvent = new(true);

    public GameInputHandler(
        ILogger<GameInputHandler> logger,
        IReActLlmClient llmClient,
        ReActExaminerAgent reActExaminerAgent,
        ReActNavigatorAgent reActNavigatorAgent,
        ReActArchivistAgent reActArchivistAgent,
        IConfiguration configuration)
    {
        _logger = logger;
        _llmClient = llmClient;
        _reActExaminerAgent = reActExaminerAgent;
        _reActNavigatorAgent = reActNavigatorAgent;
        _reActArchivistAgent = reActArchivistAgent;
        _streamChatCompletions = configuration.GetValue("StreamChatCompletions", true);
        if (configuration.GetValue("UseMocks", false))
        {
            _streamChatCompletions = false;
        }

        IConfigurationSection sysPromptSec = configuration.GetRequiredSection("SystemPrompts");
        _systemPrompts.Add(SystemPromptType.Initial, sysPromptSec.GetValue("Initial", ""));
        _systemPrompts.Add(SystemPromptType.DoAction, sysPromptSec.GetValue("DoAction", ""));
        _systemPrompts.Add(SystemPromptType.SayAction, sysPromptSec.GetValue("SayAction", ""));
        _systemPromptsWithVerdict.Add(SystemPromptType.DoAction, sysPromptSec.GetValue("DoActionWithVerdict", ""));
        _systemPromptsWithVerdict.Add(SystemPromptType.SayAction, sysPromptSec.GetValue("SayActionWithVerdict", ""));
    }

    public event EventHandler<ChatCompletionReceivedEventArgs>? ChatCompletionReceived;
    public event EventHandler<ChatCompletionChunkReceivedEventArgs>? ChatCompletionChunkReceived;
    public event Action? CampaignUpdated;

    private void OnChatCompletionReceived(OpenAiGptMessage message)
    {
        ChatCompletionReceived?.Invoke(this, new ChatCompletionReceivedEventArgs(message));
    }

    private void OnChatCompletionChunkReceived(bool isStreamingDone, string? chunk = null)
    {
        ChatCompletionChunkReceivedEventArgs args = (chunk is null)
            ? new ChatCompletionChunkReceivedEventArgs(isStreamingDone)
            : new ChatCompletionChunkReceivedEventArgs(isStreamingDone, chunk);
        ChatCompletionChunkReceived?.Invoke(this, args);
    }

    private void OnCampaignUpdated()
    {
        CampaignUpdated?.Invoke();
    }

    public async Task HandleUserPrompt(Campaign campaign, UserPromptType promptType, string userInput)
    {
        // Wait for the previous archivist task to finish before processing next prompt
        _autoResetEvent.WaitOne();

        // Check if the campaign is in a state that allows performing adherence checks
        string? userInputAdherenceVerdict = null;
        string? graphUpdateSummary = null;
        var relevantSystemPrompts = _systemPrompts;
        if (!campaign.IsOpenWorld)
        {
            relevantSystemPrompts = _systemPromptsWithVerdict;
            userInputAdherenceVerdict = await _reActExaminerAgent.ExaminePlayerInput(campaign, userInput);
            // Check if the verdict is disallowed and if so, skip the graph update since no changes are needed
            if (!userInputAdherenceVerdict.Contains("DISALLOWED", StringComparison.Ordinal))
            {
                graphUpdateSummary =
                    await _reActNavigatorAgent.ReviewGraph(campaign, userInput, userInputAdherenceVerdict);
            }
        }

        switch (promptType)
        {
            case UserPromptType.Do:
                await GetResponseAndUpdateState(campaign, relevantSystemPrompts[SystemPromptType.DoAction],
                    userInput, userInputAdherenceVerdict, graphUpdateSummary);
                break;
            case UserPromptType.Say:
                await GetResponseAndUpdateState(campaign, relevantSystemPrompts[SystemPromptType.SayAction],
                    userInput, userInputAdherenceVerdict, graphUpdateSummary);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _logger.LogInformation("Finished processing prompt");
    }

    public async Task HandleInitialPrompt(Campaign campaign, string initialInput)
    {
        string? graphUpdateSummary = null;
        if (!campaign.IsOpenWorld)
        {
            graphUpdateSummary = await _reActNavigatorAgent.ReviewGraph(
                campaign,
                initialInput,
                "Verdict: ALLOWED\nThe scenario is created directly from the scenario document.");
        }

        await GetResponseAndUpdateState(campaign, _systemPrompts[SystemPromptType.Initial], initialInput,
            graphUpdateSummary: graphUpdateSummary);
        _logger.LogInformation("Finished processing prompt");
    }

    private async Task GetResponseAndUpdateState(Campaign campaign, string actionPrompt, string playerInput,
        string? verdict = null, string? graphUpdateSummary = null)
    {
        var input = playerInput;
        if (!campaign.IsOpenWorld)
        {
            input = $"""
                     Player input: 
                     {playerInput}
                     {(verdict is null ? "" :
                         $"""
                          Adherence verdict: 
                          {verdict}
                          """)}
                     {(graphUpdateSummary is null ? "" :
                         $"""
                          Graph update summary:
                          {graphUpdateSummary}
                          """)}
                     """;
        }

        if (_streamChatCompletions)
        {
            OpenAiGptMessage message = new(MessageRole.Assistant, "");
            OnChatCompletionReceived(message);

            await foreach (var chunk in
                           _llmClient.GetStreamedChatCompletionAsync(campaign, actionPrompt, input))
            {
                OnChatCompletionChunkReceived(isStreamingDone: false, chunk);
            }

            OnChatCompletionChunkReceived(isStreamingDone: true);

            _ = Task.Run(async () =>
            {
                await SaveInteraction(campaign, playerInput, message.Content, verdict);
                _autoResetEvent.Set();
            });
        }
        else
        {
            var response = await _llmClient.GetChatCompletionAsync(campaign, actionPrompt, input);
            OpenAiGptMessage message = new(MessageRole.Assistant, response);
            OnChatCompletionReceived(message);

            _ = Task.Run(async () =>
            {
                await SaveInteraction(campaign, playerInput, message.Content, verdict);
                _autoResetEvent.Set();
            });
        }
    }

    private async Task SaveInteraction(Campaign campaign, string input, string response, string? verdict = null)
    {
        await _reActArchivistAgent.UpdateCampaignFromNarrative(campaign, input, response);
        await _reActArchivistAgent.StoreMessagesInCampaign(campaign, input, response, verdict);
        await _reActArchivistAgent.SaveCurrentState(campaign);
        OnCampaignUpdated();
    }
}

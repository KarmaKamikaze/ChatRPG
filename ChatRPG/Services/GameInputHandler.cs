using ChatRPG.API;
using ChatRPG.Data.Models;
using ChatRPG.Pages;
using ChatRPG.Services.Events;

namespace ChatRPG.Services;

public class GameInputHandler
{
    private readonly ILogger<GameInputHandler> _logger;
    private readonly IReActLlmClient _llmClient;
    private readonly GameStateManager _gameStateManager;
    private readonly bool _streamChatCompletions;
    private readonly Dictionary<SystemPromptType, string> _systemPrompts = new();
    private const int PlayerDmgMin = 10;
    private const int PlayerDmgMax = 25;
    private const int PlayerHealMin = 15;
    private const int PlayerHealMax = 30;

    private static readonly Dictionary<CharacterType, (int, int)> CharacterTypeDamageDict = new()
    {
        { CharacterType.Humanoid, (10, 20) },
        { CharacterType.SmallCreature, (5, 10) },
        { CharacterType.LargeCreature, (15, 25) },
        { CharacterType.Monster, (20, 30) }
    };

    public GameInputHandler(ILogger<GameInputHandler> logger, IReActLlmClient llmClient,
        GameStateManager gameStateManager, IConfiguration configuration)
    {
        _logger = logger;
        _llmClient = llmClient;
        _gameStateManager = gameStateManager;
        _streamChatCompletions = configuration.GetValue("StreamChatCompletions", true);
        if (configuration.GetValue("UseMocks", false))
        {
            _streamChatCompletions = false;
        }

        IConfigurationSection sysPromptSec = configuration.GetRequiredSection("SystemPrompts");
        _systemPrompts.Add(SystemPromptType.Initial, sysPromptSec.GetValue("Initial", "")!);
        _systemPrompts.Add(SystemPromptType.CombatHitHit, sysPromptSec.GetValue("CombatHitHit", "")!);
        _systemPrompts.Add(SystemPromptType.CombatHitMiss, sysPromptSec.GetValue("CombatHitMiss", "")!);
        _systemPrompts.Add(SystemPromptType.CombatMissHit, sysPromptSec.GetValue("CombatMissHit", "")!);
        _systemPrompts.Add(SystemPromptType.CombatMissMiss, sysPromptSec.GetValue("CombatMissMiss", "")!);
        _systemPrompts.Add(SystemPromptType.CombatOpponentDescription,
            sysPromptSec.GetValue("CombatOpponentDescription", "")!);
        _systemPrompts.Add(SystemPromptType.HurtOrHeal, sysPromptSec.GetValue("DoActionHurtOrHeal", "")!);
        _systemPrompts.Add(SystemPromptType.DoAction, sysPromptSec.GetValue("DoAction", "")!);
        _systemPrompts.Add(SystemPromptType.SayAction, sysPromptSec.GetValue("SayAction", "")!);
    }

    public event EventHandler<ChatCompletionReceivedEventArgs>? ChatCompletionReceived;
    public event EventHandler<ChatCompletionChunkReceivedEventArgs>? ChatCompletionChunkReceived;

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

    public async Task HandleUserPrompt(Campaign campaign, UserPromptType promptType, string userInput)
    {
        switch (promptType)
        {
            case UserPromptType.Do:
                await GetResponseAndUpdateState(campaign, _systemPrompts[SystemPromptType.DoAction], userInput);
                break;
            case UserPromptType.Say:
                await GetResponseAndUpdateState(campaign, _systemPrompts[SystemPromptType.SayAction], userInput);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _logger.LogInformation("Finished processing prompt");
    }

    public async Task HandleInitialPrompt(Campaign campaign, string initialInput)
    {
        await GetResponseAndUpdateState(campaign, _systemPrompts[SystemPromptType.Initial], initialInput);
        _logger.LogInformation("Finished processing prompt");
    }

    private async Task GetResponseAndUpdateState(Campaign campaign, string actionPrompt, string input)
    {
        if (_streamChatCompletions)
        {
            OpenAiGptMessage message = new(MessageRole.Assistant, "");
            OnChatCompletionReceived(message);

            await foreach (var chunk in _llmClient.GetStreamedChatCompletionAsync(campaign, actionPrompt, input))
            {
                OnChatCompletionChunkReceived(isStreamingDone: false, chunk);
            }

            await SaveInteraction(campaign, input, message.Content);
            OnChatCompletionChunkReceived(isStreamingDone: true);
        }
        else
        {
            string response = await _llmClient.GetChatCompletionAsync(campaign, actionPrompt, input);
            OpenAiGptMessage message = new(MessageRole.Assistant, response);
            OnChatCompletionReceived(message);

            await SaveInteraction(campaign, input, response);
        }
    }

    private async Task SaveInteraction(Campaign campaign, string input, string response)
    {
        await _gameStateManager.StoreMessagesInCampaign(campaign, input, response);
        await _gameStateManager.UpdateCampaignFromNarrative(campaign, input, response);

        await _gameStateManager.SaveCurrentState(campaign);
    }
}

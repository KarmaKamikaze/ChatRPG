using ChatRPG.API;
using ChatRPG.Data.Models;
using ChatRPG.Services.Events;
using OpenAI_API.Chat;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;

namespace ChatRPG.Services;

public partial class GameInputHandler
{
    private readonly ILogger<GameInputHandler> _logger;
    private readonly IOpenAiLlmClient _llmClient;
    private readonly GameStateManager _gameStateManager;
    private readonly bool _streamChatCompletions;
    private readonly Dictionary<SystemPromptType, string> systemPrompts = new Dictionary<SystemPromptType, string>();

    public GameInputHandler(ILogger<GameInputHandler> logger, IOpenAiLlmClient llmClient, GameStateManager gameStateManager, IConfiguration configuration)
    {
        _logger = logger;
        _llmClient = llmClient;
        _gameStateManager = gameStateManager;
        _streamChatCompletions = configuration.GetValue("StreamChatCompletions", true);
        if (configuration.GetValue("UseMocks", false))
        {
            _streamChatCompletions = false;
        }
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

    public async Task HandleUserPrompt(Campaign campaign, IList<OpenAiGptMessage> conversation)
    {
        string systemPrompt = GetRelevantSystemPrompt(conversation);
        await GetResponseAndUpdateState(campaign, conversation, systemPrompt);
        _logger.LogInformation("Finished processing prompt.");
    }

    private string GetRelevantSystemPrompt(ICollection<OpenAiGptMessage> conversation)
    {
        SystemPromptType type = SystemPromptType.Default;
        if (_gameStateManager.CombatMode)
        {
            type = DetermineCombatOutcome();
            (int playerDmg, int opponentDmg) = ComputeCombatDamage(type);
            string messageContent = "";
            if (playerDmg != 0)
            {
                messageContent += $"The player hits with their attack, dealing {playerDmg} damage.";
            }
            else
            {
                messageContent += "The player misses with their attack, dealing no damage.";
            }

            if (opponentDmg != 0)
            {
                messageContent += $"The opponent will hit with their next attack, dealing {opponentDmg} damage.";
            }
            else
            {
                messageContent += "The opponent will miss their next attack, dealing no damage.";
            }

            OpenAiGptMessage message = new OpenAiGptMessage(ChatMessageRole.System, messageContent);
            conversation.Add(message);
        }
        return systemPrompts[type];
    }

    private static SystemPromptType DetermineCombatOutcome()
    {
        Random rand = new Random();
        double playerRoll = rand.NextDouble();
        double opponentRoll = rand.NextDouble();

        if (playerRoll >= 0.4)
        {
            return opponentRoll >= 0.6 ? SystemPromptType.CombatHitHit : SystemPromptType.CombatHitMiss;
        }

        return opponentRoll >= 0.6 ? SystemPromptType.CombatMissHit : SystemPromptType.CombatMissMiss;
    }

    private static (int, int) ComputeCombatDamage(SystemPromptType combatOutcome)
    {
        Random rand = new Random();
        int playerDmg = 0;
        int opponentDmg = 0;

        switch (combatOutcome)
        {
            case SystemPromptType.CombatHitHit:
                playerDmg = rand.Next(5, 20);
                opponentDmg = rand.Next(3, 15);
                break;
            case SystemPromptType.CombatHitMiss:
                playerDmg = rand.Next(5, 20);
                break;
            case SystemPromptType.CombatMissHit:
                opponentDmg = rand.Next(3, 15);
                break;
            case SystemPromptType.CombatMissMiss:
                break;
        }
        return (playerDmg, opponentDmg);
    }

    private async Task GetResponseAndUpdateState(Campaign campaign, IList<OpenAiGptMessage> conversation, string systemPrompt)
    {
        _gameStateManager.UpdateStateFromMessage(campaign, conversation.Last(m => m.Role.Equals(ChatMessageRole.User)));
        if (_streamChatCompletions)
        {
            OpenAiGptMessage message = new(ChatMessageRole.Assistant, "");
            OnChatCompletionReceived(message);

            await foreach (string chunk in _llmClient.GetStreamedChatCompletion(conversation, systemPrompt))
            {
                OnChatCompletionChunkReceived(isStreamingDone: false, chunk);
            }
            OnChatCompletionChunkReceived(isStreamingDone: true);
            _gameStateManager.UpdateStateFromMessage(campaign, message);
            await _gameStateManager.SaveCurrentState(campaign);
        }
        else
        {
            string response = await _llmClient.GetChatCompletion(conversation, systemPrompt);
            OpenAiGptMessage message = new(ChatMessageRole.Assistant, response);
            OnChatCompletionReceived(message);
            _gameStateManager.UpdateStateFromMessage(campaign, message);
            await _gameStateManager.SaveCurrentState(campaign);
        }
    }
}

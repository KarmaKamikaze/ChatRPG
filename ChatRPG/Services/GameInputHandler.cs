using ChatRPG.API;
using ChatRPG.API.Response;
using ChatRPG.Data.Models;
using ChatRPG.Pages;
using ChatRPG.Services.Events;
using OpenAI_API.Chat;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Services;

public class GameInputHandler
{
    private readonly ILogger<GameInputHandler> _logger;
    private readonly IOpenAiLlmClient _llmClient;
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
        IConfigurationSection sysPromptSec = configuration.GetRequiredSection("SystemPrompts");
        _systemPrompts.Add(SystemPromptType.Default, sysPromptSec.GetValue("Default", "")!);
        _systemPrompts.Add(SystemPromptType.CombatHitHit, sysPromptSec.GetValue("CombatHitHit", "")!);
        _systemPrompts.Add(SystemPromptType.CombatHitMiss, sysPromptSec.GetValue("CombatHitMiss", "")!);
        _systemPrompts.Add(SystemPromptType.CombatMissHit, sysPromptSec.GetValue("CombatMissHit", "")!);
        _systemPrompts.Add(SystemPromptType.CombatMissMiss, sysPromptSec.GetValue("CombatMissMiss", "")!);
        _systemPrompts.Add(SystemPromptType.CombatOpponentDescription, sysPromptSec.GetValue("CombatOpponentDescription", "")!);
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

    public async Task HandleUserPrompt(Campaign campaign, IList<OpenAiGptMessage> conversation)
    {
        string systemPrompt = await GetRelevantSystemPrompt(campaign, conversation);
        await GetResponseAndUpdateState(campaign, conversation, systemPrompt);
        _logger.LogInformation("Finished processing prompt.");
    }

    private async Task<string> GetRelevantSystemPrompt(Campaign campaign, IList<OpenAiGptMessage> conversation)
    {
        if (!conversation.Any(m => m.Role.Equals(ChatMessageRole.User)))
        {
            return _systemPrompts[SystemPromptType.Default];
        }

        UserPromptType userPromptType = conversation.Last(m => m.Role.Equals(ChatMessageRole.User)).UserPromptType;

        SystemPromptType systemPromptType = SystemPromptType.Default;

        switch (userPromptType)
        {
            case UserPromptType.Say:
                systemPromptType = SystemPromptType.SayAction;
                break;
            case UserPromptType.Do:
                OpenAiGptMessage lastUserMessage = conversation.Last(m => m.Role.Equals(ChatMessageRole.User));
                string hurtOrHealString = await _llmClient.GetChatCompletion(new List<OpenAiGptMessage>(){lastUserMessage}, _systemPrompts[SystemPromptType.HurtOrHeal]);
                _logger.LogInformation("Hurt or heal response: {hurtOrHealString}", hurtOrHealString);
                OpenAiGptMessage hurtOrHealMessage = new(ChatMessageRole.Assistant, hurtOrHealString);
                LlmResponse? hurtOrHealResponse = hurtOrHealMessage.TryParseFromJson();
                string hurtOrHealMessageContent = "";
                Random rand = new Random();
                if (hurtOrHealResponse?.Heal == true)
                {
                    int healAmount = rand.Next(PlayerHealMin, PlayerHealMax);
                    campaign.Player.AdjustHealth(healAmount);
                    hurtOrHealMessageContent +=
                        $"The player heals {healAmount} health points, setting them at {campaign.Player.CurrentHealth} health points. Mention these numbers in your response.";
                }

                if (hurtOrHealResponse?.Hurt == true)
                {
                    int dmgAmount = rand.Next(PlayerDmgMin, PlayerDmgMax);
                    hurtOrHealMessageContent += $"The player hurts themselves for {dmgAmount} damage. The player has {(campaign.Player.CurrentHealth - dmgAmount < 0 ? 0 : campaign.Player.CurrentHealth - dmgAmount)} health remaining. Mention these numbers in your response.";
                    if (campaign.Player.AdjustHealth(-dmgAmount))
                    {
                        hurtOrHealMessageContent += "The player has died and their adventure ends.";
                    }
                }
                OpenAiGptMessage hurtOrHealSystemMessage = new(ChatMessageRole.System, hurtOrHealMessageContent);
                conversation.Add(hurtOrHealSystemMessage);
                systemPromptType = SystemPromptType.DoAction;
                break;
            case UserPromptType.Attack:
                string opponentDescriptionString = await _llmClient.GetChatCompletion(conversation, _systemPrompts[SystemPromptType.CombatOpponentDescription]);
                _logger.LogInformation("Opponent description response: {opponentDescriptionString}", opponentDescriptionString);
                OpenAiGptMessage opponentDescriptionMessage = new(ChatMessageRole.Assistant, opponentDescriptionString);
                LlmResponse? opponentDescriptionResponse = opponentDescriptionMessage.TryParseFromJson();
                LlmResponseCharacter? resChar = opponentDescriptionResponse?.Characters?.FirstOrDefault();
                if (resChar != null)
                {
                    Environment environment = campaign.Environments.Last();
                    Character character = new(campaign, environment, GameStateManager.ParseToEnum(resChar.Type!, CharacterType.Humanoid),
                        resChar.Name!, resChar.Description!, false);
                    campaign.InsertOrUpdateCharacter(character);
                }
                string? opponentName = opponentDescriptionResponse?.Opponent?.ToLower();
                Character? opponent = campaign.Characters.LastOrDefault(c => !c.IsPlayer && c.Name.ToLower().Equals(opponentName));
                if (opponent == null)
                {
                    _logger.LogError("Opponent is unknown!");
                    return _systemPrompts[SystemPromptType.Default];
                }
                systemPromptType = DetermineCombatOutcome();
                (int playerDmg, int opponentDmg) = ComputeCombatDamage(systemPromptType, opponent.Type);
                string combatMessageContent = "";
                if (playerDmg != 0)
                {
                    if (opponent.AdjustHealth(-playerDmg))
                    {
                        combatMessageContent +=
                            $" With no health points remaining, {opponent.Name} dies and can no longer participate in the narrative.";
                    }
                    combatMessageContent += $"The player hits with their attack, dealing {playerDmg} damage. The opponent has {opponent.CurrentHealth} health remaining.";
                    _logger.LogInformation("Combat: {Name} hits {Name} for {x} damage. Health: {CurrentHealth}/{MaxHealth}", campaign.Player.Name, opponent.Name, playerDmg, opponent.CurrentHealth, opponent.MaxHealth);
                }
                else
                {
                    combatMessageContent += $"The player misses with their attack, dealing no damage. The opponent has {opponent.CurrentHealth} health remaining.";
                }

                if (opponentDmg != 0)
                {
                    combatMessageContent += $"The opponent will hit with their next attack, dealing {opponentDmg} damage. The player has {(campaign.Player.CurrentHealth - opponentDmg < 0 ? 0 : campaign.Player.CurrentHealth - opponentDmg)} health remaining.";
                    if (campaign.Player.AdjustHealth(-opponentDmg))
                    {
                        combatMessageContent += "The player has died and their adventure ends.";
                    }
                    _logger.LogInformation("Combat: {Name} hits {Name} for {x} damage. Health: {CurrentHealth}/{MaxHealth}", opponent.Name, campaign.Player.Name, opponentDmg, campaign.Player.CurrentHealth, campaign.Player.MaxHealth);
                }
                else
                {
                    combatMessageContent += $"The opponent will miss their next attack, dealing no damage. The player has {campaign.Player.CurrentHealth} health remaining.";
                }

                OpenAiGptMessage combatSystemMessage = new(ChatMessageRole.System, combatMessageContent);
                conversation.Add(combatSystemMessage);
                break;
        }

        return _systemPrompts[systemPromptType];
    }

    private static SystemPromptType DetermineCombatOutcome()
    {
        Random rand = new Random();
        double playerRoll = rand.NextDouble();
        double opponentRoll = rand.NextDouble();

        if (playerRoll >= 0.3)
        {
            return opponentRoll >= 0.6 ? SystemPromptType.CombatHitHit : SystemPromptType.CombatHitMiss;
        }

        return opponentRoll >= 0.5 ? SystemPromptType.CombatMissHit : SystemPromptType.CombatMissMiss;
    }

    private static (int, int) ComputeCombatDamage(SystemPromptType combatOutcome, CharacterType opponentType)
    {
        Random rand = new Random();
        int playerDmg = 0;
        int opponentDmg = 0;
        (int opponentMin, int opponentMax) = CharacterTypeDamageDict[opponentType];
        switch (combatOutcome)
        {
            case SystemPromptType.CombatHitHit:
                playerDmg = rand.Next(PlayerDmgMin, PlayerDmgMax);
                opponentDmg = rand.Next(opponentMin, opponentMax);
                break;
            case SystemPromptType.CombatHitMiss:
                playerDmg = rand.Next(PlayerDmgMin, PlayerDmgMax);
                break;
            case SystemPromptType.CombatMissHit:
                opponentDmg = rand.Next(opponentMin, opponentMax);
                break;
            case SystemPromptType.CombatMissMiss:
                break;
        }
        return (playerDmg, opponentDmg);
    }

    private async Task GetResponseAndUpdateState(Campaign campaign, IList<OpenAiGptMessage> conversation, string systemPrompt)
    {
        if (conversation.Any(m => m.Role.Equals(ChatMessageRole.User)))
        {
            _gameStateManager.UpdateStateFromMessage(campaign, conversation.Last(m => m.Role.Equals(ChatMessageRole.User)));
        }
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

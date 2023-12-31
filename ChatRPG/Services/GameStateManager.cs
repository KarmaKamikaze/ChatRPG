using ChatRPG.API;
using ChatRPG.API.Response;
using ChatRPG.Data.Models;
using OpenAI_API.Chat;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Services;

public class GameStateManager
{
    private readonly ILogger<GameStateManager> _logger;
    private readonly IPersistenceService _persistenceService;

    public GameStateManager(ILogger<GameStateManager> logger, IPersistenceService persistenceService)
    {
        _logger = logger;
        _persistenceService = persistenceService;
    }

    public void UpdateStateFromMessage(Campaign campaign, OpenAiGptMessage gptMessage)
    {
        Message message = new(campaign, ParseToEnum(gptMessage.Role.ToString()!, MessageRole.User), gptMessage.Content);
        campaign.Messages.Add(message);
        if (gptMessage.Role.Equals(ChatMessageRole.Assistant))
        {
            UpdateStateFromResponse(campaign, gptMessage);
        }
    }

    public async Task SaveCurrentState(Campaign campaign)
    {
        await _persistenceService.SaveAsync(campaign);
    }

    private void UpdateStateFromResponse(Campaign campaign, OpenAiGptMessage message)
    {
        try
        {
            LlmResponse? response = message.TryParseFromJson();
            if (response is null) return;

            if (response.Environment is { Name: not null, Description: not null })
            {
                Environment environment = new(campaign, response.Environment.Name, response.Environment.Description);
                environment = campaign.InsertOrUpdateEnvironment(environment);
                campaign.Player.Environment = environment;
                _logger.LogInformation("Set environment: \"{Name}\"", environment.Name);
            }

            if (response.Characters is not null)
            {
                foreach (LlmResponseCharacter resChar in response.Characters.Where(c => c is { Name: not null, Description: not null, Type: not null }))
                {
                    Environment environment = campaign.Environments.Last();
                    Character character = new(campaign, environment, ParseToEnum(resChar.Type!, CharacterType.Humanoid),
                        resChar.Name!, resChar.Description!, false);
                    campaign.InsertOrUpdateCharacter(character);
                    _logger.LogInformation("Created character: \"{Name}\"", character.Name);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to parse message content as response: \"{Content}\"", message.Content);
        }
    }

    public static T ParseToEnum<T>(string input, T defaultVal) where T : struct, Enum
    {
        return Enum.TryParse(input, true, out T type) ? type : defaultVal;
    }
}

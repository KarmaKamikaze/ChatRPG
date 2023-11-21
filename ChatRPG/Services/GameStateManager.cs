using System.Text;
using ChatRPG.API;
using ChatRPG.API.Response;
using ChatRPG.Data.Models;
using OpenAI_API.Chat;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.Services;

public class GameStateManager
{
    private readonly ILogger<GameStateManager> _logger;
    private readonly IPersisterService _persisterService;

    public GameStateManager(ILogger<GameStateManager> logger, IPersisterService persisterService)
    {
        _logger = logger;
        _persisterService = persisterService;
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
        await _persisterService.SaveAsync(campaign);
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
                campaign.Environments.Add(environment);
                campaign.Player.Environment = environment;
                _logger.LogInformation("Set environment: \"{Name}\"", environment.Name);
            }

            if (response.Characters is not null)
            {
                foreach (LlmResponseCharacter resChar in response.Characters.Where(c => c is { Name: not null, Description: not null, Type: not null }))
                {
                    Environment environment = campaign.Environments.Last();
                    Character character = new(campaign, environment, ParseToEnum(resChar.Type!, CharacterType.Humanoid),
                        resChar.Name!, resChar.Description!, false, resChar.HealthPoints);
                    campaign.Characters.Add(character);
                    _logger.LogInformation("Created character: \"{Name}\"", character.Name);
                }
            }

            if (response.Events is not null)
            {
                foreach (LlmResponseEvent rEvent in response.Events.Where(e => e is { Name: not null, Description: not null, Type: not null }))
                {
                    Event @event = new(campaign, campaign.Environments.Last(), ParseToEnum(rEvent.Type!, EventType.Other),
                        rEvent.Name!, rEvent.Description!);
                    campaign.Events.Add(@event);
                    _logger.LogInformation("Created event: \"{Name}\"", @event.Name);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to parse message content as response: \"{Content}\"", message.Content);
        }
    }

    private static T ParseToEnum<T>(string input, T defaultVal) where T : struct, Enum
    {
        return Enum.TryParse(input, true, out T type) ? type : defaultVal;
    }
}

using ChatRPG.API;
using ChatRPG.Data.Models;
using ChatRPG.Services;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI_API.Chat;
using RichardSzalay.MockHttp;

namespace ChatRPGTests;

public class GameStateManagerTests
{
    private readonly GameStateManager _parser;
    private readonly User _user;
    private readonly Campaign _campaign;
    private readonly Character _player;

    public GameStateManagerTests()
    {
        ILogger<GameStateManager> logger = Mock.Of<ILogger<GameStateManager>>();
        _parser = new GameStateManager(logger);
        _user = new User("test");
        _campaign = new Campaign(_user, "Test");
        _player = new Character(_campaign, CharacterType.Humanoid, "Player", "The player", true, 100);
        _campaign.Characters.Add(_player);
    }

    [Fact]
    public void GivenCompleteInput_BasicExample_UpdatesStateAsExpected()
    {
        OpenAiGptMessage message = new(ChatMessageRole.Assistant, """
            {
              "narrative": "Welcome, Sarmilan, the bewitching mage from Eldoria. Your journey begins at the towering gates of the ancient city of Thundertop, known for its imposing architecture and bustling markets. Rumor has it that the city holds a secret - a powerful artifact known as the 'Eye of the Storm'.",
              "characters": [
                {
                  "name": "Sarmilan",
                  "description": "A charming and attractive mage hailing from the mystical land of Eldoria.",
                  "type": "Humanoid",
                  "HealthPoints": 50
                }
              ],
              "events": [],
              "environment": {
                "name": "Thundertop City",
                "description": "A sprawling ancient city with towering architecture, bustling markets and whispered secrets."
              }
            }
            """);

        _parser.UpdateStateFromMessage(_campaign, message);

        Assert.Equal(1, _campaign.Messages.Count);
        Assert.Contains(_campaign.Characters, c => c is { Name: "Sarmilan", Description: "A charming and attractive mage hailing from the mystical land of Eldoria.", Type: CharacterType.Humanoid });
        Assert.Equal("Thundertop City", _campaign.Environments.Last().Name);
        Assert.Empty(_campaign.Events);
    }
}

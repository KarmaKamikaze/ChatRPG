using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace ChatRPG.API.Tools;

public class HealCharacterTool(
    IConfiguration configuration,
    Campaign campaign,
    ToolUtilities utilities,
    string name,
    string? description = null) : AgentTool(name, description)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, (int, int)> HealingRanges = new()
    {
        { "low", (5, 10) },
        { "medium", (10, 20) },
        { "high", (15, 25) },
        { "extraordinary", (25, 80) }
    };

    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        try
        {
            var healInput = JsonSerializer.Deserialize<HealInput>(input, JsonOptions) ??
                              throw new JsonException("Failed to deserialize");

            var instruction = configuration.GetSection("SystemPrompts").GetValue<string>("HealCharacterInstruction")!;
            var character = await utilities.FindCharacter(campaign, healInput.Input!, instruction);

            if (character is null)
            {
                return "Could not determine the character to heal. The character does not exist in the game. " +
                       "Consider creating the character before healing it.";
            }

            // Determine damage
            Random rand = new Random();
            var (minHealing, maxHealing) = HealingRanges[healInput.Magnitude!];
            var healing = rand.Next(minHealing, maxHealing);

            character.AdjustHealth(healing);

            return $"The character {character.Name} is healed for {healing} health points. They now have {character.CurrentHealth} health points out of a total of {character.MaxHealth}.";

        }
        catch (Exception)
        {
            return "Could not determine the character to heal. Tool input format was invalid. " +
                   "Please provide a valid character name, description, and magnitude level in valid JSON without markdown.";
        }
    }
}

using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using System.Text.Json;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace ChatRPG.API.Tools;

public class WoundCharacterTool(
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

    private static readonly Dictionary<string, (int, int)> DamageRanges = new()
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
            var woundInput = JsonSerializer.Deserialize<WoundInput>(input, JsonOptions) ??
                              throw new JsonException("Failed to deserialize");

            var instruction = configuration.GetSection("SystemPrompts").GetValue<string>("WoundCharacterInstruction")!;
            var character = await utilities.FindCharacter(campaign, woundInput.Input!, instruction);

            if (character is null)
            {
                return "Could not determine the character to wound. The character does not exist in the game. " +
                       "Consider creating the character before wounding it.";
            }

            // Determine damage
            Random rand = new Random();
            var (minDamage, maxDamage) = DamageRanges[woundInput.Severity!];
            var damage = rand.Next(minDamage, maxDamage);

            if (character.AdjustHealth(-damage))
            {
                // Character died
                var result =
                    $"The character {character.Name} is wounded for {damage} damage. They have no remaining health points.";

                if (character.IsPlayer)
                {
                    return result +
                           $" With no health points remaining, {character.Name} dies and the adventure is over. No more actions can be taken.";
                }

                return result +
                       $" With no health points remaining, {character.Name} dies and can no longer perform actions in the narrative.";
            }

            // Character survived
            return
                $"The character {character.Name} is wounded for {damage} damage. They have {character.CurrentHealth} " +
                $"health points out of {character.MaxHealth} remaining.";
        }
        catch (Exception)
        {
            return "Could not determine the character to wound. Tool input format was invalid. " +
                   "Please provide a valid character name, description, and severity level in valid JSON without markdown.";
        }
    }
}

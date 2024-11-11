using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.IdentityModel.Tokens;

namespace ChatRPG.API.Tools;

public class UpdateCharacterTool(
    Campaign campaign,
    string name,
    string? description = null) : AgentTool(name, description)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        try
        {
            var updateCharacterInput = JsonSerializer.Deserialize<CharacterInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
                                       throw new JsonException("Failed to deserialize");
            if (updateCharacterInput.Name.IsNullOrEmpty())
            {
                return Task.FromResult("No character name was provided. Please provide a name for the character.");
            }

            if (updateCharacterInput.Description.IsNullOrEmpty())
            {
                return Task.FromResult(
                    "No character description was provided. Please provide a description for the character.");
            }

            if (updateCharacterInput.Type.IsNullOrEmpty())
            {
                return Task.FromResult("No character type was provided. Please provide a type for the character.");
            }

            if (updateCharacterInput.State.IsNullOrEmpty())
            {
                return Task.FromResult(
                    "No character state was provided. Please provide a health state for the character.");
            }

            try
            {
                var character = campaign.Characters
                    .First(c => c.Name == updateCharacterInput.Name &&
                                c.Type.ToString().Equals(updateCharacterInput.Type,
                                    StringComparison.CurrentCultureIgnoreCase));

                character.Description = updateCharacterInput.Description!;
                character.Environment = campaign.Player.Environment;
                return Task.FromResult(
                    $"{character.Name} has been updated with the following description: {updateCharacterInput.Description}");
            }
            catch (InvalidOperationException)
            {
                // The character was not found in the campaign database
                var newCharacter = new Character(campaign, campaign.Player.Environment,
                    Enum.Parse<CharacterType>(updateCharacterInput.Type!), updateCharacterInput.Name!,
                    updateCharacterInput.Description!, false);

                newCharacter.AdjustHealth((int)-(newCharacter.MaxHealth -
                                                 newCharacter.MaxHealth *
                                                 ScaleHealthBasedOnState(updateCharacterInput.State!)));
                campaign.Characters.Add(newCharacter);
                return Task.FromResult(
                    $"A new character named {newCharacter.Name} has been created with the following description: " +
                    $"{newCharacter.Description}");
            }
        }
        catch (JsonException)
        {
            return Task.FromResult("Could not determine the character to update. Tool input format was invalid. " +
                                   "Please provide a valid character name, description, type, and state in valid JSON.");
        }
    }

    private static double ScaleHealthBasedOnState(string state)
    {
        return state switch
        {
            "Dead" => 0,
            "Unconscious" => 0.1,
            "HeavilyWounded" => 0.35,
            "LightlyWounded" => 0.75,
            "Healthy" => 1,
            _ => 1
        };
    }
}

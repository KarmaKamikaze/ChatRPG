using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using Microsoft.IdentityModel.Tokens;
using Environment = ChatRPG.Data.Models.Environment;

namespace ChatRPG.API.Tools;

public class UpdateEnvironmentTool(
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
            var updateEnvironmentInput = JsonSerializer.Deserialize<EnvironmentInput>(ToolUtilities.RemoveMarkdown(input), JsonOptions) ??
                                         throw new JsonException("Failed to deserialize");

            if (updateEnvironmentInput.Name.IsNullOrEmpty())
            {
                return Task.FromResult("No environment name was provided. Please provide a name for the environment.");
            }

            if (updateEnvironmentInput.Description.IsNullOrEmpty())
            {
                return Task.FromResult(
                    "No environment description was provided. Please provide a description for the environment.");
            }

            if (updateEnvironmentInput.IsPlayerHere is null)
            {
                return Task.FromResult(
                    "No IsPlayerHere boolean was provided. Please provide this boolean for the environment.");
            }

            try
            {
                var environment = campaign.Environments.First(e => e.Name == updateEnvironmentInput.Name);

                environment.Description = updateEnvironmentInput.Description!;
                if (updateEnvironmentInput.IsPlayerHere is true)
                {
                    campaign.Player.Environment = environment;
                }

                return Task.FromResult(
                    $"{environment.Name} has been updated with the following description {updateEnvironmentInput.Description}");
            }
            catch (InvalidOperationException)
            {
                // The environment was not found in the campaign database
                var newEnvironment = new Environment(campaign, updateEnvironmentInput.Name!,
                    updateEnvironmentInput.Description!);

                if (updateEnvironmentInput.IsPlayerHere is true)
                {
                    campaign.Player.Environment = newEnvironment;
                }

                campaign.Environments.Add(newEnvironment);
                return Task.FromResult($"A new environment {newEnvironment.Name} has been created with the following " +
                                       $"description: {newEnvironment.Description}");
            }
        }
        catch (JsonException)
        {
            return Task.FromResult("Could not determine the environment to update. Tool input format was invalid. " +
                                   "Please provide a valid environment name, description, and determine if the " +
                                   "player character is present in the environment in valid JSON.");
        }
    }
}

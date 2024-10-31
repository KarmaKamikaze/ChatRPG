using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;

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

            throw new NotImplementedException();


        }
        catch (JsonException)
        {
            return Task.FromResult("Could not determine the environment to update. Tool input format was invalid. " +
                                   "Please provide a valid environment name, description, and list of characters in valid JSON without markdown.");
        }
    }
}

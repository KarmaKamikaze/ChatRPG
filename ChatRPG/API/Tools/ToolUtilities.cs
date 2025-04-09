using System.Text;
using System.Text.Json;
using ChatRPG.API.Response;
using ChatRPG.API.Tools.InputModels;
using ChatRPG.Data.Models;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using MessageRole = ChatRPG.Data.Models.MessageRole;

namespace ChatRPG.API.Tools;

public class ToolUtilities(IConfiguration configuration)
{
    private const int IncludedPreviousMessages = 4;
    private readonly bool _shouldIncludePreviousMessages = configuration.GetValue<bool>("ShouldSummarize");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Character?> FindCharacter(Campaign campaign, string input, string instruction)
    {
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        var llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.1 }
        };

        // Add system prompt and construct LLM query
        var query = new StringBuilder();
        query.Append(configuration.GetSection("SystemPrompts").GetValue<string>("FindCharacter")!
            .Replace("{instruction}", instruction));

        query.Append(ConstructSummary(campaign, _shouldIncludePreviousMessages));

        query.Append("\n\nHere is the list of all characters present in the story:\n\n{\"characters\": [");

        foreach (var character in campaign.Characters)
        {
            query.Append(
                $"\n{{\n\"name\": \"{character.Name}\", \"description\": \"{character.Description}\", \"type\": \"{character.Type}\"\n}},");
        }

        query.Length--; // Remove last comma

        query.Append("\n]}");

        query.Append($"\n\nThe player is {campaign.Player.Name}. First-person pronouns refer to them.");

        query.Append($"\n\nFind the character using the following content: {input}.");

        var response = await llm.GenerateAsync(query.ToString());

        try
        {
            var llmResponseCharacter =
                JsonSerializer.Deserialize<LlmResponseCharacter>(RemoveMarkdown(response.ToString()),
                    JsonOptions);

            if (llmResponseCharacter is null) return null;

            try
            {
                var character = campaign.Characters
                    .First(c => c.Name == llmResponseCharacter.Name &&
                                c.Description == llmResponseCharacter.Description && c.Type
                                    .ToString().Equals(llmResponseCharacter.Type,
                                        StringComparison.CurrentCultureIgnoreCase));

                return character;
            }
            catch (InvalidOperationException)
            {
                // The character was not found in the campaign database
                return null;
            }
        }
        catch (JsonException)
        {
            return null; // Format was unexpected
        }
    }

    public static string RemoveMarkdown(string text)
    {
        if (text.StartsWith("```json") && text.EndsWith("```"))
        {
            text = text.Replace("```json", "");
            text = text.Replace("```", "");
        }

        return text;
    }

    public static string ConstructSummary(Campaign campaign, bool shouldIncludePreviousMessages)
    {
        var result = $"\n\nThe story up until now: {campaign.GameSummary}";

        if (shouldIncludePreviousMessages)
        {
            var messages = campaign.Messages.TakeLast(IncludedPreviousMessages);
            result +=
                "\n\nUse these previous messages as context. They only serve to give a hint of the current scenario:";
            foreach (var message in messages)
            {
                if (message.Role == MessageRole.User)
                {
                    result += $"\nPlayer: {message.Content}";
                    if (message.Verdict is not null)
                    {
                        result += $"\nAdherence verdict: {message.Verdict.Content}";
                    }
                }
                else
                {
                    result += $"\nGM: {message.Content}\n";
                }
            }
        }

        return result;
    }

    public static bool NodesValidForNewEdge(NarrativeNode? sourceNode, NarrativeNode? targetNode, AddEdgeInput newEdge,
        out List<string> errorMessages)
    {
        errorMessages = [];
        if (targetNode is null)
        {
            errorMessages.Add($"Target node with name {newEdge.TargetNodeName} not found.");
        }

        if (sourceNode is null)
        {
            errorMessages.Add($"Source node with name {newEdge.SourceNodeName} not found.");
        }
        else if (sourceNode.Name == "End")
        {
            errorMessages.Add($"Node {sourceNode.Name} cannot have an edge to another node.");
        }
        else if (sourceNode == targetNode)
        {
            errorMessages.Add($"Node {sourceNode.Name} cannot have an edge to itself.");
        }
        else if (targetNode is not null && sourceNode.Edges.Any(e => e.TargetNode == targetNode))
        {
            errorMessages.Add($"An edge already exists between {newEdge.SourceNodeName} and {newEdge.TargetNodeName}.");
        }

        return errorMessages.Count == 0;
    }
}

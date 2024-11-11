using System.Text;
using System.Text.Json;
using ChatRPG.API.Response;
using ChatRPG.Data.Models;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;

namespace ChatRPG.API.Tools;

public class ToolUtilities(IConfiguration configuration)
{
    private const int IncludedPreviousMessages = 4;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Character?> FindCharacter(Campaign campaign, string input, string instruction)
    {
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI")!);
        var llm = new Gpt4OmniModel(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false }
        };

        // Add system prompt and construct LLM query
        var query = new StringBuilder();
        query.Append(configuration.GetSection("SystemPrompts")?.GetValue<string>("FindCharacter")!
            .Replace("{instruction}", instruction));

        query.Append($"\n\nThe story up until now: {campaign.GameSummary}");

        var content = campaign.Messages.TakeLast(IncludedPreviousMessages).Select(m => m.Content);
        query.Append("\n\nUse these previous messages as context:");
        foreach (var message in content)
        {
            query.Append($"\n {message}");
        }

        query.Append("\n\nHere is the list of all characters present in the story:\n\n{\"characters\": [\n");

        foreach (var character in campaign.Characters)
        {
            query.Append(
                $"{{\"name\": \"{character.Name}\", \"description\": \"{character.Description}\", \"type\": \"{character.Type}\"}},");
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
}
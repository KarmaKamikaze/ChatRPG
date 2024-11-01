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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Character?> FindCharacter(Campaign campaign, string input, string instruction)
    {
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI")!);
        var llm = new Gpt4Model(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false }
        };

        // Add system prompt and construct LLM query
        var query = new StringBuilder();
        query.Append(configuration.GetSection("SystemPrompts")?.GetValue<string>("FindCharacter")!
            .Replace("{instruction}", instruction));

        query.Append($"\n\nThe story up until now: {campaign.GameSummary}");

        query.Append($"\n\nFind the character using the following content: {input}");

        query.Append("\n\nHere is the list of all characters present in the story:\n\n{\"characters\": [\n");

        foreach (var character in campaign.Characters)
        {
            query.Append(
                $"{{\"name\": \"{character.Name}\", \"description\": \"{character.Description}\", \"type\": \"{character.Type}\"}},");
        }

        query.Length--; // Remove last comma

        query.Append("\n]}");

        var response = await llm.GenerateAsync(query.ToString());

        try
        {
            var llmResponseCharacter =
                JsonSerializer.Deserialize<LlmResponseCharacter>(response.ToString(), JsonOptions);

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
}

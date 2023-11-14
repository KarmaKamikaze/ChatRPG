using OpenAI_API.Chat;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChatRPG.API.Response;

namespace ChatRPG.API;

public partial class OpenAiGptMessage
{
    public OpenAiGptMessage(ChatMessageRole role, string content)
    {
        Role = role;
        Content = content;
        NarrativePart = "";
        UpdateNarrativePart();
    }

    public ChatMessageRole Role { get; }
    public string Content { get; private set; }
    public string NarrativePart { get; private set; }

    public LlmResponse? TryParseFromJson()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<LlmResponse>(Content, options);
    }

    public void AddChunk(string chunk)
    {
        Content += chunk;
        UpdateNarrativePart();
    }

    private void UpdateNarrativePart()
    {
        Match match = NarrativeRegex().Match(Content);
        if (match is { Success: true, Groups.Count: 2 })
        {
            NarrativePart = match.Groups[1].ToString();
        }
    }

    [GeneratedRegex("^\\s*{\\s*\"narrative\":\\s*\"([^\"]*)", RegexOptions.IgnoreCase)]
    private static partial Regex NarrativeRegex();
}

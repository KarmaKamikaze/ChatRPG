using OpenAI_API.Chat;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChatRPG.API.Response;
using ChatRPG.Data.Models;
using ChatRPG.Pages;
using Microsoft.IdentityModel.Tokens;

namespace ChatRPG.API;

public partial class OpenAiGptMessage
{
    public OpenAiGptMessage(ChatMessageRole role, string content)
    {
        Role = role;
        Content = content;
        NarrativePart = "";
        UpdateNarrativePart();
        if (!Content.IsNullOrEmpty() && NarrativePart.IsNullOrEmpty() && role.Equals(ChatMessageRole.Assistant))
        {
            NarrativePart = Content;
        }
    }

    public OpenAiGptMessage(ChatMessageRole role, string content, UserPromptType userPromptType) : this(role, content)
    {
        UserPromptType = userPromptType;
    }

    public ChatMessageRole Role { get; }
    public string Content { get; private set; }
    public string NarrativePart { get; private set; }
    public readonly UserPromptType UserPromptType = UserPromptType.Do;

    public LlmResponse? TryParseFromJson()
    {
        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<LlmResponse>(Content, options);
        }
        catch (JsonException)
        {
            return new LlmResponse { Narrative = Content }; // Format was unexpected
        }
    }

    public void AddChunk(string chunk)
    {
        Content += chunk.Replace("\\\"", "'");
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

    public static OpenAiGptMessage FromMessage(Message message)
    {
        ChatMessageRole role = ChatMessageRole.FromString(message.Role.ToString().ToLower());
        return new OpenAiGptMessage(role, message.Content);
    }
}

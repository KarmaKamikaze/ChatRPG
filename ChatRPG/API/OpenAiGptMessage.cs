using ChatRPG.Data.Models;
using ChatRPG.Pages;

namespace ChatRPG.API;

public class OpenAiGptMessage
{
    public OpenAiGptMessage(MessageRole role, string content)
    {
        Role = role;
        Content = content;
    }

    public OpenAiGptMessage(MessageRole role, string content, UserPromptType userPromptType) : this(role, content)
    {
        UserPromptType = userPromptType;
    }

    public MessageRole Role { get; }
    public string Content { get; private set; }
    public readonly UserPromptType UserPromptType = UserPromptType.Do;

    public void AddChunk(string chunk)
    {
        Content += chunk.Replace("\\\"", "'");
    }

    public static OpenAiGptMessage FromMessage(Message message)
    {
        return new OpenAiGptMessage(message.Role, message.Content);
    }
}

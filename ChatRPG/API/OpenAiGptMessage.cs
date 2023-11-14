using OpenAI_API.Chat;

namespace ChatRPG.API;

public class OpenAiGptMessage
{
    public OpenAiGptMessage(ChatMessageRole role, string content)
    {
        Role = role;
        Content = content;
    }
    
    public ChatMessageRole Role { get; }
    public string Content { get; set; }
}
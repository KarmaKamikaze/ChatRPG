using ChatRPG.API;

namespace ChatRPG.Services.Events;

public class ChatCompletionReceivedEventArgs(OpenAiGptMessage message) : EventArgs
{
    public OpenAiGptMessage Message { get; } = message;
}

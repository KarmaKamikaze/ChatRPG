using ChatRPG.API;

namespace ChatRPG.Services.Events;

public class ChatCompletionReceivedEventArgs : EventArgs
{
    public ChatCompletionReceivedEventArgs(OpenAiGptMessage message)
    {
        Message = message;
    }
    public OpenAiGptMessage Message { get; }
}
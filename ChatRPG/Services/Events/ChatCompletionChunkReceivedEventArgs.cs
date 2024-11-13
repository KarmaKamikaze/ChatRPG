namespace ChatRPG.Services.Events;

public class ChatCompletionChunkReceivedEventArgs(bool isStreamingDone, string? chunk = null) : EventArgs
{
    public bool IsStreamingDone { get; } = isStreamingDone;
    public string? Chunk { get; } = chunk;
}

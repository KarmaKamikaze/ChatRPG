namespace ChatRPG.Services.Events;

public class ChatCompletionChunkReceivedEventArgs : EventArgs
{
    public bool IsStreamingDone { get; }
    public string? Chunk { get; }

    public ChatCompletionChunkReceivedEventArgs(bool isStreamingDone, string? chunk = null)
    {
        IsStreamingDone = isStreamingDone;
        Chunk = chunk;
    }
}
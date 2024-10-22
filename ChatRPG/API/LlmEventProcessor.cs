using System.Text;
using System.Threading.Channels;
using LangChain.Providers;
using LangChain.Providers.OpenAI;

namespace ChatRPG.API;

public class LlmEventProcessor
{
    private readonly object _lock = new object();
    private readonly StringBuilder _buffer = new StringBuilder();
    private bool _foundFinalAnswer = false;
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    public LlmEventProcessor(OpenAiChatModel model)
    {
        model.DeltaReceived += OnDeltaReceived;
        model.ResponseReceived += OnResponseReceived;
    }

    public async IAsyncEnumerable<string> GetContentStreamAsync()
    {
        while (await _channel.Reader.WaitToReadAsync())
        {
            while (_channel.Reader.TryRead(out var chunk))
            {
                yield return chunk;
            }
        }
    }

    private void OnDeltaReceived(object? sender, ChatResponseDelta delta)
    {
        lock (_lock)
        {
            if (_foundFinalAnswer)
            {
                // Directly output content after "Final Answer: " has been detected
                _channel.Writer.TryWrite(delta.Content);
            }
            else
            {
                // Accumulate the content in the buffer
                _buffer.Append(delta.Content);

                // Check if the buffer contains "Final Answer: "
                var bufferString = _buffer.ToString();
                int finalAnswerIndex = bufferString.IndexOf("Final Answer:", StringComparison.Ordinal);

                if (finalAnswerIndex != -1)
                {
                    // Output everything after "Final Answer: " has been detected
                    int startOutputIndex = finalAnswerIndex + "Final Answer:".Length;

                    // Switch to streaming mode
                    _foundFinalAnswer = true;

                    // Output any content after "Final Answer: "
                    _channel.Writer.TryWrite(bufferString[startOutputIndex..]);

                    // Clear the buffer since it's no longer needed
                    _buffer.Clear();
                }
            }
        }
    }

    private void OnResponseReceived(object? sender, ChatResponse response)
    {
        lock (_lock)
        {
            _buffer.Clear(); // Clear buffer to avoid carrying over any previous data
            if (!_foundFinalAnswer) return;

            // Reset the state so that the process can start over
            _foundFinalAnswer = false;
            _channel.Writer.TryComplete();
        }
    }
}

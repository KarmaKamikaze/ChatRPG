using ChatRPG.API;
using ChatRPG.Data.Models;
using ChatRPG.Services.Events;
using OpenAI_API.Chat;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;

namespace ChatRPG.Services;

public class GameController
{
    private readonly ILogger<GameController> _logger;
    private readonly IOpenAiLlmClient _llmClient;
    private readonly bool _streamChatCompletions;

    public Campaign? Campaign { get; set; }

    public GameController(ILogger<GameController> logger, IOpenAiLlmClient llmClient, IConfiguration configuration)
    {
        _logger = logger;
        _llmClient = llmClient;
        _streamChatCompletions = configuration.GetValue("StreamChatCompletions", true);
        if (configuration.GetValue("UseMocks", false))
        {
            _streamChatCompletions = false; // Streaming does not work with mocking currently.
        }
    }

    public event EventHandler<ChatCompletionReceivedEventArgs>? ChatCompletionReceived;
    public event EventHandler<ChatCompletionChunkReceivedEventArgs>? ChatCompletionChunkReceived;

    private void OnChatCompletionReceived(OpenAiGptMessage message)
    {
        ChatCompletionReceived?.Invoke(this, new ChatCompletionReceivedEventArgs(message));
    }

    private void OnChatCompletionChunkReceived(bool isStreamingDone, string? chunk = null)
    {
        ChatCompletionChunkReceivedEventArgs args = (chunk is null)
            ? new ChatCompletionChunkReceivedEventArgs(isStreamingDone)
            : new ChatCompletionChunkReceivedEventArgs(isStreamingDone, chunk);
        ChatCompletionChunkReceived?.Invoke(this, args);
    }

    public async Task HandleUserPrompt(IList<OpenAiGptMessage> conversation)
    {
        if (Campaign is null) return;

        if (_streamChatCompletions)
        {
            OpenAiGptMessage message = new OpenAiGptMessage(ChatMessageRole.Assistant, "");
            OnChatCompletionReceived(message);

            await foreach (string chunk in _llmClient.GetStreamedChatCompletion(conversation))
            {
                OnChatCompletionChunkReceived(isStreamingDone: false, chunk);
            }
            OnChatCompletionChunkReceived(isStreamingDone: true);
        }
        else
        {
            string response = await _llmClient.GetChatCompletion(conversation);
            OpenAiGptMessage message = new OpenAiGptMessage(ChatMessageRole.Assistant, response);
            OnChatCompletionReceived(message);
        }
    }
}

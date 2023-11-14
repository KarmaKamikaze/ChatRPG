using ChatRPG.API;
using ChatRPG.Services.Events;
using OpenAI_API.Chat;

namespace ChatRPG.Services;

public class GameController
{
    private readonly IOpenAiLlmClient _llmClient;
    private readonly bool _streamChatCompletions;
    
    public GameController(IOpenAiLlmClient llmClient, IConfiguration configuration)
    {
        _llmClient = llmClient;
        _streamChatCompletions = configuration.GetValue("StreamChatCompletions", true);
        if (configuration.GetValue("UseMocks", false))
        {
            _streamChatCompletions = false; // Streaming does not work with mocking currently.
        }
    }

    public event EventHandler<ChatCompletionReceivedEventArgs>? ChatCompletionReceived;
    public event EventHandler<ChatCompletionChunkReceivedEventArgs>? ChatCompletionChunkReceived; 

    public async Task HandleUserPrompt(IList<OpenAiGptMessage> conversation)
    {
        if (_streamChatCompletions)
        {
            OpenAiGptMessage message = new OpenAiGptMessage(ChatMessageRole.Assistant, "");
            ChatCompletionReceived?.Invoke(this, new ChatCompletionReceivedEventArgs(message));

            await foreach (string chunk in _llmClient.GetStreamedChatCompletion(conversation))
            {
                ChatCompletionChunkReceived?.Invoke(this, new ChatCompletionChunkReceivedEventArgs(false, chunk));
            }
            ChatCompletionChunkReceived?.Invoke(this, new ChatCompletionChunkReceivedEventArgs(true));
        }
        else
        {
            string response = await _llmClient.GetChatCompletion(conversation);
            OpenAiGptMessage message = new OpenAiGptMessage(ChatMessageRole.Assistant, response);
            ChatCompletionReceived?.Invoke(this, new ChatCompletionReceivedEventArgs(message));
        }
    }
}

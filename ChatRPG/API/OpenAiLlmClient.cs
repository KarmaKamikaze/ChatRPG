using Microsoft.IdentityModel.Tokens;
using OpenAI_API;
using OpenAI_API.Chat;

namespace ChatRPG.API;

public class OpenAiLlmClient : IOpenAiLlmClient
{
    private const string Model = "gpt-4";
    private const double Temperature = 0.7;

    private readonly OpenAIAPI _openAiApi;

    public OpenAiLlmClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _openAiApi = new OpenAIAPI(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI"));
        _openAiApi.Chat.DefaultChatRequestArgs.Model = Model;
        _openAiApi.Chat.DefaultChatRequestArgs.Temperature = Temperature;
        _openAiApi.HttpClientFactory = httpClientFactory;
    }

    public async Task<string> GetChatCompletion(IList<OpenAiGptMessage> inputs, string systemPrompt)
    {
        Conversation chat = CreateConversation(inputs, systemPrompt);

        return await chat.GetResponseFromChatbotAsync();
    }

    public IAsyncEnumerable<string> GetStreamedChatCompletion(IList<OpenAiGptMessage> inputs, string systemPrompt)
    {
        Conversation chat = CreateConversation(inputs, systemPrompt);

        return chat.StreamResponseEnumerableFromChatbotAsync();
    }

    private Conversation CreateConversation(IList<OpenAiGptMessage> messages, string systemPrompt)
    {
        if (messages.IsNullOrEmpty()) throw new ArgumentNullException(nameof(messages));

        Conversation chat = _openAiApi.Chat.CreateConversation();
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            chat.AppendSystemMessage(systemPrompt);
        }
        foreach (OpenAiGptMessage openAiGptInputMessage in messages)
        {
            chat.AppendMessage(openAiGptInputMessage.Role, openAiGptInputMessage.Content);
        }

        return chat;
    }
}

using Microsoft.IdentityModel.Tokens;
using OpenAI_API;
using OpenAI_API.Chat;

namespace ChatRPG.API;

public class OpenAiLlmClient : IOpenAiLlmClient
{
    private const string Model = "gpt-3.5-turbo";
    private const double Temperature = 0.7;

    private readonly OpenAIAPI _openAiApi;

    public OpenAiLlmClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _openAiApi = new OpenAIAPI(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI"));
        _openAiApi.Chat.DefaultChatRequestArgs.Model = Model;
        _openAiApi.Chat.DefaultChatRequestArgs.Temperature = Temperature;
        _openAiApi.HttpClientFactory = httpClientFactory;
    }

    public async Task<string> GetChatCompletion(params OpenAiGptMessage[] inputs)
    {
        Conversation chat = CreateConversation(inputs);

        return await chat.GetResponseFromChatbotAsync();
    }

    public IAsyncEnumerable<string> GetStreamedChatCompletion(params OpenAiGptMessage[] inputs)
    {
        Conversation chat = CreateConversation(inputs);

        return chat.StreamResponseEnumerableFromChatbotAsync();
    }

    private Conversation CreateConversation(params OpenAiGptMessage[] messages)
    {
        if (messages.IsNullOrEmpty()) throw new ArgumentNullException(nameof(messages));

        Conversation chat = _openAiApi.Chat.CreateConversation();
        foreach (OpenAiGptMessage openAiGptInputMessage in messages)
        {
            chat.AppendMessage(ChatMessageRole.FromString(openAiGptInputMessage.Role), openAiGptInputMessage.Content);
        }

        return chat;
    }
}

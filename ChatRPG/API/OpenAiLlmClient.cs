using Microsoft.IdentityModel.Tokens;
using OpenAI_API;
using OpenAI_API.Chat;

namespace ChatRPG.API;

public class OpenAiLlmClient : IOpenAiLlmClient
{
    private const string Model = "gpt-3.5-turbo";
    private const double Temperature = 0.7;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAIAPI _openAiApi;

    public OpenAiLlmClient(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _openAiApi = new OpenAIAPI(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI") ?? string.Empty);
        _openAiApi.Chat.DefaultChatRequestArgs.Model = Model;
        _openAiApi.Chat.DefaultChatRequestArgs.Temperature = Temperature;
    }

    public async Task<string> GetChatCompletion(OpenAiGptMessage input)
    {
        var inputList = new List<OpenAiGptMessage> { input };
        return await GetChatCompletion(inputList);
    }

    public async Task<string> GetChatCompletion(List<OpenAiGptMessage> inputs)
    {
        if (inputs.IsNullOrEmpty()) throw new ArgumentNullException(nameof(inputs));

        var chat = _openAiApi.Chat.CreateConversation();
        _openAiApi.HttpClientFactory = _httpClientFactory;
        foreach (var openAiGptInputMessage in inputs)
        {
            chat.AppendMessage(ChatMessageRole.FromString(openAiGptInputMessage.Role), openAiGptInputMessage.Content);
        }

        return await chat.GetResponseFromChatbotAsync();
    }

    public IAsyncEnumerable<string> GetStreamedChatCompletion(OpenAiGptMessage input)
    {
        var inputList = new List<OpenAiGptMessage> { input };
        return GetStreamedChatCompletion(inputList);
    }

    public IAsyncEnumerable<string> GetStreamedChatCompletion(List<OpenAiGptMessage> inputs)
    {
        if (inputs.IsNullOrEmpty()) throw new ArgumentNullException(nameof(inputs));

        var chat = _openAiApi.Chat.CreateConversation();
        _openAiApi.HttpClientFactory = _httpClientFactory;
        foreach (var openAiGptInputMessage in inputs)
        {
            chat.AppendMessage(ChatMessageRole.FromString(openAiGptInputMessage.Role), openAiGptInputMessage.Content);
        }

        return chat.StreamResponseEnumerableFromChatbotAsync();
    }
}

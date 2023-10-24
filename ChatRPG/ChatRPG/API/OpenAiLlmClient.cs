using Microsoft.IdentityModel.Tokens;
using RestSharp;
using RestSharp.Authenticators;

namespace ChatRPG.API;

public class OpenAiLlmClient : IOpenAiLlmClient, IDisposable
{
    private const string OpenAiBaseUrl = "https://api.openai.com/v1/";
    private const string Model = "gpt-3.5-turbo";
    private const double Temperature = 0.7;
    private const double PromptToken1KCost = 0.0015;
    private const double CompletionToken1KCost = 0.002;

    private readonly ILogger<OpenAiLlmClient> _logger;
    private readonly RestClient _client;

    public OpenAiLlmClient(ILogger<OpenAiLlmClient> logger, IConfiguration configuration,
        HttpMessageHandler httpMessageHandler)
    {
        _logger = logger;

        var options = new RestClientOptions(OpenAiBaseUrl)
        {
            Authenticator = new JwtAuthenticator(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI") ?? string.Empty),
            FailOnDeserializationError = false,
            ConfigureMessageHandler = _ => httpMessageHandler
        };
        _client = new RestClient(options);
    }

    public async Task<ChatCompletionObject> GetChatCompletion(List<OpenAiGptInputMessage> inputs)
    {
        if (inputs.IsNullOrEmpty()) throw new ArgumentNullException(nameof(inputs));

        var openAiGptInput = new OpenAiGptInput(Model, inputs, Temperature);

        var request = new RestRequest("chat/completions", Method.Post);
        request.AddJsonBody(openAiGptInput, ContentType.Json);

        _logger.LogInformation("""
                               Request URL: {Url}
                               Method: {Method}
                               Parameters: {Parameters}
                               Messages: {Messages}
                               """,
            OpenAiBaseUrl + request.Resource,
            request.Method,
            string.Join(", ", request.Parameters.Select(p => $"{p.Name}={p.Value}")),
            string.Join(", ", inputs.Select(input => input.Content))
        );

        var response = await _client.ExecuteAsync<ChatCompletionObject>(request);

        if (response.ErrorException != null)
        {
            _logger.LogError("Error retrieving data from API: {ErrorExceptionMessage}", response.ErrorException.Message);
        }

        var data = response!.Data;

        if (data is null)
        {
            throw new EmptyResponseException(response, "The API response has no data.");
        }

        var promptTokens = data.Usage.PromptTokens;
        var completionTokens = data.Usage.CompletionTokens;

        var promptCost = (promptTokens / 1000.0) * PromptToken1KCost;
        var completionCost = (completionTokens / 1000.0) * CompletionToken1KCost;
        var estimatedCost = promptCost + completionCost;

        _logger.LogInformation("""
                               Prompt tokens: {PTokens}
                               Completion tokens: {CTokens}
                               Estimated cost: {EstCost}
                               """,
            promptTokens,
            completionTokens,
            "$" + estimatedCost
        );

        return data;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class EmptyResponseException : Exception
{
    public RestResponse Response;
    public EmptyResponseException(RestResponse response, string message) : base(message)
    {
        Response = response;
    }
}

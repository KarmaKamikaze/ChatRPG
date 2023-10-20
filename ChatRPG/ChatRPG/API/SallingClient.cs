using RestSharp;
using RestSharp.Authenticators;

namespace ChatRPG.API;

public class SallingClient : IFoodWasteClient, IDisposable
{
    private const string SallingBaseUrl = "https://api.sallinggroup.com/v1/";
    private readonly ILogger<SallingClient> _logger;
    private readonly RestClient _client;

    public SallingClient(ILogger<SallingClient> logger, IConfiguration configuration)
    {
        _logger = logger;

        var options = new RestClientOptions(SallingBaseUrl)
        {
            Authenticator = new JwtAuthenticator(configuration.GetSection("ApiKeys").GetValue<String>("Salling")),
            FailOnDeserializationError = false
        };
        _client = new RestClient(options);
    }

    public async Task<List<FoodWasteResponse>> GetFoodwasteResponse(string zip)
    {
        var request = new RestRequest("food-waste/", Method.Get);
        request.AddQueryParameter("zip", zip);

        _logger.LogInformation("""
                               Request URL: {Url}
                               Method: {Method}
                               Parameters: {Parameters}
                               """,
            SallingBaseUrl + request.Resource,
            request.Method,
            string.Join(", ", request.Parameters.Select(p => $"{p.Name}={p.Value}"))
        );


        var response = await _client.ExecuteAsync<List<FoodWasteResponse>>(request);

        if (response.ErrorException != null)
        {
            _logger.LogError($"Error retrieving data from API: {response.ErrorException.Message}");
        }

        return response!.Data;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}

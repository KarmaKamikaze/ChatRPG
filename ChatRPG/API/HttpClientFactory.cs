namespace ChatRPG.API;

public class HttpClientFactory : IHttpClientFactory
{
    private HttpMessageHandler _httpMessageHandler;
    
    public HttpClientFactory(HttpMessageHandler httpMessageHandler)
    {
        _httpMessageHandler = httpMessageHandler;
    }
    
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_httpMessageHandler);
    }
}

namespace ChatRPG.API;

public class HttpClientFactory : IHttpClientFactory
{
    private HttpMessageHandlerFactory _httpMessageHandlerFactory;
    
    public HttpClientFactory(HttpMessageHandlerFactory httpMessageHandlerFactory)
    {
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
    }
    
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(_httpMessageHandlerFactory.CreateHandler());
    }
}

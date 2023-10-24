using RichardSzalay.MockHttp;

namespace ChatRPG.API;

public class HttpMessageHandlerFactory : IHttpMessageHandlerFactory
{
    private bool _useMocks;
    public HttpMessageHandlerFactory(IConfiguration configuration)
    {
        _useMocks = configuration.GetValue<bool>("UseMocks");
    }

   public HttpMessageHandler CreateHandler(string name)
   {
       if (!_useMocks)
       {
           return new HttpClientHandler();
       }

       var mockHttpMessageHandler = new MockHttpMessageHandler();
       mockHttpMessageHandler.When("*")
           .Respond(GenerateMockResponse);

       return mockHttpMessageHandler;
   }

    private static HttpResponseMessage GenerateMockResponse(HttpRequestMessage request)
    {
        Console.Write("Please enter mocked API response: ");
        var input = Console.ReadLine();
        var responseContent = new StringContent($$"""
                                                  {
                                                      "id": "chatcmpl-000",
                                                      "object": "chat.completion",
                                                      "created": {{(int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds)}},
                                                      "model": "gpt-3.5-turbo",
                                                      "choices": [{
                                                          "index": 0,
                                                          "message": {
                                                              "role": "assistant",
                                                              "content": "{{input}}"
                                                          },
                                                          "finish_reason": "stop"
                                                      }],
                                                      "usage": {
                                                          "prompt_tokens": 0,
                                                          "completion_tokens": 0,
                                                          "total_tokens": 0
                                                      }
                                                  }
                                                  """);

        return new HttpResponseMessage
        {
            Content = responseContent
        };
    }
}

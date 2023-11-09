namespace ChatRPG.API;

public interface IOpenAiLlmClient
{
    Task<string> GetChatCompletion(params OpenAiGptMessage[] inputs);
    IAsyncEnumerable<string> GetStreamedChatCompletion(params OpenAiGptMessage[] inputs);
}

public record OpenAiGptMessage(string Role, string Content);

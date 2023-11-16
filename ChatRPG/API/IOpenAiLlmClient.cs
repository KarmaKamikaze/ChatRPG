namespace ChatRPG.API;

public interface IOpenAiLlmClient
{
    Task<string> GetChatCompletion(IList<OpenAiGptMessage> inputs);
    IAsyncEnumerable<string> GetStreamedChatCompletion(IList<OpenAiGptMessage> inputs);
}
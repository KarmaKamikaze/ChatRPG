namespace ChatRPG.API;

public interface IOpenAiLlmClient
{
    Task<string> GetChatCompletion(IList<OpenAiGptMessage> inputs, string systemPrompt);
    IAsyncEnumerable<string> GetStreamedChatCompletion(IList<OpenAiGptMessage> inputs, string systemPrompt);
}
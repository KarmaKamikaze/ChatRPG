namespace ChatRPG.API;

public interface IOpenAiLlmClient
{
    Task<string> GetChatCompletion(OpenAiGptMessage input);
    Task<string> GetChatCompletion(List<OpenAiGptMessage> inputs);
    IAsyncEnumerable<string> GetStreamedChatCompletion(OpenAiGptMessage input);
    IAsyncEnumerable<string> GetStreamedChatCompletion(List<OpenAiGptMessage> inputs);
}

public record ChatCompletionObject(string Id, string Object, int Created, string Model, Choice[] Choices, Usage Usage);

public record Choice(int Index, Message Message, string FinishReason);

public record Message(string Role, string Content);

public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);

public record OpenAiGptMessage(string Role, string Content);

public record OpenAiGptInput(string Model, List<OpenAiGptMessage> Messages, double Temperature);

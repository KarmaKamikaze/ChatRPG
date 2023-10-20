namespace ChatRPG.API;

public interface IOpenAiGptClient
{
    Task<ChatCompletionObject> GetChatCompletion(string input);
}

public record ChatCompletionObject(string Id, string Object, int Created, string Model, Choice[] Choices, Usage Usage);

public record Choice(int Index, Message Message, string FinishReason);

public record Message(string Role, string Content);

public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);

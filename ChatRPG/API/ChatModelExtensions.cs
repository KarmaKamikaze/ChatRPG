using LangChain.Providers;

namespace ChatRPG.API;

public static class ChatModelExtensions
{
    /// <summary>
    /// Sends a single prompt to any chat model and returns the final response as text. Works across
    /// providers (OpenAI, Ollama, ...) since it only relies on the common IChatModel contract, unlike
    /// the OpenAI-specific streaming convenience methods.
    /// </summary>
    public static async Task<string> GenerateAsTextAsync(this IChatModel model, string prompt,
        CancellationToken cancellationToken = default)
    {
        ChatResponse? lastResponse = null;
        await foreach (var response in model.GenerateAsync(prompt, cancellationToken: cancellationToken))
        {
            lastResponse = response;
        }

        return lastResponse?.ToString() ?? string.Empty;
    }
}

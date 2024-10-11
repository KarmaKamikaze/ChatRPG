using ChatRPG.Data.Models;
using ChatRPG.Pages;

namespace ChatRPG.API;

public interface IReActLlmClient
{
    Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input);
    IAsyncEnumerable<string> GetStreamedChatCompletionAsync(Campaign campaign, string actionPrompt, string input);
}

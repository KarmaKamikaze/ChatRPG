using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;

namespace ChatRPG.API.Tools;

public class NarrativeTool(
    IConfiguration configuration,
    Campaign campaign,
    string actionPrompt,
    string name,
    string? description = null) : AgentTool(name, description)
{
    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        var llm = new Gpt4Model(provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false }
        };

        var query = configuration.GetSection("SystemPrompts")?.GetValue<string>("Narrative")!
            .Replace("{action}", actionPrompt).Replace("{input}", input).Replace("{summary}", campaign.GameSummary);
        return await llm.GenerateAsync(query!, cancellationToken: token);
    }
}

using ChatRPG.API.Memory;
using ChatRPG.Data.Models;
using ChatRPG.Pages;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.API;

public class ReActLlmClient : IReActLlmClient
{
    private readonly Gpt4Model _llm;
    private readonly string _reActPrompt;

    public ReActLlmClient(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")?.GetValue<string>("ReAct"));
        _reActPrompt = configuration.GetSection("SystemPrompts")?.GetValue<string>("ReAct")!;
        var provider = new OpenAiProvider(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI")!);
        _llm = new Gpt4Model(provider);
    }

    public async Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var memory = new ChatRPGConversationMemory(campaign, _llm);
        var agent = new ReActAgentChain(_llm, memory, _reActPrompt, useStreaming: false);
        //agent.UseTool();

        var chain = Set(actionPrompt, "action") | Set(input, "input") | agent;
        return (await chain.RunAsync("text"))!;
    }

    public IAsyncEnumerable<string> GetStreamedChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var eventProcessor = new LlmEventProcessor(_llm);
        var memory = new ChatRPGConversationMemory(campaign, _llm);
        var agent = new ReActAgentChain(_llm, memory, _reActPrompt, useStreaming: false);
        //agent.UseTool();

        var chain = Set(actionPrompt, "action") | Set(input, "input") | agent;
        _ = Task.Run(async () => await chain.RunAsync());

        return eventProcessor.GetContentStreamAsync();
    }
}

using ChatRPG.API.Memory;
using ChatRPG.Data.Models;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.API;

public class ReActLlmClient : IReActLlmClient
{
    private readonly OpenAiProvider _provider;
    private readonly string _reActPrompt;

    public ReActLlmClient(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")?.GetValue<string>("ReAct"));
        _reActPrompt = configuration.GetSection("SystemPrompts")?.GetValue<string>("ReAct")!;
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI")!);
    }

    public async Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var llm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false }
        };
        var memory = new ChatRPGConversationMemory(llm, campaign.GameSummary);
        var agent = new ReActAgentChain(llm, memory, _reActPrompt, "", useStreaming: false);
        //agent.UseTool();

        var chain = Set(input, "input") | agent;
        var result = await chain.RunAsync("text");

        UpdateCampaign(campaign, memory);

        return result!;
    }

    public async IAsyncEnumerable<string> GetStreamedChatCompletionAsync(Campaign campaign, string actionPrompt,
        string input)
    {
        var agentLlm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = true }
        };

        var memoryLlm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false }
        };

        var eventProcessor = new LlmEventProcessor(agentLlm);
        var memory = new ChatRPGConversationMemory(memoryLlm, campaign.GameSummary);
        var agent = new ReActAgentChain(agentLlm, memory, _reActPrompt, "", useStreaming: true);
        //agent.UseTool();

        var chain = Set(input, "input") | agent;

        var result = chain.RunAsync();

        await foreach (var content in eventProcessor.GetContentStreamAsync())
        {
            yield return content;
        }

        await result;

        UpdateCampaign(campaign, memory);
    }

    private static void UpdateCampaign(Campaign campaign, ChatRPGConversationMemory memory)
    {
        campaign.GameSummary = memory.Summary;
        foreach (var (role, message) in memory.Messages)
        {
            // Only add the message, is the list is empty.
            // This is because if the list is empty, the input is the initial prompt. Not player input.
            if (campaign.Messages.Count != 0)
            {
                campaign.Messages.Add(new Message(campaign, role, message));
            }
        }
    }
}

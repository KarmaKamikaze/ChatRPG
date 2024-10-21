using Anthropic;
using ChatRPG.API.Memory;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;
using Message = ChatRPG.Data.Models.Message;
using MessageRole = ChatRPG.Data.Models.MessageRole;

namespace ChatRPG.API;

public class ReActLlmClient : IReActLlmClient
{
    private readonly IConfiguration _configuration;
    private readonly OpenAiProvider _provider;
    private readonly string _reActPrompt;

    public ReActLlmClient(IConfiguration configuration)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")?.GetValue<string>("ReAct"));
        _configuration = configuration;
        _reActPrompt = _configuration.GetSection("SystemPrompts")?.GetValue<string>("ReAct")!;
        _provider = new OpenAiProvider(_configuration.GetSection("ApiKeys")?.GetValue<string>("OpenAI")!);
    }

    public async Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var llm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Number = 1 }
        };
        var memory = new ChatRPGConversationMemory(llm, campaign.GameSummary);
        var agent = new ReActAgentChain(llm, memory, _reActPrompt, actionPrompt, useStreaming: false);
        var tools = CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

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
            Settings = new OpenAiChatSettings() { UseStreaming = true, Number = 1 }
        };

        var memoryLlm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Number = 1 }
        };

        var eventProcessor = new LlmEventProcessor(agentLlm);
        var memory = new ChatRPGConversationMemory(memoryLlm, campaign.GameSummary);
        var agent = new ReActAgentChain(agentLlm, memory, _reActPrompt, actionPrompt, useStreaming: true);
        var tools = CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

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
            // Only add the message, if the list is empty.
            // This is because if the list is empty, the input is the initial prompt. Not player input.
            if (campaign.Messages.Count == 0 && role == MessageRole.User)
            {
                continue;
            }

            campaign.Messages.Add(new Message(campaign, role, message.Trim()));
        }
    }

    private List<AgentTool> CreateTools(Campaign campaign)
    {
        var tools = new List<AgentTool>();
        var utils = new ToolUtilities(_configuration);

        var woundCharacterTool = new WoundCharacterTool(_configuration, campaign, utils, "woundcharactertool",
            "This tool must be used when a character will be hurt or wounded resulting from unnoticed attacks" +
            "or performing dangerous activities that will lead to injury. The tool is only appropriate if the damage " +
            "cannot be mitigated, dodged, or avoided. Example: A character performs a sneak attack " +
            "without being spotted by the enemies they try to attack. A dangerous activity could be to threateningly " +
            "approach a King, which may result in injury when his guards step forward to stop the character. " +
            "Input to this tool must be in the following RAW JSON format: {\"input\": \"The game summary appended with the " +
            "player's input\", \"severity\": \"Describes how devastating the injury to the character will be based on " +
            "the action. Can be one of the following values: {low, medium, high, extraordinary}}");
        tools.Add(woundCharacterTool);

        // Use battle when an attack can be mitigated or dodged by the involved participants.
        // This tool is appropriate for combat, battle between multiple participants,
        // or attacks that can be avoided and a to-hit roll would be needed in order to determine a hit.

        return tools;
    }
}

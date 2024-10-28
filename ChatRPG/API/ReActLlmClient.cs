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
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts").GetValue<string>("ReAct"));
        _configuration = configuration;
        _reActPrompt = _configuration.GetSection("SystemPrompts").GetValue<string>("ReAct")!;
        _provider = new OpenAiProvider(_configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
    }

    public async Task<string> GetChatCompletionAsync(Campaign campaign, string actionPrompt, string input)
    {
        var llm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false }
        };
        var agent = new ReActAgentChain(llm, _reActPrompt, actionPrompt, campaign.GameSummary);
        var tools = CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(input, "input") | agent;
        var result = await chain.RunAsync("text");

        await UpdateCampaign(campaign, input, result!);

        return result!;
    }

    public async IAsyncEnumerable<string> GetStreamedChatCompletionAsync(Campaign campaign, string actionPrompt,
        string input)
    {
        var agentLlm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = true, Temperature = 0.7, }
        };

        var eventProcessor = new LlmEventProcessor(agentLlm);
        var agent = new ReActAgentChain(agentLlm, _reActPrompt, actionPrompt, campaign.GameSummary);
        var tools = CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(input, "input") | agent;

        var response = chain.RunAsync("text");

        await foreach (var content in eventProcessor.GetContentStreamAsync())
        {
            yield return content;
        }

        var result = await response;

        await UpdateCampaign(campaign, input, result!);
    }

    private async Task UpdateCampaign(Campaign campaign, string playerInput, string assistantOutput)
    {
        var newMessages = new List<LangChain.Providers.Message>
        {
            new(playerInput, LangChain.Providers.MessageRole.Human),
            new(assistantOutput, LangChain.Providers.MessageRole.Ai)
        };

        var summaryLlm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        campaign.GameSummary = await summaryLlm.SummarizeAsync(newMessages, campaign.GameSummary ?? "");
        foreach (var message in newMessages)
        {
            // Only add the message, if the list is empty.
            // This is because if the list is empty, the input is the initial prompt. Not player input.
            if (campaign.Messages.Count == 0 && message.Role == LangChain.Providers.MessageRole.Human)
            {
                continue;
            }

            campaign.Messages.Add(new Message(campaign,
                (message.Role == LangChain.Providers.MessageRole.Human ? MessageRole.User : MessageRole.Assistant),
                message.Content.Trim()));
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
            "the action. Can be one of the following values: {low, medium, high, extraordinary}}\". Do not use markdown, " +
            "only raw JSON as input. Use this tool only once per character at most.");
        tools.Add(woundCharacterTool);

        // Use battle when an attack can be mitigated or dodged by the involved participants.
        // This tool is appropriate for combat, battle between multiple participants,
        // or attacks that can be avoided and a to-hit roll would be needed in order to determine a hit.

        return tools;
    }
}

using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

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
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };
        var agent = new ReActAgentChain(llm, _reActPrompt, actionPrompt:actionPrompt, campaign.GameSummary);
        var tools = CreateTools(campaign, actionPrompt);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(input, "input") | agent;
        return (await chain.RunAsync("text"))!;
    }

    public async IAsyncEnumerable<string> GetStreamedChatCompletionAsync(Campaign campaign, string actionPrompt,
        string input)
    {
        var agentLlm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = true, Temperature = 0.7 }
        };

        var eventProcessor = new LlmEventProcessor(agentLlm);
        var agent = new ReActAgentChain(agentLlm, _reActPrompt, actionPrompt:actionPrompt, campaign.GameSummary);
        var tools = CreateTools(campaign, actionPrompt);
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

        await response;
    }

    private List<AgentTool> CreateTools(Campaign campaign, string actionPrompt)
    {
        var tools = new List<AgentTool>();
        var utils = new ToolUtilities(_configuration);

        /*var narrativeTool = new NarrativeTool(_configuration, campaign, actionPrompt, "narrativetool",
            "This tool must be used when the player's input requires a narrative response. " +
            "The tool is appropriate for any action that requires a narrative response. " +
            "Example: A player's input could be to explore a new area, " +
            "interact with a non-player character, or perform a specific action. " +
            "Input to this tool must be the player's most recent action.");
        tools.Add(narrativeTool);*/

        var woundCharacterTool = new WoundCharacterTool(_configuration, campaign, utils, "woundcharactertool",
            "This tool must be used when a character will be hurt or wounded resulting from unnoticed attacks" +
            "or performing dangerous activities that will lead to injury. The tool is only appropriate if the damage " +
            "cannot be mitigated, dodged, or avoided. Example: A character performs a sneak attack " +
            "without being spotted by the enemies they try to attack. A dangerous activity could be to threateningly " +
            "approach a King, which may result in injury when his guards step forward to stop the character. " +
            "Input to this tool must be in the following RAW JSON format: {\"input\": \"The player's input\", " +
            "\"severity\": \"Describes how devastating the injury to the character will be based on the action. " +
            "Can be one of the following values: {low, medium, high, extraordinary}}\". Do not use markdown, " +
            "only raw JSON as input. Use this tool only once per character at most.");
        tools.Add(woundCharacterTool);

        var healCharacterTool = new HealCharacterTool(_configuration, campaign, utils, "healcharactertool",
            "This tool must be used when a character performs an action that could heal or restore them to " +
            "health after being wounded. The tool is only appropriate if the healing can be done without any " +
            "further actions. Example: A character is wounded by an enemy attack and the player decides to heal " +
            "the character. Another example would be a scenario where a character consumes a beneficial item like " +
            "a potion, a magical item, or spends time in an area that could provide healing " +
            "benefits. Resting may provide modest healing effects depending on the duration of the rest. " +
            "Input to this tool must be in the following RAW JSON format: {\"input\": \"The player's input\", " +
            "\"magnitude\": \"Describes how much health the character will regain based on the action. " +
            "Can be one of the following values: {low, medium, high, extraordinary}}\". " +
            "Do not use markdown, only raw JSON as input. Use this tool only once per character at most.");
        tools.Add(healCharacterTool);

        // Use battle when an attack can be mitigated or dodged by the involved participants.
        // This tool is appropriate for combat, battle between multiple participants,
        // or attacks that can be avoided and a to-hit roll would be needed in order to determine a hit.

        return tools;
    }
}

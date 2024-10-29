using System.Text;
using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;

namespace ChatRPG.Services;

public class GameStateManager
{
    private readonly OpenAiProvider _provider;
    private readonly IPersistenceService _persistenceService;
    private readonly IConfiguration _configuration;
    private readonly string _updateCampaignPrompt;

    public GameStateManager(IConfiguration configuration, IPersistenceService persistenceService)
    {
        _configuration = configuration;
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts").GetValue<string>("UpdateCampaignFromNarrative"));
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _updateCampaignPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("UpdateCampaignFromNarrative")!;
        _persistenceService = persistenceService;
    }

    public async Task SaveCurrentState(Campaign campaign)
    {
        await _persistenceService.SaveAsync(campaign);
    }

    public async Task UpdateCampaignFromNarrative(Campaign campaign, string narrative)
    {
        var llm = new Gpt4Model(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        var characters = new StringBuilder();
        characters.Append("{\"characters\": [\n");

        foreach (var character in campaign.Characters)
        {
            characters.Append(
                $"{{\"name\": \"{character.Name}\", \"description\": \"{character.Description}\", \"type\": \"{character.Type}\"}},");
        }

        characters.Length--; // Remove last comma
        characters.Append("\n]}");

        var environments = new StringBuilder();
        environments.Append("{\"environments\": [\n");

        foreach (var environment in campaign.Environments)
        {
            environments.Append($"{{\"name:\" \"{environment.Name}\", \"description\": \"{environment.Description}\"}},");
        }

        environments.Length--; // Remove last comma

        environments.Append("\n]}");

        var agent = new ReActAgentChain(llm, _updateCampaignPrompt, characters.ToString(), environments.ToString(),
            gameSummary: campaign.GameSummary);

        var tools = CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var chain = Set(narrative, "input") | agent;
        await chain.RunAsync("text");
    }

    private List<AgentTool> CreateTools(Campaign campaign)
    {
        var tools = new List<AgentTool>();
        var utils = new ToolUtilities(_configuration);

        var updateCharacterTool = new UpdateCharacterTool(campaign, "updatecharactertool",
            "This tool must be used to create a new character or update an existing character in the campaign. " +
            "Example: The narrative text mentions a new character or contains changes to an existing character. " +
            "Input to this tool must be in the following RAW JSON format: {\"name\": \"<character name>\", " +
            "\"description\": \"<new or updated character description>\", \"type\": \"<character type>\", " +
            "\"state\": \"<character health state>\"}, where type is one of the following: {Humanoid, SmallCreature, " +
            "LargeCreature, Monster}, and state is one of the following: {Dead, Unconscious, HeavilyWounded, LightlyWounded, Healthy}.");
        tools.Add(updateCharacterTool);

        return tools;
    }


    public async Task StoreMessagesInCampaign(Campaign campaign, string playerInput, string assistantOutput)
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
}

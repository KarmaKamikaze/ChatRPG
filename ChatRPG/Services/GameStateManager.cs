using System.Text;
using ChatRPG.API;
using ChatRPG.API.Tools;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;
using LangChain.Providers;
using LangChain.Providers.OpenAI;
using LangChain.Providers.OpenAI.Predefined;
using static LangChain.Chains.Chain;
using Message = ChatRPG.Data.Models.Message;
using MessageRole = ChatRPG.Data.Models.MessageRole;

namespace ChatRPG.Services;

public class GameStateManager
{
    private readonly OpenAiProvider _provider;
    private readonly IPersistenceService _persistenceService;
    private readonly string _updateCampaignPrompt;
    private readonly bool _archivistDebugMode;
    private readonly bool _summarizeMessages;

    public GameStateManager(IConfiguration configuration, IPersistenceService persistenceService)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("UpdateCampaignFromNarrative"));
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _updateCampaignPrompt =
            configuration.GetSection("SystemPrompts").GetValue<string>("UpdateCampaignFromNarrative")!;
        _archivistDebugMode = configuration.GetValue<bool>("ArchivistChainDebug");
        _summarizeMessages = configuration.GetValue<bool>("ShouldSummarize");
        _persistenceService = persistenceService;
    }

    public async Task SaveCurrentState(Campaign campaign)
    {
        await _persistenceService.SaveAsync(campaign);
    }

    public async Task UpdateCampaignFromNarrative(Campaign campaign, string input, string narrative)
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
            environments.Append(
                $"{{\"name:\" \"{environment.Name}\", \"description\": \"{environment.Description}\"}},");
        }

        environments.Length--; // Remove last comma

        environments.Append("\n]}");

        var agent = new ReActAgentChain(_archivistDebugMode ? llm.UseConsoleForDebug() : llm, _updateCampaignPrompt,
            characters.ToString(), campaign.Player.Name, environments.ToString(), gameSummary: campaign.GameSummary);

        var tools = CreateTools(campaign);
        foreach (var tool in tools)
        {
            agent.UseTool(tool);
        }

        var newInformation = $"The player says: {input}\nThe DM says: {narrative}";

        var chain = Set(newInformation, "input") | agent;
        await chain.RunAsync("text");
    }

    private static List<AgentTool> CreateTools(Campaign campaign)
    {
        var tools = new List<AgentTool>();

        var updateCharacterTool = new UpdateCharacterTool(campaign, "updatecharactertool",
            "This tool must be used to create a new character or update an existing character in the campaign. " +
            "Example: The narrative text mentions a new character or contains changes to an existing character. " +
            "Input to this tool must be in the following RAW JSON format: {\"name\": \"<character name>\", " +
            "\"description\": \"<new or updated character description>\", \"type\": \"<character type>\", " +
            "\"state\": \"<character health state>\"}, where type is one of the following: {SmallCreature, Humanoid, " +
            "LargeCreature, Monster}, and state is one of the following: {Dead, Unconscious, HeavilyWounded, " +
            "LightlyWounded, Healthy}. The description of a character could describe their physical characteristics, " +
            "personality, what they are known for, or other cool descriptive features. " +
            "The tool should only be used once per character.");
        tools.Add(updateCharacterTool);

        var updateEnvironmentTool = new UpdateEnvironmentTool(campaign, "updateenvironmenttool",
            "This tool must be used to create a new environment or update an existing environment in the " +
            "campaign. Example: The narrative text mentions a new environment or contains changes to an existing " +
            "environment. An environment refers to a place, location, or area that is well enough defined that it " +
            "warrants its own description. Such a place could be a landmark with its own history, a building where " +
            "story events take place, or a larger place like a magical forest. Input to this tool must be in the " +
            "following RAW JSON format: {\"name\": \"<environment name>\", \"description\": \"<new or updated " +
            "environment description>\", \"isPlayerHere\": <true if the Player character is currently at this " +
            "environment, false otherwise>}, where the description of an environment could describe its physical " +
            "characteristics, its significance, the creatures that inhabit it, the weather, or other cool " +
            "descriptive features so that it gives the Player useful information about the places they travel to " +
            "while keeping the locations' descriptions interesting, mysterious and engaging. " +
            "The tool should only be used once per environment.");
        tools.Add(updateEnvironmentTool);

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

        if (_summarizeMessages)
        {
            campaign.GameSummary = await summaryLlm.SummarizeAsync(newMessages, campaign.GameSummary ?? "");
        }
        else
        {
            campaign.GameSummary ??= string.Empty;
            campaign.GameSummary += string.Join("\n", newMessages.Select(m => m.Content)) + "\n";
        }

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

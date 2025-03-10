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
    private readonly IConfiguration _configuration;

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
        _configuration = configuration;
        _persistenceService = persistenceService;
    }

    public async Task SaveCurrentState(Campaign campaign)
    {
        await _persistenceService.SaveAsync(campaign);
    }

    public async Task UpdateCampaignFromNarrative(Campaign campaign, string input, string narrative)
    {
        var llm = new Gpt4OmniModel(_provider)
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

    private List<AgentTool> CreateTools(Campaign campaign)
    {
        var tools = new List<AgentTool>();

        var updateCharacterToolDescription = new StringBuilder();
        updateCharacterToolDescription.Append(
            "This tool must be used to create a new character or update an existing character in the campaign. " +
            "Example: The narrative text mentions a new character or contains changes to an existing character. " +
            "Input to this tool must be in the following RAW JSON format: {\"name\": \"<character name>\", " +
            "\"description\": \"<new or updated character description>\", \"type\": \"<character type>\", " +
            "\"state\": \"<character health state>\"}, where type is one of the following: {");

        var characterTypes = Enum.GetNames<CharacterType>();
        for (var i = 0; i < characterTypes.Length; i++)
        {
            updateCharacterToolDescription.Append($"{characterTypes[i]}");
            if (i < characterTypes.Length - 1)
            {
                updateCharacterToolDescription.Append(", ");
            }
        }

        updateCharacterToolDescription.Append(
            "}, and state is one of the following: {Dead, Unconscious, HeavilyWounded, " +
            "LightlyWounded, Healthy}. The description of a character could describe their physical characteristics, " +
            "personality, what they are known for, or other cool descriptive features. " +
            "The tool should only be used once per character.");
        var updateCharacterTool =
            new UpdateCharacterTool(campaign, "updatecharactertool", updateCharacterToolDescription.ToString());
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

        var searchScenarioTool = new SearchScenarioTool(_configuration, campaign, "searchscenariotool",
            "This tool must be used whenever you are unsure of what is available in the " +
            "current location or need to reference existing details from " +
            "the adventure module to maintain consistency. The tool helps you retrieve structured information " +
            "about the game world, ensuring it adheres to the story's established details.\n" +
            "When to use this tool:\n" +
            "- Unknown Details: If you do not have enough information about a location, NPC, quest, faction, or " +
            "available actions, this tool must be used to find relevant context from the adventure module.\n" +
            "- Story Consistency: You should follow the scenario structure but can adapt if minor " +
            "details are missing. However, if key details exist in the adventure module, they must be used to shape " +
            "the game world.\n" +
            "- Exploration & Interaction: If changes occur related to an NPC, object, or location that has " +
            "not been described yet, use this tool to determine what is relevant.\n" +
            "Example Uses:\n" +
            "Scenario 1 – Player in a Castle Hall\n" +
            "The player says: \"I want to talk to the ghost of the former king.\"\n" +
            "The DM says: \"As you approach the broken throne, the suit of armor standing to its left begins to rattle. " +
            "You see two red eyes staring at you from the helmet. \"I am Ulemar. Who awakens me?\"\"\n" +
            "You are unsure who the entity in the suit of armor is. You call the tool with the input \"Who haunts the  " +
            "suit of armor in the Castle Hall?\" and retrieve details, learning " +
            "that the suit of armor containing red eyes that greets the player is Ulemar, the Knight of the King, who " +
            "secretly plans to have the player release his evil spirit.\n" +
            "Scenario 2 – Exploring a Village\n" +
            "The player says: \"I enter a random house in the village. What do I see?\"\n" +
            "The DM says: \"You enter a modest home with a fireplace and a wooden table. A family of four sits around " +
            "the table, eating a meal. The man of the house grabs his knife and says \"Who are you? Why are you here?\"\"\n" +
            "You are unsure about who this family is and call the tool with the input \"Tell me about the houses in the village.\"\n" +
            "If the adventure module contains details about the house, the tool retrieves them.\n" +
            "If the house is not mentioned, you may improvise a minor detail (e.g., \"A modest home with a " +
            "fireplace and a wooden table\") while ensuring it does not contradict existing world details.\n" +
            "Scenario 3 – Deviating from the Main Story\n" +
            "The player says: \"I want to talk to the ghost of the former king.\" \n" +
            "You are unsure what the player can find in the forest. You call the tool with the input \"Tell me about " +
            "the forest. Are there any objectives there? Does anything happen when the player leaves the dungeon?\"\n" +
            "If the adventure module has no information about the forest, you may allow limited exploration but " +
            "eventually use the tool to reference details from the current chapter, nudging the player back toward " +
            "the dungeon in a natural way.\n" +
            "Use this tool as often as needed to maintain consistency, but allow for creative flexibility when small " +
            "details are missing. Never fabricate major lore elements if the adventure module provides context. You " +
            "can call this tool multiple times in a single narrative to ensure the story remains coherent and engaging.");
        tools.Add(searchScenarioTool);

        return tools;
    }

    public async Task StoreMessagesInCampaign(Campaign campaign, string playerInput, string assistantOutput)
    {
        var newMessages = new List<LangChain.Providers.Message>
        {
            new(playerInput, LangChain.Providers.MessageRole.Human),
            new(assistantOutput, LangChain.Providers.MessageRole.Ai)
        };

        var summaryLlm = new Gpt4OmniModel(_provider)
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

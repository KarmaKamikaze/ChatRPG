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

public class ReActArchivistAgent
{
    private readonly OpenAiProvider _provider;
    private readonly IPersistenceService _persistenceService;
    private readonly bool _archivistDebugMode;
    private readonly bool _summarizeMessages;
    private readonly string _reActPrompt;

    public ReActArchivistAgent(IConfiguration configuration, IPersistenceService persistenceService)
    {
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI"));
        ArgumentException.ThrowIfNullOrEmpty(configuration.GetSection("SystemPrompts")
            .GetValue<string>("ArchivistReActPrompt"));
        _provider = new OpenAiProvider(configuration.GetSection("ApiKeys").GetValue<string>("OpenAI")!);
        _archivistDebugMode = configuration.GetValue<bool>("ArchivistChainDebug");
        _summarizeMessages = configuration.GetValue<bool>("ShouldSummarize");
        _reActPrompt = configuration.GetSection("SystemPrompts").GetValue<string>("ArchivistReActPrompt")!;
        _persistenceService = persistenceService;
    }

    public async Task SaveCurrentState(Campaign campaign)
    {
        await _persistenceService.SaveAsync(campaign);
    }

    public async Task UpdateCampaignFromNarrative(Campaign campaign, string input, string narrative)
    {
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

        var llm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.7 }
        };

        var agent = new ReActAgentChain(_archivistDebugMode ? llm.UseConsoleForDebug() : llm, reactPrompt: _reActPrompt,
            gameSummary: campaign.GameSummary, characters: characters.ToString(), playerCharacter: campaign.Player.Name,
            environments: environments.ToString());

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

        var updateCharacterToolDescription = new StringBuilder();

        updateCharacterToolDescription.Append("""
                                              ### Character Creation & Update Tool

                                              This tool is used to **create a new character** or **update an existing character** in the campaign.
                                              A character can be an ally, enemy, neutral figure, or even a mysterious unknown.

                                              ---

                                              ### Conditions for Use:
                                              - The narrative introduces a **new character**.
                                              - The narrative updates information about an **existing character**.

                                              ---

                                              ### Use Cases:
                                              - A new NPC or creature is named or described.
                                              - The narrative updates a character's **appearance**, **health**, or **role**.
                                              - The player interacts with someone important enough to track.

                                              ---

                                              ### Character Description Guidelines:
                                              - Include **physical features**, **personality**, or **distinctive traits**.
                                              - Mention **known affiliations**, **roles**, or **notable actions**.
                                              - Keep descriptions vivid and interesting for the player.

                                              ---

                                              ### Expected Input Format:
                                              Input must be provided in **RAW JSON format** (do not use markdown):

                                              {
                                                "name": "<character name>",
                                                "description": "<new or updated character description>",
                                                "type": "<character type>",
                                                "state": "<character health state>"
                                              }

                                              - **name**: The character's name.
                                              - **description**: A detailed and engaging character description.
                                              - **type**: One of the following values: {
                                              """);

        var characterTypes = Enum.GetNames<CharacterType>();
        for (var i = 0; i < characterTypes.Length; i++)
        {
            updateCharacterToolDescription.Append(characterTypes[i]);
            if (i < characterTypes.Length - 1)
            {
                updateCharacterToolDescription.Append(", ");
            }
        }

        updateCharacterToolDescription.Append("""
                                              }
                                              - **state**: One of the following values: {Dead, Unconscious, HeavilyWounded, LightlyWounded, Healthy}

                                              ---

                                              The tool should only be used **once per character**.
                                              """);

        var updateCharacterTool =
            new UpdateCharacterTool(campaign, "updatecharactertool", updateCharacterToolDescription.ToString());
        tools.Add(updateCharacterTool);

        var updateEnvironmentTool = new UpdateEnvironmentTool(
            campaign,
            "updateenvironmenttool",
            """
            This tool must be used to **create a new environment** or **update an existing environment** in the campaign.  

            **Example Usage:**  
            The narrative text mentions a new environment or contains changes to an existing environment.

            ---

            ### **What is an Environment?**
            An environment refers to a **place**, **location**, or **area** that is well enough defined to warrant its own description.  
            Such places could include:
            - A **landmark** with its own history.
            - A **building** where story events take place.
            - A larger place like a **magical forest**.

            ---

            ### **Tool Input Format**
            Input to this tool must be in the following **RAW JSON format**:
            {
                "name": "<environment name>",
                "description": "<new or updated environment description>",
                "isPlayerHere": <true if the Player character is currently at this environment, false otherwise>
            }

            ### **Description of an Environment**
            - The **description** could cover:
              - Its **physical characteristics**.
              - Its **significance** in the story.
              - The **creatures** that inhabit it.
              - The **weather** or other descriptive features.

            The goal is to provide the Player with **useful information** about the places they travel to, 
            while keeping the locations' descriptions **interesting**, **mysterious**, and **engaging**.

            ---

            ### **Important Notes**
            - The tool should **only be used once** per environment to avoid redundancy and maintain clarity in the narrative.
            """);
        tools.Add(updateEnvironmentTool);

        return tools;
    }

    public async Task StoreMessagesInCampaign(Campaign campaign, string playerInput, string assistantOutput)
    {
        var newMessages = new List<LangChain.Providers.Message>
        {
            new(playerInput.Trim(), LangChain.Providers.MessageRole.Human),
            new(assistantOutput.Trim(), LangChain.Providers.MessageRole.Ai)
        };

        var summaryLlm = new Gpt4OmniModel(_provider)
        {
            Settings = new OpenAiChatSettings() { UseStreaming = false, Temperature = 0.4 }
        };

        if (_summarizeMessages)
        {
            campaign.GameSummary = await summaryLlm.SummarizeAsync(newMessages, campaign.GameSummary);
        }
        else
        {
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
